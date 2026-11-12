using System.IO;
using System.Text;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Generation;
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
        public void TemplatePreview_IsTheTemplatePixelForPixel()
        {
            var template = QrPicture.Load(Cat()).ToTemplate(PictureTemplateSource.CodeColor, SKColors.Black, SKColors.White);
            using var preview = SKBitmap.Decode(QrPicture.TemplatePreviewPng(template, null, SKColors.Black));

            Assert.Multiple(() =>
            {
                Assert.That(preview.Width, Is.EqualTo(template.GetLength(1)));
                Assert.That(preview.Height, Is.EqualTo(template.GetLength(0)));
                Assert.That(preview.GetPixel(preview.Width / 2, preview.Height * 380 / 600), Is.EqualTo(SKColors.Black), "the cat's body");
                Assert.That(preview.GetPixel(0, 0), Is.EqualTo(SKColors.White), "the background");
            });
        }

        [Test]
        public void CodeSquare_IsCentredOnThePoint_AndStaysInside()
        {
            var (left, top, side) = MaskShape.CodeSquare(400, 200, 0.5, new ShapePoint(0.25, 0.5));
            var (edgeLeft, edgeTop, _) = MaskShape.CodeSquare(400, 200, 0.5, new ShapePoint(1.2, -0.3));

            Assert.Multiple(() =>
            {
                Assert.That(side, Is.EqualTo(100));
                Assert.That(left, Is.EqualTo(50), "centred on x = 100");
                Assert.That(top, Is.EqualTo(50), "centred on y = 100");
                Assert.That(edgeLeft, Is.EqualTo(300), "pushed back inside on the right");
                Assert.That(edgeTop, Is.EqualTo(0), "and at the top");
            });
        }

        [Test]
        public void MovedCode_MovesTheShapeAroundIt_AndStillScans()
        {
            // A wide black bar: with the code on the left, the bar sticks out to the right of the code, and the other way round.
            var bar = new bool[100, 400];

            for (var y = 30; y < 70; y++)
            {
                for (var x = 0; x < 400; x++)
                    bar[y, x] = true;
            }

            QrCodeOptions Options() => new QrCodeOptions { PixelsPerModule = 4 };

            byte[] WithCodeAt(double u) => _service.Generate(Url, Options(), new ShapeStyle(new MaskShape(bar, 0.6, inkPart: ShapePart.Textured, codeCenter: new ShapePoint(u, 0.5))));

            var left = WithCodeAt(0.1);
            var right = WithCodeAt(0.9);

            // The code is taller than the bar, so the topmost dark pixel is the code's: where it is, as a share of the width.
            static double CodePosition(byte[] png)
            {
                using var bitmap = SKBitmap.Decode(png);

                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        if (bitmap.GetPixel(x, y).Red < 128)
                            return (double)x / bitmap.Width;
                    }
                }

                return -1;
            }

            Assert.Multiple(() =>
            {
                Assert.That(QrCodeGeneratorServiceTests.Decode(left), Is.EqualTo(Url));
                Assert.That(QrCodeGeneratorServiceTests.Decode(right), Is.EqualTo(Url));
                Assert.That(CodePosition(left), Is.LessThan(0.2), "code at the left end of the bar");
                Assert.That(CodePosition(right), Is.GreaterThan(0.6), "code at the right end of the bar");
            });
        }

        [Test]
        public void MovingTheCode_KeepsItInside_AndCenterResetsIt()
        {
            // What dragging the blue square does (CodePlacementOverlay), on a 600 x 720 picture with a 30 % code.
            const int width = 600, height = 720;

            var corner = CodePlacement.MoveTo(width, height, 0.3, -1000, -1000);
            var (cornerLeft, cornerTop, _) = MaskShape.CodeSquare(width, height, 0.3, corner);
            var moved = CodePlacement.MoveTo(width, height, 0.3, width * 0.3, height * 0.7);

            var folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());
            var pictureFile = Path.Combine(folder, "cat.png");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(pictureFile, Cat());

            try
            {
                var viewModel = new PictureCodesViewModel(new RecordingDialogService(), new WpfPlatformService(), _service, new NullLoggingService(), new Settings());
                viewModel.LoadPicture(pictureFile);
                viewModel.CodeCenter = moved;
                viewModel.CenterCodeCommand.Execute(null);

                Assert.Multiple(() =>
                {
                    Assert.That(cornerLeft, Is.EqualTo(0), "dragged past the top-left corner: stops at the edge");
                    Assert.That(cornerTop, Is.EqualTo(0));
                    Assert.That(moved.U, Is.EqualTo(0.3).Within(0.01));
                    Assert.That(moved.V, Is.EqualTo(0.7).Within(0.01));
                    Assert.That(viewModel.CodeCenter, Is.EqualTo(new ShapePoint(0.5, 0.5)));
                });
            }
            finally
            {
                Directory.Delete(folder, true);
            }
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

        /// <summary>
        /// A whole picture, dithered: the default style of the Picture tab.
        /// </summary>
        private static IQrStyle Dithered(byte[] picture, int detail = 4, double codeSize = 0.3, ShapePoint? codeCenter = null, double? codeBlend = null)
        {
            var loaded = QrPicture.Load(picture);
            var mask = new MaskShape(loaded.ToTemplate(PictureTemplateSource.WholePicture, SKColors.Black, SKColors.White), codeSize, inkPart: ShapePart.Textured, codeClosesAreas: false, largestFilledArea: 0, codeCenter: codeCenter);

            return new ShapeStyle(new PictureShape(mask, loaded.ToDarkness(), drawOutline: false, codeBlend), detail);
        }

        /// <summary>
        /// Left half black, right half white.
        /// </summary>
        private static byte[] HalfBlack() => Png(400, 200, canvas =>
        {
            canvas.Clear(SKColors.White);
            using var black = new SKPaint { Color = SKColors.Black };
            canvas.DrawRect(0, 0, 200, 200, black);
        });

        [Test]
        public void WholePicture_IsEveryOpaquePixel()
        {
            var template = QrPicture.Load(Scene()).ToTemplate(PictureTemplateSource.WholePicture, SKColors.Black, SKColors.White);

            Assert.That(template.Cast<bool>().All(pixel => pixel), Is.True);
        }

        [Test]
        public void Darkness_FollowsTheTones_AndBrightnessLightensThem()
        {
            var picture = QrPicture.Load(HalfBlack());
            var normal = picture.ToDarkness();
            var lighter = picture.ToDarkness(brightness: 0.3);

            Assert.Multiple(() =>
            {
                Assert.That(normal[100, 50], Is.EqualTo(1).Within(0.01), "black");
                Assert.That(normal[100, 350], Is.EqualTo(0).Within(0.01), "white");
                Assert.That(lighter[100, 50], Is.EqualTo(0.7).Within(0.01), "black, 30 % lighter");
            });
        }

        [Test]
        public void Dithering_CarriesThePicturesTonesIntoTheCode()
        {
            // The code on the right half: around it, the left of the image must be mostly dark, the right mostly light.
            var bytes = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 4 }, Dithered(HalfBlack(), codeSize: 0.4, codeCenter: new ShapePoint(0.75, 0.5)));
            using var image = SKBitmap.Decode(bytes);

            double DarkShare(int fromX, int toX)
            {
                var dark = 0;
                var all = 0;

                for (var y = image.Height / 4; y < image.Height * 3 / 4; y += 2)
                {
                    for (var x = fromX; x < toX; x += 2)
                    {
                        all++;
                        dark += image.GetPixel(x, y).Red < 128 ? 1 : 0;
                    }
                }

                return (double)dark / all;
            }

            Assert.Multiple(() =>
            {
                Assert.That(DarkShare(image.Width / 10, image.Width * 4 / 10), Is.GreaterThan(0.9), "the black half");
                Assert.That(QrCodeGeneratorServiceTests.Decode(bytes), Is.EqualTo(Url));
            });
        }

        [Test]
        public void Dithering_ShowsMidTonesAsAMixOfDots()
        {
            // A mid-gray picture dithers to about half dark dots, not to solid black or white.
            var bytes = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 4 }, Dithered(Png(300, 300, canvas => canvas.Clear(new SKColor(128, 128, 128))), codeSize: 0.3, codeCenter: new ShapePoint(0.8, 0.8)));
            using var image = SKBitmap.Decode(bytes);

            var dark = 0;
            var all = 0;

            // The top-left quarter is far from the code and its gaps.
            for (var y = image.Height / 10; y < image.Height * 4 / 10; y++)
            {
                for (var x = image.Width / 10; x < image.Width * 4 / 10; x++)
                {
                    all++;
                    dark += image.GetPixel(x, y).Red < 128 ? 1 : 0;
                }
            }

            Assert.That((double)dark / all, Is.EqualTo(0.5).Within(0.1));
        }

        [Test]
        public void DitheredPicture_ScansAtEveryDetailAndPixelSize([Values(1, 2, 3, 4)] int detail)
        {
            var style = Dithered(Scene(), detail);

            var failures = new[] { 3, 4, 6, 9, 12, 16 }
                           .Where(pixels => QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = pixels }, style)) != Url)
                           .ToList();

            Assert.That(failures, Is.Empty, "pixel sizes that did not scan");
        }

        [Test]
        public void DitheredPicture_OfHostilePictures_Scans([Values] bool black)
        {
            // All black around the code is the hardest case: the gaps and checks must still give a code that scans.
            var picture = Png(300, 300, canvas => canvas.Clear(black ? SKColors.Black : SKColors.White));

            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = 6 }, Dithered(picture))), Is.EqualTo(Url));
        }

        [Test]
        public void ViewModel_DefaultsToTheWholePictureAtUltraDetail()
        {
            var viewModel = new PictureCodesViewModel(new RecordingDialogService(), new WpfPlatformService(), _service, new NullLoggingService(), new Settings());

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.SelectedSource.Source, Is.EqualTo(PictureTemplateSource.WholePicture));
                Assert.That(viewModel.SelectedDetail.CellsPerModule, Is.EqualTo(4));
                Assert.That(viewModel.UsesTones, Is.True);
                Assert.That(viewModel.UsesSensitivity, Is.False);
            });

            viewModel.SelectedSource = PictureCodesViewModel.Sources.Single(source => source.Source == PictureTemplateSource.CodeColor);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.UsesTones, Is.False);
                Assert.That(viewModel.UsesSensitivity, Is.True);
            });
        }

        private static IEnumerable<double> BlendStrengths() => PictureCodesViewModel.CodeBlends.Where(option => option.Bias is not null).Select(option => option.Bias.Value);

        [Test]
        public void BlendedCode_ScansAtEveryPixelSize([ValueSource(nameof(BlendStrengths))] double strength, [Values(3, 4)] int detail)
        {
            var style = Dithered(Scene(), detail, codeBlend: strength);

            var failures = new[] { 3, 4, 6, 9, 12, 16 }
                           .Where(pixels => QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = pixels }, style)) != Url)
                           .ToList();

            Assert.That(failures, Is.Empty, "pixel sizes that did not scan");
        }

        [Test]
        public void BlendedCode_OnHostilePictures_Scans([Values("black", "white", "noise")] string picture)
        {
            var random = new Random(3);
            var bytes = picture switch
            {
                "black" => Png(300, 300, canvas => canvas.Clear(SKColors.Black)),
                "white" => Png(300, 300, canvas => canvas.Clear(SKColors.White)),
                _ => Png(300, 300, canvas =>
                {
                    using var paint = new SKPaint();

                    for (var y = 0; y < 300; y += 2)
                    {
                        for (var x = 0; x < 300; x += 2)
                        {
                            var gray = (byte)random.Next(256);
                            paint.Color = new SKColor(gray, gray, gray);
                            canvas.DrawRect(x, y, 2, 2, paint);
                        }
                    }
                })
            };

            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = 8 }, Dithered(bytes, codeBlend: 0.15))), Is.EqualTo(Url));
        }

        [Test]
        public void BlendedCode_KeepsCentresAndFunctionPatternsExact_AndShowsThePicture()
        {
            // A black picture with the code on it: blended module borders turn dark, centres and patterns do not change.
            var code = QrMatrixEncoder.Encode(Url, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var size = code.GetLength(0);
            var fixedModules = FunctionPatterns.Mask(size);
            const int detail = 4;

            var drawing = Dithered(Png(300, 300, canvas => canvas.Clear(SKColors.Black)), detail, codeSize: 0.5, codeBlend: 0.15).Apply(code, Url);
            var cells = drawing.Cells;

            // The code sits in the middle of the cropped drawing; find its top-left by its finder pattern's size.
            var codeCells = size * detail;
            var top = (cells.GetLength(0) - codeCells) / 2;
            var left = (cells.GetLength(1) - codeCells) / 2;

            int borderDarkened = 0, borderLight = 0;

            for (var row = 0; row < size; row++)
            {
                for (var column = 0; column < size; column++)
                {
                    for (var i = 0; i < detail; i++)
                    {
                        for (var j = 0; j < detail; j++)
                        {
                            var cell = cells[top + row * detail + i, left + column * detail + j];
                            var centre = i is 1 or 2 && j is 1 or 2;

                            if (fixedModules[row, column] || centre)
                                Assert.That(cell, Is.EqualTo(code[row, column]), $"module ({row}, {column}), cell ({i}, {j}) must stay exact");
                            else if (!code[row, column])
                            {
                                borderLight++;
                                borderDarkened += cell ? 1 : 0;
                            }
                        }
                    }
                }
            }

            Assert.That((double)borderDarkened / borderLight, Is.GreaterThan(0.5), "on a black picture, light modules' borders mostly turn dark");
        }

        [Test]
        public void ResizingTheCode_FollowsTheCorner_WithinLimits()
        {
            // What dragging the corner handle does (CodePlacementOverlay), on a 400 x 300 picture, code centred.
            const int width = 400, height = 300;
            var centre = new ShapePoint(0.5, 0.5);
            const double min = PictureCodesViewModel.MinCodeSize, max = PictureCodesViewModel.MaxCodeSize;

            // Dragging the corner 25 % of the shorter side away from the centre makes a code half the shorter side.
            var dragged = CodePlacement.ResizeTo(width, height, 0.3, centre, 200 + 300 * 0.25, 150 + 300 * 0.1, min, max);
            var largest = CodePlacement.ResizeTo(width, height, 0.3, centre, 10_000, 150, min, max);
            var smallest = CodePlacement.ResizeTo(width, height, 0.3, centre, 200, 150, min, max);

            Assert.Multiple(() =>
            {
                Assert.That(dragged, Is.EqualTo(0.5).Within(0.01));
                Assert.That(largest, Is.EqualTo(max));
                Assert.That(smallest, Is.EqualTo(min));
                Assert.That(CodePlacement.HandleSize(width, height), Is.EqualTo(400 / 24.0).Within(1e-9));
            });
        }

        [Test]
        public void ViewModel_OffersBlendingOnlyWhereItCanWork()
        {
            var viewModel = new PictureCodesViewModel(new RecordingDialogService(), new WpfPlatformService(), _service, new NullLoggingService(), new Settings());
            var defaults = (viewModel.CanBlendCode, viewModel.SelectedCodeBlend.Name);

            viewModel.SelectedDetail = PictureCodesViewModel.Details.Single(detail => detail.CellsPerModule == 2);
            var fine = viewModel.CanBlendCode;

            viewModel.SelectedDetail = PictureCodesViewModel.Details[^1];
            viewModel.SelectedSource = PictureCodesViewModel.Sources.Single(source => source.Source == PictureTemplateSource.CodeColor);
            var codeColor = viewModel.CanBlendCode;

            Assert.Multiple(() =>
            {
                Assert.That(defaults, Is.EqualTo((true, "Balanced")));
                Assert.That(fine, Is.False, "Fine has no border of dots around a module's centre");
                Assert.That(codeColor, Is.False, "QR code color has no tones");
            });
        }

        [Test]
        public void NotAPicture_IsRefused()
        {
            Assert.Throws<InvalidDataException>(() => QrPicture.Load(Encoding.UTF8.GetBytes("not a picture")));
        }

        [Test]
        public async Task ViewModel_AsksForAPictureFirst_ThenUsesItAsTheTemplate()
        {
            var folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());
            var pictureFile = Path.Combine(folder, "cat.png");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(pictureFile, Cat());

            try
            {
                var dialogs = new RecordingDialogService();
                var settings = new Settings { PathsSettings = new PathsSettings { UseCustomPath = true, OutputPath = Path.Combine(folder, "out") } };
                var viewModel = new PictureCodesViewModel(dialogs, new WpfPlatformService(), _service, new NullLoggingService(), settings)
                {
                    QrCodeContent = Url,
                    UseExactPixelSize = true,
                    SelectedPixelsPerModule = 4
                };

                await viewModel.GenerateQrCodeCommand.ExecuteAsync(null);
                var warnedWithoutPicture = dialogs.Warnings.Count == 1 && viewModel.QrCodeImage is null;

                viewModel.LoadPicture(pictureFile);
                var firstPreview = viewModel.TemplatePreview;

                await viewModel.GenerateQrCodeCommand.ExecuteAsync(null);
                var saved = Directory.GetFiles(Path.Combine(folder, "out")).Single();
                using var image = SKBitmap.Decode(saved);

                // The ink follows the QR code color, so a new color makes a new template.
                viewModel.SelectedForegroundColor = System.Drawing.Color.DarkRed;

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
                var viewModel = new PictureCodesViewModel(dialogs, new WpfPlatformService(), _service, new NullLoggingService(), new Settings());

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

            public Task<string> OpenFolderDialogAsync(string initialDirectory = null) => Task.FromResult(string.Empty);

            public Task<string> OpenFileDialogAsync(string filter = null) => Task.FromResult(string.Empty);

            public void ShowWarnMessageBox(string message) => Warnings.Add(message);

            public void ShowErrorMessageBox(string message) => Errors.Add(message);
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
