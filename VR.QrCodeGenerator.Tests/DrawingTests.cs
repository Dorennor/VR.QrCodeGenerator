using System.IO;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.Tests
{
    /// <summary>
    /// QR codes shaped like a drawing (<see cref="MaskShape"/>): pen lines solid, enclosed areas textured, always scans.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DrawingTests
    {
        private const string Url = "https://github.com/Dorennor/VR.QrCodeGenerator";

        // The canvas size and pen of the Drawing page.
        private const int CanvasSize = 360;
        private const double PenRadius = 5;
        private const double CodeFraction = DrawingCodesViewModel.CodeFraction;

        private readonly QrCodeGeneratorService _service = new QrCodeGeneratorService();

        /// <summary>
        /// A test drawing: what the pen covers, as on the canvas.
        /// </summary>
        private sealed class Sketch
        {
            public bool[,] Pixels { get; } = new bool[CanvasSize, CanvasSize];

            public Sketch Line(double x0, double y0, double x1, double y1)
            {
                var steps = (int)Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) + 1;

                for (var i = 0; i <= steps; i++)
                    Dot(x0 + (x1 - x0) * i / steps, y0 + (y1 - y0) * i / steps);

                return this;
            }

            /// <param name="gapDegrees">An opening left in the outline, centred on the right-hand side.</param>
            public Sketch Ellipse(double cx, double cy, double rx, double ry, double gapDegrees = 0)
            {
                for (var a = gapDegrees / 2; a < 360 - gapDegrees / 2; a += 1)
                {
                    var b = a + 1;
                    Line(cx + rx * Math.Cos(a * Math.PI / 180), cy + ry * Math.Sin(a * Math.PI / 180),
                         cx + rx * Math.Cos(b * Math.PI / 180), cy + ry * Math.Sin(b * Math.PI / 180));
                }

                return this;
            }

            private void Dot(double cx, double cy)
            {
                for (var y = (int)(cy - PenRadius); y <= cy + PenRadius; y++)
                {
                    for (var x = (int)(cx - PenRadius); x <= cx + PenRadius; x++)
                    {
                        if (x >= 0 && y >= 0 && x < CanvasSize && y < CanvasSize && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= PenRadius * PenRadius)
                            Pixels[y, x] = true;
                    }
                }
            }
        }

        private static MaskShape Shape(Sketch sketch) => new MaskShape(sketch.Pixels, CodeFraction);

        // Converts a canvas pixel to the shape's code-width coordinates.
        private static (double U, double V) ToCode(double x, double y)
        {
            var side = CodeFraction * CanvasSize;
            var offset = (CanvasSize - side) / 2;

            return ((x - offset) / side, (y - offset) / side);
        }

        private static ShapePart PartAt(MaskShape shape, double x, double y, int size = 29)
        {
            var (u, v) = ToCode(x, y);

            return shape.Classify(u, v, size);
        }

        [Test]
        public void ClosedOutline_IsFilledWithTexture_AndTheLineIsSolid()
        {
            var shape = Shape(new Sketch().Ellipse(180, 180, 150, 150));

            Assert.Multiple(() =>
            {
                Assert.That(PartAt(shape, 180, 50), Is.EqualTo(ShapePart.Textured), "inside the circle, above the code");
                Assert.That(PartAt(shape, 180, 30), Is.EqualTo(ShapePart.Solid), "on the line");
                Assert.That(PartAt(shape, 10, 10), Is.EqualTo(ShapePart.None), "outside");
            });
        }

        [Test]
        public void OpenLine_StaysALine()
        {
            var shape = Shape(new Sketch().Line(20, 20, 100, 60));

            Assert.Multiple(() =>
            {
                Assert.That(PartAt(shape, 60, 40), Is.EqualTo(ShapePart.Solid));
                Assert.That(PartAt(shape, 40, 60), Is.EqualTo(ShapePart.None), "beside the line");
            });
        }

        [Test]
        public void SmallGapInOutline_IsClosed_ButAWideOneIsNot()
        {
            // Ears above the code: 4 degrees open is a slip of the pen, 60 degrees is a "C".
            var shape = Shape(new Sketch().Ellipse(90, 60, 35, 45, gapDegrees: 4).Ellipse(270, 60, 35, 45, gapDegrees: 60));

            Assert.Multiple(() =>
            {
                Assert.That(PartAt(shape, 90, 60), Is.EqualTo(ShapePart.Textured));
                Assert.That(PartAt(shape, 260, 60), Is.EqualTo(ShapePart.None));
            });
        }

        [Test]
        public void HeadDrawnOnTopOfTheCode_IsFilled()
        {
            // An arch that starts and ends on the code's square: the code closes it.
            var sketch = new Sketch();
            var side = CodeFraction * CanvasSize;
            var top = (CanvasSize - side) / 2;
            var left = top;

            sketch.Line(left + 10, top + 2, left + 10, top - 60).Line(left + 10, top - 60, left + side - 10, top - 60).Line(left + side - 10, top - 60, left + side - 10, top + 2);

            Assert.That(PartAt(Shape(sketch), 180, top - 30), Is.EqualTo(ShapePart.Textured));
        }

        [Test]
        public void Drawing_ScansAndIsBiggerThanThePlainCode([Values(4, 8)] int pixelsPerModule)
        {
            var style = new ShapeStyle(Shape(new Sketch().Ellipse(180, 190, 160, 150).Ellipse(120, 45, 25, 40).Ellipse(240, 45, 25, 40).Line(330, 200, 355, 150)));
            var options = new QrCodeOptions { PixelsPerModule = pixelsPerModule };

            var bytes = _service.Generate(Url, options, style);
            using var drawing = SKBitmap.Decode(bytes);
            using var plain = SKBitmap.Decode(_service.Generate(Url, options));

            Assert.Multiple(() =>
            {
                Assert.That(QrCodeGeneratorServiceTests.Decode(bytes), Is.EqualTo(Url));
                Assert.That(drawing.Width, Is.GreaterThan(plain.Width));
                Assert.That(drawing.Height, Is.GreaterThan(plain.Height));
            });
        }

        [Test]
        public void DrawingAcrossTheWholeCanvas_StillScans()
        {
            // Scribbles everywhere, including over the code and next to its finder patterns.
            var sketch = new Sketch();

            for (var i = 0; i < CanvasSize; i += 24)
                sketch.Line(i, 0, CanvasSize - i, CanvasSize).Line(0, i, CanvasSize, CanvasSize - i);

            var bytes = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 6 }, new ShapeStyle(Shape(sketch)));

            Assert.That(QrCodeGeneratorServiceTests.Decode(bytes), Is.EqualTo(Url));
        }

        [Test]
        public void EmptyCanvas_GivesThePlainCode()
        {
            var options = new QrCodeOptions { PixelsPerModule = 4 };

            var drawn = _service.Generate(Url, options, new ShapeStyle(Shape(new Sketch())));

            Assert.That(drawn, Is.EqualTo(_service.Generate(Url, options)));
        }

        [Test]
        public async Task ViewModel_UsesTheDrawingAndSavesWhatThePreviewShows()
        {
            var folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());

            try
            {
                var settings = new Settings { PathsSettings = new PathsSettings { UseCustomPath = true, OutputPath = folder } };
                var viewModel = new DrawingCodesViewModel(new NullDialogService(), _service, new NullLoggingService(), settings)
                {
                    QrCodeContent = Url,
                    UseExactPixelSize = true,
                    SelectedPixelsPerModule = 4,
                    Drawing = new Sketch().Ellipse(180, 180, 160, 160).Pixels
                };

                await viewModel.GenerateQrCodeCommand.ExecuteAsync(null);
                var previewWidth = viewModel.QrCodeImage.PixelWidth;

                // Changing the drawing after Generate must not change what Save writes.
                viewModel.Drawing = null;
                await viewModel.SaveQrCodeCommand.ExecuteAsync(null);

                var files = Directory.GetFiles(folder);

                Assert.Multiple(() =>
                {
                    Assert.That(viewModel.ShowsStyles, Is.False);
                    Assert.That(files, Has.Length.EqualTo(2), "Generate saves once, Save once more");
                    Assert.That(files.Select(file => SKBitmap.Decode(file).Width), Is.All.EqualTo(previewWidth));
                    Assert.That(files.Select(file => QrCodeGeneratorServiceTests.Decode(File.ReadAllBytes(file))), Is.All.EqualTo(Url));
                });
            }
            finally
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
        }

        private sealed class NullDialogService : IDialogService
        {
            public string OpenFolderDialog(string initialDirectory = null) => string.Empty;

            public string OpenFileDialog(string filter = null) => string.Empty;

            public string SaveFileDialog(string defaultFileName) => string.Empty;

            public void ShowWarnMessageBox(string message) => Assert.Fail($"Unexpected warning: {message}");

            public void ShowErrorMessageBox(string message) => Assert.Fail($"Unexpected error: {message}");

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
