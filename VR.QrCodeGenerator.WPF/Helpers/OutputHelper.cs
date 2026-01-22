using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using VR.QrCodeGenerator.WPF.Enums;

namespace VR.QrCodeGenerator.WPF.Helpers
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
                string exeLocation = Environment.ProcessPath;
                string exeDirectory = Path.GetDirectoryName(exeLocation);

                outputPath = Path.Combine(exeDirectory, "Output");

                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);
            }

            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{ConvertImageType(imageFormat)}";
            string fullPath = Path.Combine(outputPath, fileName);

            File.WriteAllBytes(fullPath, qrCode);
            return fullPath;
        }

        public static void CopyQrToClipboard(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            BitmapImage bitmap = new BitmapImage();

            bitmap.BeginInit();

            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            bitmap.EndInit();

            Clipboard.SetImage(bitmap);
            Clipboard.Flush();
        }

        private static string ConvertImageType(EnumImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case EnumImageFormat.Jpg:
                    return ".jpg";

                case EnumImageFormat.Png:
                    return ".png";

                //case EnumImageFormat.Svg:
                //    return ".svg";

                default:
                    throw new ArgumentOutOfRangeException(nameof(imageFormat), "Image type type is not supported!");
            }
        }
    }
}