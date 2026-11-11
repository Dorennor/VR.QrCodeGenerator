using System.IO;
using System.Text.Json;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    public class SettingsService : ISettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _legacySettingsFilePath;

        public SettingsService()
            : this(AppPaths.SettingsFile, AppPaths.LegacySettingsFile)
        {
        }

        public SettingsService(string settingsFilePath, string legacySettingsFilePath = null)
        {
            SettingsFilePath = settingsFilePath;
            _legacySettingsFilePath = legacySettingsFilePath;
        }

        public string SettingsFilePath { get; }

        public Settings Load()
        {
            // An appsettings.json beside the exe from an older version is picked up until settings are saved once.
            var path = File.Exists(SettingsFilePath) ? SettingsFilePath : _legacySettingsFilePath;

            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), JsonOptions) ?? new Settings();

                    settings.LoggingSettings ??= new LoggingSettings();
                    settings.PathsSettings ??= new PathsSettings();
                    settings.StyleSettings ??= new StyleSettings();

                    return settings;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // A damaged or unreadable file must not stop the app from starting; the defaults apply.
            }

            return new Settings();
        }

        public void Save(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath));

            var json = JsonSerializer.Serialize(settings, JsonOptions);

            // Write to a temporary file first so a failed write never leaves a half-written settings file.
            var temporaryPath = SettingsFilePath + ".tmp";

            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, SettingsFilePath, overwrite: true);
        }
    }
}
