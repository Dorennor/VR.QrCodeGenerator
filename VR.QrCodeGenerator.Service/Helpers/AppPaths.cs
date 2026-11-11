using System.IO;

namespace VR.QrCodeGenerator.Service.Helpers
{
    /// <summary>
    /// Settings and logs live in %LOCALAPPDATA%\VR.QrCodeGenerator, so the published app is a single .exe
    /// that can sit anywhere, including read-only folders.
    /// </summary>
    public static class AppPaths
    {
        public static string Root { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VR.QrCodeGenerator");

        public static string SettingsFile => Path.Combine(Root, "settings.json");

        public static string LogsDirectory => Path.Combine(Root, "Logs");

        /// <summary>
        /// Where older versions kept their settings; read once if settings.json does not exist yet.
        /// </summary>
        public static string LegacySettingsFile => Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }
}
