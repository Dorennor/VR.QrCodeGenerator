using System.Text;
using System.Xml.Linq;

using QRCoder;
using QRCoder.Exceptions;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Exceptions;
using VR.QrCodeGenerator.Service.Services;

using ZXing;
using ZXing.Common;

namespace VR.QrCodeGenerator.Tests
{
    [TestFixture]
    public class QrCodeGeneratorServiceTests
    {
        private const string Url = "https://example.com/hello?a=1&b=2";

        private readonly QrCodeGeneratorService _service = new QrCodeGeneratorService();

        internal static string Decode(byte[] image)
        {
            using var decoded = SKBitmap.Decode(image);
            using var bitmap = decoded.Copy(SKColorType.Bgra8888);

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = false,
                Options = new DecodingOptions { PossibleFormats = [BarcodeFormat.QR_CODE], TryHarder = true }
            };

            return reader.Decode(new RGBLuminanceSource(bitmap.Bytes, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.BGRA32))?.Text;
        }

        [Test]
        public void Png_IsRealPngAndScans()
        {
            var bytes = _service.Generate(Url, new QrCodeOptions(), EnumImageFormat.Png);

            Assert.That(bytes.Take(4), Is.EqualTo(new byte[] { 0x89, 0x50, 0x4E, 0x47 }));
            Assert.That(Decode(bytes), Is.EqualTo(Url));
        }

        [Test]
        public void Jpg_IsRealJpegAndScans()
        {
            var bytes = _service.Generate(Url, new QrCodeOptions(), EnumImageFormat.Jpg);

            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xFF, 0xD8, 0xFF }));
            Assert.That(Decode(bytes), Is.EqualTo(Url));
        }

        [Test]
        public void Svg_IsVectorWithColorsAndSize()
        {
            var options = new QrCodeOptions { Width = 300, Height = 300, ForegroundColor = [0x12, 0x34, 0x56], BackgroundColor = [0xFE, 0xDC, 0xBA] };

            var text = Encoding.UTF8.GetString(_service.Generate(Url, options, EnumImageFormat.Svg));
            var svg = XDocument.Parse(text).Root;
            XNamespace ns = "http://www.w3.org/2000/svg";

            Assert.Multiple(() =>
            {
                Assert.That(svg.Name, Is.EqualTo(ns + "svg"));
                Assert.That(svg.Attribute("width").Value, Is.EqualTo("300"));
                Assert.That(svg.Descendants(ns + "image"), Is.Empty, "must not embed a bitmap");
                Assert.That(svg.Element(ns + "path").Attribute("fill").Value, Is.EqualTo("#123456"));
                Assert.That(svg.Element(ns + "rect").Attribute("fill").Value, Is.EqualTo("#FEDCBA"));
            });
        }

        [TestCase(500, 500)]
        [TestCase(400, 300)]
        [TestCase(300, 700)]
        public void Raster_HasRequestedSizeAndScans(int width, int height)
        {
            var bytes = _service.Generate(Url, new QrCodeOptions { Width = width, Height = height });

            using var bitmap = SKBitmap.Decode(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(width));
                Assert.That(bitmap.Height, Is.EqualTo(height));
                Assert.That(Decode(bytes), Is.EqualTo(Url));
            });
        }

        [Test]
        public void Raster_HasOnlyTheTwoColors()
        {
            var bytes = _service.Generate(Url, new QrCodeOptions { Width = 317, Height = 317 });

            using var bitmap = SKBitmap.Decode(bytes);

            Assert.That(bitmap.Pixels.Distinct().Count(), Is.EqualTo(2), "resizing must not blur the squares");
            Assert.That(Decode(bytes), Is.EqualTo(Url));
        }

        [TestCase(3)]
        [TestCase(7)]
        [TestCase(20)]
        public void ExactPixelSize_EverySquareIsExactlyThatManyPixels(int pixels)
        {
            // Width and Height are too small on purpose: they must be ignored.
            var bytes = _service.Generate(Url, new QrCodeOptions { PixelsPerModule = pixels, Width = 10, Height = 10 });

            using var bitmap = SKBitmap.Decode(bytes);

            var modules = bitmap.Width / pixels;

            Assert.Multiple(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(bitmap.Height));
                Assert.That(bitmap.Width % pixels, Is.Zero);
                Assert.That(modules, Is.GreaterThanOrEqualTo(29), "smallest QR code with its quiet zone");
                Assert.That(Decode(bytes), Is.EqualTo(Url));
            });

            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    if (bitmap.GetPixel(x, y) != bitmap.GetPixel(x - x % pixels, y - y % pixels))
                        Assert.Fail($"pixel {x},{y} differs from the rest of its square");
                }
            }
        }

        [Test]
        public void ExactPixelSize_SvgGetsTheSameSizeAsPng()
        {
            var options = new QrCodeOptions { PixelsPerModule = 7 };

            using var bitmap = SKBitmap.Decode(_service.Generate(Url, options));

            var svg = XDocument.Parse(Encoding.UTF8.GetString(_service.Generate(Url, options, EnumImageFormat.Svg))).Root;

            Assert.That(svg.Attribute("width").Value, Is.EqualTo(bitmap.Width.ToString()));
        }

        [TestCase("Привіт, світе! Це український текст.")]
        [TestCase("Ünïcödé café")]
        [TestCase("日本語のテキスト")]
        [TestCase("emoji 🎯 text")]
        public void NonAsciiText_RoundTrips(string text)
        {
            Assert.That(Decode(_service.Generate(text, new QrCodeOptions())), Is.EqualTo(text));
        }

        [Test]
        public void TooLongContent_Throws()
        {
            Assert.Throws<DataTooLongException>(() => _service.Generate(new string('x', 5000), new QrCodeOptions { EccLevel = QRCodeGenerator.ECCLevel.H }));
        }

        [TestCase(EnumImageFormat.Png)]
        [TestCase(EnumImageFormat.Jpg)]
        [TestCase(EnumImageFormat.Svg)]
        public void TooSmallImage_Throws(EnumImageFormat format)
        {
            var exception = Assert.Throws<QrCodeTooSmallException>(() => _service.Generate(Url, new QrCodeOptions { Width = 10, Height = 10 }, format));

            Assert.That(exception.MinimumSize, Is.GreaterThan(10));
        }
    }
}