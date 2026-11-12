using System.IO;

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

            var fullPath = Path.Combine(outputFolder, CreateFileName(imageFormat));

            File.WriteAllBytes(fullPath, fileContent);

            return fullPath;
        }

        /// <summary>
        /// A new, unique file name such as 20261112_215520_&lt;guid&gt;.png.
        /// </summary>
        public static string CreateFileName(EnumImageFormat imageFormat) => $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{GetExtension(imageFormat)}";

        public static string GetExtension(EnumImageFormat imageFormat)
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

        public static string GetMimeType(EnumImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case EnumImageFormat.Jpg:
                    return "image/jpeg";

                case EnumImageFormat.Png:
                    return "image/png";

                case EnumImageFormat.Svg:
                    return "image/svg+xml";

                default:
                    throw new ArgumentOutOfRangeException(nameof(imageFormat), "Image type type is not supported!");
            }
        }
    }
}