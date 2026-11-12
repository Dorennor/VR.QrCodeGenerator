using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    /// <summary>
    /// What the app does differently on each platform: where files go and how they are opened, copied and shared.
    /// Windows: <see cref="Services.FileSystemPlatformService"/> (and the WPF app's clipboard). Android: the MAUI app.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Whether <see cref="ShareImageAsync"/> works here; the page shows a Share button only then.
        /// </summary>
        bool CanShare { get; }

        /// <summary>
        /// Where QR codes are saved with these settings, as shown to the user.
        /// </summary>
        string GetOutputFolder(PathsSettings settings);

        /// <summary>
        /// Writes a new file to the output folder and returns where it went, as shown to the user. Called off the UI
        /// thread.
        /// </summary>
        string SaveToOutputFolder(byte[] content, EnumImageFormat format, PathsSettings settings);

        /// <summary>
        /// Shows the output folder (Windows) or the last saved code (Android).
        /// </summary>
        Task OpenOutputFolderAsync(PathsSettings settings);

        /// <summary>
        /// Shows the logs folder (Windows) or shares the latest log file (Android).
        /// </summary>
        Task OpenLogsAsync(string logsFolder);

        Task CopyImageAsync(byte[] png);

        Task ShareImageAsync(byte[] png);
    }
}