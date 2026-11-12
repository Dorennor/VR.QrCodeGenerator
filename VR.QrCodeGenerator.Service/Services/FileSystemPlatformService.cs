using System.Diagnostics;
using System.IO;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    /// <summary>
    /// Windows: files go to a folder on disk (<see cref="OutputHelper"/>) and folders open in Explorer. The clipboard
    /// belongs to the UI framework, so the WPF app overrides <see cref="CopyImageAsync"/>.
    /// </summary>
    public class FileSystemPlatformService : IPlatformService
    {
        public virtual bool CanShare => false;

        public string GetOutputFolder(PathsSettings settings) => OutputHelper.GetOutputFolder(settings);

        public string SaveToOutputFolder(byte[] content, EnumImageFormat format, PathsSettings settings) =>
            OutputHelper.SaveToFile(content, OutputHelper.GetOutputFolder(settings), format);

        public Task OpenOutputFolderAsync(PathsSettings settings)
        {
            OpenInExplorer(OutputHelper.GetOutputFolder(settings));
            return Task.CompletedTask;
        }

        public Task OpenLogsAsync(string logsFolder)
        {
            OpenInExplorer(logsFolder);
            return Task.CompletedTask;
        }

        public virtual Task CopyImageAsync(byte[] png) => throw new NotSupportedException("Copying images is not supported here.");

        public virtual Task ShareImageAsync(byte[] png) => throw new NotSupportedException("Sharing is not supported here.");

        private static void OpenInExplorer(string folder)
        {
            Directory.CreateDirectory(folder);

            // The quotes matter: without them a path with spaces opens the wrong folder.
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        }
    }
}