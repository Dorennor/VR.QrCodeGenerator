using System.Text;
using System.Xml.Linq;

using QRCoder;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Services;

namespace VR.QrCodeGenerator.Tests
{
    /// <summary>
    /// Chicken-shaped QR codes ("Kurkod"): they must still scan, whatever the content and settings.
    /// </summary>
    [TestFixture]
    public class KurkodTests
    {
        private const string Url = "https://example.com/hello?a=1&b=2";

        private readonly QrCodeGeneratorService _service = new QrCodeGeneratorService();

        private static QrCodeOptions Chicken(int? pixelsPerModule = 8) => new QrCodeOptions { ChickenShape = true, PixelsPerModule = pixelsPerModule };

        [TestCase("Hi")]
        [TestCase(Url)]
        [TestCase("https://github.com/Dorennor/VR.QrCodeGenerator")]
        [TestCase("Привіт, світе! Це український текст.")]
        [TestCase("emoji 🎯 text")]
        public void Png_Scans(string content)
        {
            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(content, Chicken())), Is.EqualTo(content));
        }

        [TestCase(QRCodeGenerator.ECCLevel.L)]
        [TestCase(QRCodeGenerator.ECCLevel.M)]
        [TestCase(QRCodeGenerator.ECCLevel.Q)]
        [TestCase(QRCodeGenerator.ECCLevel.H)]
        public void EveryErrorCorrectionLevel_Scans(QRCodeGenerator.ECCLevel level)
        {
            var options = Chicken();
            options.EccLevel = level;

            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, options)), Is.EqualTo(Url));
        }

        [Test]
        public void LongContent_Scans()
        {
            var content = "https://example.com/" + new string('a', 300);

            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(content, Chicken(4))), Is.EqualTo(content));
        }

        [Test]
        public void Jpg_Scans()
        {
            Assert.That(QrCodeGeneratorServiceTests.Decode(_service.Generate(Url, Chicken(), EnumImageFormat.Jpg)), Is.EqualTo(Url));
        }

        [Test]
        public void IsWiderAndTallerThanThePlainCode()
        {
            using var plain = SKBitmap.Decode(_service.Generate(Url, new QrCodeOptions { PixelsPerModule = 8 }));
            using var chicken = SKBitmap.Decode(_service.Generate(Url, Chicken()));

            Assert.Multiple(() =>
            {
                Assert.That(chicken.Width, Is.GreaterThan(plain.Width));
                Assert.That(chicken.Height, Is.GreaterThan(plain.Height));
                Assert.That(chicken.Width, Is.Not.EqualTo(chicken.Height), "a chicken is not square");
            });
        }

        [Test]
        public void SameContent_GivesTheSameChicken()
        {
            // The preview and the saved file are generated separately, so they must match exactly.
            Assert.That(_service.Generate(Url, Chicken()), Is.EqualTo(_service.Generate(Url, Chicken())));
        }

        [TestCase(500, 500)]
        [TestCase(800, 300)]
        public void FitsTheRequestedSizeAndScans(int width, int height)
        {
            var bytes = _service.Generate(Url, Chicken(null).With(width, height));
            using var bitmap = SKBitmap.Decode(bytes);

            Assert.Multiple(() =>
            {
                Assert.That(bitmap.Width, Is.EqualTo(width));
                Assert.That(bitmap.Height, Is.EqualTo(height));
                Assert.That(QrCodeGeneratorServiceTests.Decode(bytes), Is.EqualTo(Url));
            });
        }

        [Test]
        public void Svg_HasTheSameSizeAsPng()
        {
            using var png = SKBitmap.Decode(_service.Generate(Url, Chicken()));
            var svg = XDocument.Parse(Encoding.UTF8.GetString(_service.Generate(Url, Chicken(), EnumImageFormat.Svg))).Root!;

            Assert.Multiple(() =>
            {
                Assert.That(svg.Attribute("width")!.Value, Is.EqualTo(png.Width.ToString()));
                Assert.That(svg.Attribute("height")!.Value, Is.EqualTo(png.Height.ToString()));
            });
        }
    }

    internal static class QrCodeOptionsExtensions
    {
        public static QrCodeOptions With(this QrCodeOptions options, int width, int height)
        {
            options.Width = width;
            options.Height = height;
            return options;
        }
    }
}
