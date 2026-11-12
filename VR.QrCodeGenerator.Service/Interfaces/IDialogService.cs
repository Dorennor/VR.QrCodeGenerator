namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IDialogService
    {
        /// <returns>The chosen folder, or an empty string when cancelled. On Android, a folder URI.</returns>
        Task<string> OpenFolderDialogAsync(string initialDirectory = null);

        /// <param name="filter">A file type filter such as "Pictures|*.png;*.jpg"; null shows all files.</param>
        /// <returns>The chosen file's path, or an empty string when cancelled.</returns>
        Task<string> OpenFileDialogAsync(string filter = null);

        void ShowWarnMessageBox(string message);

        void ShowErrorMessageBox(string message);
    }
}