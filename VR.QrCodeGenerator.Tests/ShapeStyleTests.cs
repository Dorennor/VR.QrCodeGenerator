using System.Text;
using System.Xml.Linq;

using QRCoder;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.Tests
{
    /// <summary>
    /// Custom styles (Kurkod, cat, dog): every one must still scan, whatever the content and settings.
    /// </summary>
    [TestFixture]
    public class ShapeStyleTests
    {
        private const string Url = "https://example.com/hello?a=1&b=2";

        private static readonly EnumQrStyle[] CustomStyles = Enum.GetValues<EnumQrStyle>().Where(style => style != EnumQrStyle.Classic).ToArray();

        private readonly QrCodeGeneratorService _service = new QrCodeGeneratorService();

        private static QrCodeOptions Options(EnumQrStyle style, int? pixelsPerModule = 8) => new QrCodeOptions { Style = style, PixelsPerModule = pixelsPerModule };

        private static string Decode(byte[] image) => QrCodeGeneratorServiceTests.Decode(image);

        [Test]
        public void Png_Scans(
            [ValueSource(nameof(CustomStyles))] EnumQrStyle style,
            [Values("Hi", Url, "https://github.com/Dorennor/VR.QrCodeGenerator", "Привіт, світе! Це український текст.", "emoji 🎯 text")] string content)
        {
            Assert.That(Decode(_service.Generate(content, Options(style))), Is.EqualTo(content));
        }

        [Test]
        public void EveryErrorCorrectionLevel_Scans([ValueSource(nameof(CustomStyles))] EnumQrStyle style, [Values] QRCodeGenerator.ECCLevel level)
        {
            var options = Options(style);
            options.EccLevel = level;

            Assert.That(Decode(_service.Generate(Url, options)), Is.EqualTo(Url));
        }

        [Test]
        public void ManyContents_AllScan([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            var failures = Enumerable.Range(0, 40)
                                     .Select(i => i % 2 == 0 ? $"https://example.com/{i}?q={i * 7919}" : new string((char)('a' + i % 26), 5 + i * 3))
                                     .Where(content => Decode(_service.Generate(content, Options(style, 4))) != content)
                                     .ToList();

            Assert.That(failures, Is.Empty);
        }

        [Test]
        public void ScansAtEveryPixelSize([ValueSource(nameof(CustomStyles))] EnumQrStyle style, [Values(2, 4, 6, 8, 12)] int pixelsPerModule)
        {
            // This content once drew a tail texture that only broke decoding at 6 px per module and more.
            var content = new string('z', 158);

            Assert.That(Decode(_service.Generate(content, Options(style, pixelsPerModule))), Is.EqualTo(content));
        }

        [Test]
        public void LongContent_Scans([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            var content = "https://example.com/" + new string('a', 300);

            Assert.That(Decode(_service.Generate(content, Options(style, 4))), Is.EqualTo(content));
        }

        [Test]
        public void Jpg_Scans([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            Assert.That(Decode(_service.Generate(Url, Options(style), EnumImageFormat.Jpg)), Is.EqualTo(Url));
        }

        [Test]
        public void IsBiggerThanThePlainCode([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            using var plain = SKBitmap.Decode(_service.Generate(Url, Options(EnumQrStyle.Classic)));
            using var shaped = SKBitmap.Decode(_service.Generate(Url, Options(style)));

            Assert.Multiple(() =>
            {
                Assert.That(shaped.Width, Is.GreaterThan(plain.Width));
                Assert.That(shaped.Height, Is.GreaterThan(plain.Height));
            });
        }

        [Test]
        public void SameContent_GivesTheSameDrawing([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            // The preview and the saved file are generated separately, so they must match exactly.
            Assert.That(_service.Generate(Url, Options(style)), Is.EqualTo(_service.Generate(Url, Options(style))));
        }

        [Test]
        public void FitsTheRequestedSizeAndScans([ValueSource(nameof(CustomStyles))] EnumQrStyle style, [Values(500, 800)] int width, [Values(300, 500)] int height)
        {
            var options = Options(style, null);
            options.Width = width;
            options.Height = height;

            var bytes = _service.Generate(Url, options);
            using var bitmap = SKBitmap.Decode(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(width));
                Assert.That(bitmap.Height, Is.EqualTo(height));
                Assert.That(Decode(bytes), Is.EqualTo(Url));
            });
        }

        [Test]
        public void Svg_HasTheSameSizeAsPng([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            using var png = SKBitmap.Decode(_service.Generate(Url, Options(style)));
            var svg = XDocument.Parse(Encoding.UTF8.GetString(_service.Generate(Url, Options(style), EnumImageFormat.Svg))).Root!;

            Assert.Multiple(() =>
            {
                Assert.That(svg.Attribute("width")!.Value, Is.EqualTo(png.Width.ToString()));
                Assert.That(svg.Attribute("height")!.Value, Is.EqualTo(png.Height.ToString()));
            });
        }

        [Test]
        public void EveryStyle_IsRegistered([Values] EnumQrStyle style)
        {
            Assert.That(QrStyles.Get(style).Style, Is.EqualTo(style));
        }

        [Test]
        public void EveryCustomStyle_IsOfferedOnTheCustomCodesPage([ValueSource(nameof(CustomStyles))] EnumQrStyle style)
        {
            Assert.That(CustomCodesViewModel.Styles.Select(option => option.Style), Does.Contain(style));
        }

        [Test]
        public void StyleMadeAtRuntime_IsUsedAndScans()
        {
            // How a style from a drawing will plug in: any IShape, wrapped in a ShapeStyle, passed straight to Generate.
            var style = new ShapeStyle(EnumQrStyle.Classic, new FrameShape());

            var bytes = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = 8 }, style);
            using var plain = SKBitmap.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = 8 }));
            using var framed = SKBitmap.Decode(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(framed.Width, Is.GreaterThan(plain.Width));
                Assert.That(Decode(bytes), Is.EqualTo(Url));
            });
        }

        /// <summary>
        /// A solid frame around the code, a stand-in for a shape that comes from a picture.
        /// </summary>
        private sealed class FrameShape : IShape
        {
            public ShapeMargins Margins { get; } = new(0.2, 0.2, 0.2, 0.2);

            public IReadOnlyList<ShapePoint> Eyes { get; } = [];

            public ShapePart Classify(double u, double v, int size) =>
                u < -0.1 || v < -0.1 || u > 1.1 || v > 1.1 ? ShapePart.Solid : ShapePart.None;
        }
    }
}
