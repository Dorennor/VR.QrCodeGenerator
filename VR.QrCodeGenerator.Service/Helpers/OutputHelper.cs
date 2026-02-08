using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.Service.Helpers
{
    public static class OutputHelper
    {
        public static string SaveToFile(byte[] qrCode, string outputPath = null, EnumImageFormat imageFormat = EnumImageFormat.Png)
        {
            if (!string.IsNullOrEmpty(outputPath))
            {
                if (!Directory.Exists(outputPath))
                    throw new DirectoryNotFoundException("Folder does not exist.");
            }
            else
            {
                outputPath = Path.Combine(Directory.GetCurrentDirectory(), "Output");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
            }

            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{ConvertImageType(imageFormat)}";
            string fullPath = Path.Combine(outputPath, fileName);

            if (imageFormat == EnumImageFormat.Svg)
            {
                string base64Data = Convert.ToBase64String(qrCode);

                XNamespace xNamespace = "http://www.w3.org/2000/svg";

                var xDocument = new XDocument(
                    new XDeclaration("1.0", "utf-8", "no"),
                    new XElement(xNamespace + "svg",
                        new XAttribute("version", "1.1"),
                        new XAttribute("width", "100%"),
                        new XAttribute("height", "100%"),
                        new XElement(xNamespace + "image",
                            new XAttribute("href", $"data:image/png;base64,{base64Data}"),
                            new XAttribute("width", "100%"),
                            new XAttribute("height", "100%")
                        )
                    )
                );

                xDocument.Save(fullPath);

                return fullPath;
            }

            File.WriteAllBytes(fullPath, qrCode);
            return fullPath;
        }

        public static void CopyQrToClipboard(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                return;

            var bitmap = new BitmapImage();

            bitmap.BeginInit();

            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            bitmap.EndInit();

            if (bitmap.IsDownloading)
                return;

            var convertedBitmap = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Clipboard.SetImage(convertedBitmap);
                    return;
                }
                catch (COMException)
                {
                    if (i == 2)
                        throw;

                    Thread.Sleep(50);
                }
            }
        }

        private static string ConvertImageType(EnumImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case EnumImageFormat.Jpg:
                    return ".jpg";

                case EnumImageFormat.Png:
                    return ".png";

                case EnumImageFormat.Svg:
                    return ".svg";

                default:
                    throw new ArgumentOutOfRangeException(nameof(imageFormat), "Image type type is not supported!");
            }
        }
    }
}