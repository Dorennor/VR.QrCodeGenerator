using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;

namespace VR.QrCodeGenerator.Service.Helpers
{
    public static class OutputHelper
    {
        /// <summary>
        /// The custom folder when one is configured, otherwise "Output" beside the executable.
        /// </summary>
        public static string GetOutputFolder(PathsSettings settings)
        {
            if (settings is { UseCustomPath: true } && !string.IsNullOrWhiteSpace(settings.OutputPath))
                return settings.OutputPath;

            return Path.Combine(AppContext.BaseDirectory, "Output");
        }

        public static string SaveToFile(byte[] fileContent, string outputFolder, EnumImageFormat imageFormat)
        {
            Directory.CreateDirectory(outputFolder);

            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{GetExtension(imageFormat)}";
            var fullPath = Path.Combine(outputFolder, fileName);

            File.WriteAllBytes(fullPath, fileContent);

            return fullPath;
        }

        public static void CopyQrToClipboard(byte[] pngData)
        {
            var convertedBitmap = new FormatConvertedBitmap(ImageHelper.LoadBitmap(pngData), PixelFormats.Bgra32, null, 0);

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

        private static string GetExtension(EnumImageFormat imageFormat)
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
