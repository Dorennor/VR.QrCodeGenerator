using System.IO;
using System.Text;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Pictures;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.Tests
{
    /// <summary>
    /// Pictures as templates (<see cref="QrPicture"/>): the picture's pixels in the code color become the shape around
    /// the code, filled with the QR-like texture, and the result always scans.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class PictureTests
    {
        private const string Url = "https://github.com/Dorennor/VR.QrCodeGenerator";

        private readonly QrCodeGeneratorService _service = new QrCodeGeneratorService();

        private static byte[] Png(int width, int height, Action<SKCanvas> draw)
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height));
            surface.Canvas.Clear(SKColors.Transparent);
            draw(surface.Canvas);
            using var image = surface.Snapshot();

            return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        }

        /// <summary>
        /// A black cat silhouette on a transparent background, taller than wide.
        /// </summary>
        private static byte[] Cat() => Png(500, 600, canvas =>
        {
            using var black = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            canvas.DrawOval(250, 380, 170, 190, black);
            canvas.DrawCircle(250, 150, 110, black);

            using var ears = new SKPath();
            ears.MoveTo(160, 110);
            ears.LineTo(170, 10);
            ears.LineTo(240, 60);
            ears.Close();
            ears.MoveTo(340, 110);
            ears.LineTo(330, 10);
            ears.LineTo(260, 60);
            ears.Close();
            canvas.DrawPath(ears, black);
        });

        /// <summary>
        /// The outline of a heart, line art on white.
        /// </summary>
        private static byte[] HeartOutline() => Png(500, 500, canvas =>
        {
            canvas.Clear(SKColors.White);
            using var line = new SKPaint { Color = SKColors.Black, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 14 };
            using var heart = new SKPath();
            heart.MoveTo(250, 460);
            heart.CubicTo(-60, 250, 80, -40, 250, 130);
            heart.CubicTo(420, -40, 560, 250, 250, 460);
            canvas.DrawPath(heart, line);
        });

        /// <summary>
        /// A colorful photo-like scene: no clear silhouette.
        /// </summary>
        private static byte[] Scene() => Png(400, 300, canvas =>
        {
            using var sky = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 300), [new SKColor(40, 90, 200), new SKColor(250, 180, 120)], SKShaderTileMode.Clamp) };
            using var tree = new SKPaint { Color = new SKColor(30, 20, 10) };
            canvas.DrawRect(0, 0, 400, 300, sky);
            canvas.DrawCircle(90, 120, 60, tree);
            canvas.DrawRect(80, 120, 20, 180, tree);
        });

        private static IQrStyle Style(byte[] picture, double sensitivity = 0.5, int detail = 1) =>
            new ShapeStyle(new MaskShape(QrPicture.Load(picture).ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White, sensitivity), PictureCodesViewModel.DefaultCodeSize, inkPart: ShapePart.Textured), detail);

        private static IEnumerable<TestCaseData> Pictures()
        {
            yield return new TestCaseData(Cat()).SetArgDisplayNames("cat silhouette");
            yield return new TestCaseData(HeartOutline()).SetArgDisplayNames("heart outline");
            yield return new TestCaseData(Scene()).SetArgDisplayNames("photo-like scene");
            yield return new TestCaseData(Png(200, 200, canvas => canvas.Clear(SKColors.Black))).SetArgDisplayNames("all black");
        }

        [TestCaseSource(nameof(Pictures))]
        public void EveryPicture_ScansAtEveryPixelSize(byte[] picture)
        {
            var style = Style(picture);

            var failures = new[] { 3, 4, 6, 9, 12, 16 }
                           .Where(pixels => QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = pixels }, style)) != Url)
                           .ToList();

            Assert.That(failures, Is.Empty, "pixel sizes that did not scan");
        }

        [Test]
        public void Silhouette_BecomesTheShapeAroundTheCode()
        {
            var options = new QrCodeOptions { PixelsPerModule = 4 };
            using var plain = SKBitmap.Decode(_service.Generate(Url, options));
            using var cat = SKBitmap.Decode(_service.Generate(Url, options, Style(Cat())));

            Assert.Multiple(() =>
            {
                Assert.That(cat.Height, Is.GreaterThan(plain.Height * 2), "the cat is much bigger than the code");
                Assert.That(cat.Height, Is.GreaterThan(cat.Width), "and keeps its tall shape");
            });
        }

        [Test]
        public void Template_TakesThePixelsInTheInkColor()
        {
            var picture = QrPicture.Load(Png(100, 100, canvas =>
            {
                canvas.Clear(SKColors.White);
                using var red = new SKPaint { Color = SKColors.Red };
                using var black = new SKPaint { Color = SKColors.Black };
                canvas.DrawRect(0, 0, 50, 100, red);
                canvas.DrawRect(50, 0, 50, 100, black);
            }));

            var blackInk = picture.ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White);
            var redInk = picture.ToTemplate(PictureTemplateSource.CodeColor, SKColors.Red, SKColors.White);

            Assert.Multiple(() =>
            {
                Assert.That(blackInk[50, 75], Is.True, "black on a black-ink template");
                Assert.That(redInk[50, 25], Is.True, "red on a red-ink template");
                Assert.That(redInk[50, 75], Is.False, "black is not red ink");
            });
        }

        [Test]
        public void HigherSensitivity_TakesMoreOfThePicture()
        {
            // A gradient from black to white: the share of ink follows the sensitivity.
            var picture = QrPicture.Load(Png(200, 50, canvas =>
            {
                using var gradient = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(200, 0), [SKColors.Black, SKColors.White], SKShaderTileMode.Clamp) };
                canvas.DrawRect(0, 0, 200, 50, gradient);
            }));

            int Ink(double sensitivity) => picture.ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White, sensitivity).Cast<bool>().Count(ink => ink);

            Assert.That(Ink(0.7), Is.GreaterThan(Ink(0.5)).And.GreaterThan(0));
            Assert.That(Ink(0.5), Is.GreaterThan(Ink(0.3)));
        }

        [Test]
        public void TransparentPixels_AreNeverInk()
        {
            var template = QrPicture.Load(Cat()).ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White, 0.8);

            Assert.That(template[0, 0], Is.False, "the corner is transparent");
        }

        [Test]
        public void PictureWithoutInk_GivesThePlainCode()
        {
            var options = new QrCodeOptions { PixelsPerModule = 4 };

            var bytes = _service.Generate(Url, options, Style(Png(200, 200, canvas => canvas.Clear(SKColors.White))));

            Assert.That(bytes, Is.EqualTo(_service.Generate(Url, options)));
        }

        [Test]
        public void TemplatePreview_MarksTheCodeSquare()
        {
            var template = QrPicture.Load(Cat()).ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White);
            using var preview = SKBitmap.Decode(QrPicture.TemplatePreviewPng(template, 0.4, SKColors.Black));

            Assert.Multiple(() =>
            {
                Assert.That(preview.Width, Is.EqualTo(template.GetLength(1)));
                Assert.That(preview.GetPixel(preview.Width / 2, preview.Height / 2), Is.Not.EqualTo(SKColors.Black).And.Not.EqualTo(SKColors.White), "the centre is the code's square");
            });
        }

        /// <summary>
        /// A light gray figure on white with a darker outline: what the QR code color mode misses.
        /// </summary>
        private static byte[] LightFigureOnWhite() => Png(600, 400, canvas =>
        {
            canvas.Clear(SKColors.White);
            using var body = new SKPaint { Color = new SKColor(195, 198, 210), IsAntialias = true };
            using var fur = new SKPaint { Color = new SKColor(250, 250, 252), IsAntialias = true };
            using var outline = new SKPaint { Color = new SKColor(150, 150, 165), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 3 };
            canvas.DrawOval(300, 220, 220, 120, body);
            canvas.DrawOval(300, 220, 220, 120, outline);
            canvas.DrawCircle(300, 220, 40, fur);       // a light patch inside the figure
        });

        private static int Ink(bool[,] template) => template.Cast<bool>().Count(ink => ink);

        [Test]
        public void SubjectMode_TakesALightFigureOnWhite()
        {
            var picture = QrPicture.Load(LightFigureOnWhite());

            var subject = picture.ToTemplate(PictureTemplateSource.Subject, SKColors.Black, SKColors.White);
            var codeColor = picture.ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White);

            Assert.Multiple(() =>
            {
                Assert.That(Ink(codeColor), Is.LessThan(Ink(subject) / 10), "light gray is far from black");
                Assert.That(subject[picture.Height / 2, picture.Width / 2 - 150], Is.True, "the gray body is the subject");
                Assert.That(subject[5, 5], Is.False, "the white background is not");
            });
        }

        [Test]
        public void SubjectMode_OnACutOut_TakesWhatIsOpaque()
        {
            // The cat is black on transparent; any opaque color would do.
            var template = QrPicture.Load(Cat()).ToTemplate(PictureTemplateSource.Subject, SKColors.Black, SKColors.White);

            Assert.Multiple(() =>
            {
                Assert.That(template[0, 0], Is.False);
                Assert.That(template[template.GetLength(0) * 380 / 600, template.GetLength(1) / 2], Is.True, "the body");
            });
        }

        [Test]
        public void SmallSpecks_AreDropped()
        {
            var picture = QrPicture.Load(Png(400, 400, canvas =>
            {
                canvas.Clear(SKColors.White);
                using var black = new SKPaint { Color = SKColors.Black };
                canvas.DrawRect(100, 100, 200, 200, black);
                canvas.DrawRect(20, 20, 3, 3, black);      // a speck
            }));

            var template = picture.ToTemplate(PictureTemplateSource.Subject, SKColors.Black, SKColors.White);

            Assert.Multiple(() =>
            {
                Assert.That(template[21, 21], Is.False, "the speck is gone");
                Assert.That(template[200, 200], Is.True, "the square stays");
            });
        }

        [Test]
        public void Subject_DoesNotFillTheBackgroundItSurrounds_ButFillsSmallHoles()
        {
            // An arch: background under it, enclosed by the arch and the code's square, must stay empty.
            var sketch = new bool[300, 400];

            for (var y = 0; y < 300; y++)
            {
                for (var x = 0; x < 400; x++)
                {
                    var leg = (x < 60 || x >= 340) && y >= 40;
                    var top = y < 60;
                    var hole = x >= 20 && x < 30 && y >= 100 && y < 110;     // a small hole inside a leg

                    sketch[y, x] = (leg || top) && !hole;
                }
            }

            var shape = new MaskShape(sketch, 0.3, gapToClose: 6, ShapePart.Textured, codeClosesAreas: false, largestFilledArea: 0.02);
            var size = 29;

            ShapePart PartAt(double x, double y)
            {
                var side = 0.3 * 300;
                return shape.Classify((x - (400 - side) / 2) / side, (y - (300 - side) / 2) / side, size);
            }

            Assert.Multiple(() =>
            {
                Assert.That(PartAt(120, 250), Is.EqualTo(ShapePart.None), "under the arch");
                Assert.That(PartAt(25, 105), Is.EqualTo(ShapePart.Textured), "the small hole is filled");
            });
        }

        [Test]
        public void EveryDetailLevel_ScansAtEveryPixelSize([Values(1, 2, 3)] int detail)
        {
            var style = new ShapeStyle(new MaskShape(QrPicture.Load(LightFigureOnWhite()).ToTemplate(PictureTemplateSource.Subject, SKColors.Black, SKColors.White), 0.4, inkPart: ShapePart.Textured, codeClosesAreas: false, largestFilledArea: 0.02), detail);

            var failures = new[] { 3, 4, 6, 9, 12, 16 }
                           .Where(pixels => QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = pixels }, style)) != Url)
                           .ToList();

            Assert.That(failures, Is.Empty, "pixel sizes that did not scan");
        }

        [Test]
        public void FinerDetail_KeepsTheCodeAndAddsSmallerCells()
        {
            // At 6 px per module, "Fine" draws the shape in 3 px cells: the image holds runs of 3 px, "Normal" only of 6.
            static int ShortestRun(byte[] png)
            {
                using var bitmap = SKBitmap.Decode(png);
                var shortest = int.MaxValue;

                for (var y = 0; y < bitmap.Height; y += 3)
                {
                    var run = 1;

                    for (var x = 1; x < bitmap.Width; x++)
                    {
                        if (bitmap.GetPixel(x, y) == bitmap.GetPixel(x - 1, y))
                        {
                            run++;
                            continue;
                        }

                        shortest = Math.Min(shortest, run);
                        run = 1;
                    }
                }

                return shortest;
            }

            var normal = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 6 }, Style(Cat(), detail: 1));
            var fine = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 6 }, Style(Cat(), detail: 2));

            Assert.Multiple(() =>
            {
                Assert.That(ShortestRun(normal), Is.EqualTo(6));
                Assert.That(ShortestRun(fine), Is.EqualTo(3));
                Assert.That(QrCodeGeneratorServiceTests.Decode(fine), Is.EqualTo(Url));
            });
        }

        [Test]
        public void NotAPicture_IsRefused()
        {
            Assert.Throws<InvalidDataException>(() => QrPicture.Load(Encoding.UTF8.GetBytes("not a picture")));
        }

        [Test]
        public void ViewModel_AsksForAPictureFirst_ThenUsesItAsTheTemplate()
        {
            var folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());
            var pictureFile = Path.Combine(folder, "cat.png");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(pictureFile, Cat());

            try
            {
                var dialogs = new RecordingDialogService();
                var settings = new Settings { PathsSettings = new PathsSettings { UseCustomPath = true, OutputPath = Path.Combine(folder, "out") } };
                var viewModel = new PictureCodesViewModel(dialogs, _service, new NullLoggingService(), settings)
                {
                    QrCodeContent = Url,
                    UseExactPixelSize = true,
                    SelectedPixelsPerModule = 4
                };

                viewModel.GenerateQrCodeCommand.Execute(null);
                var warnedWithoutPicture = dialogs.Warnings.Count == 1 && viewModel.QrCodeImage is null;

                viewModel.LoadPicture(pictureFile);
                var firstPreview = viewModel.TemplatePreview;

                viewModel.GenerateQrCodeCommand.Execute(null);
                var saved = Directory.GetFiles(Path.Combine(folder, "out")).Single();
                using var image = SKBitmap.Decode(saved);

                // The ink follows the QR code color, so a new color makes a new template.
                viewModel.SelectedForegroundColor = System.Windows.Media.Colors.DarkRed;

                Assert.Multiple(() =>
                {
                    Assert.That(warnedWithoutPicture, Is.True, "Generate without a picture warns and makes nothing");
                    Assert.That(viewModel.PictureName, Is.EqualTo("cat.png"));
                    Assert.That(firstPreview, Is.Not.Null);
                    Assert.That(image.Height, Is.GreaterThan(image.Width), "the code has the cat's tall shape");
                    Assert.That(QrCodeGeneratorServiceTests.Decode(File.ReadAllBytes(saved)), Is.EqualTo(Url));
                    Assert.That(viewModel.TemplatePreview, Is.Not.SameAs(firstPreview));
                });

                viewModel.RemovePictureCommand.Execute(null);
                Assert.That(viewModel.HasPicture, Is.False);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void ViewModel_UnreadableFile_ShowsAnError()
        {
            var file = Path.GetTempFileName();
            File.WriteAllText(file, "not a picture");

            try
            {
                var dialogs = new RecordingDialogService();
                var viewModel = new PictureCodesViewModel(dialogs, _service, new NullLoggingService(), new Settings());

                viewModel.LoadPicture(file);

                Assert.Multiple(() =>
                {
                    Assert.That(dialogs.Errors, Has.Count.EqualTo(1));
                    Assert.That(viewModel.HasPicture, Is.False);
                });
            }
            finally
            {
                File.Delete(file);
            }
        }

        private sealed class RecordingDialogService : IDialogService
        {
            public List<string> Warnings { get; } = [];

            public List<string> Errors { get; } = [];

            public string OpenFolderDialog(string initialDirectory = null) => string.Empty;

            public string OpenFileDialog(string filter = null) => string.Empty;

            public string SaveFileDialog(string defaultFileName) => string.Empty;

            public void ShowWarnMessageBox(string message) => Warnings.Add(message);

            public void ShowErrorMessageBox(string message) => Errors.Add(message);

            public void ShowMessageBox(string message, string title = "Info", System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage icon = System.Windows.MessageBoxImage.Information)
            {
            }
        }

        private sealed class NullLoggingService : ILoggingService
        {
            public void ApplySettings(LoggingSettings settings)
            {
            }

            public void Info(string message, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }

            public void Warn(string message, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }

            public void Error(string message, Exception exception = null, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }
        }
    }
}
