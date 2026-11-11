using VR.QrCodeGenerator.Model.Models.Settings;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface ISettingsService
    {
        /// <summary>
        /// Full path of the settings file (%LOCALAPPDATA%\VR.QrCodeGenerator\settings.json).
        /// </summary>
        string SettingsFilePath { get; }

        /// <summary>
        /// The saved settings, or the defaults when nothing was saved yet or the file cannot be read.
        /// </summary>
        Settings Load();

        /// <summary>
        /// Writes the settings to the settings file, replacing its contents.
        /// </summary>
        void Save(Settings settings);
    }
}
