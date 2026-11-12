using System.Windows;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IDialogService
    {
        string OpenFolderDialog(string initialDirectory = null);

        /// <param name="filter">A file type filter such as "Pictures|*.png;*.jpg"; null shows all files.</param>
        string OpenFileDialog(string filter = null);

        string SaveFileDialog(string defaultFileName);

        void ShowWarnMessageBox(string message);

        void ShowErrorMessageBox(string message);

        void ShowMessageBox(string message, string title = "Info", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information);
    }
}