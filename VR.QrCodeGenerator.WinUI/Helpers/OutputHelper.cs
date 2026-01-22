using System;
using System.IO;
using System.Threading.Tasks;

using VR.QrCodeGenerator.WinUI.Enums;

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Streams;

namespace VR.QrCodeGenerator.WinUI.Helpers
{
    public static class OutputHelper
    {
        public static async Task<string> SaveToFile(byte[] qrCode, string outputPath = null, EnumImageFormat imageFormat = EnumImageFormat.Png)
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

            await File.WriteAllBytesAsync(fullPath, qrCode);
            return fullPath;
        }

        public static async Task CopyQrToClipboard(byte[] imageBytes, string filePath)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            var imageFile = await StorageFile.GetFileFromPathAsync(filePath);

            var dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };

            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromFile(imageFile));

            Clipboard.SetContent(dataPackage);
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