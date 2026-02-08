using System.Windows;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IDialogService
    {
        string OpenFolderDialog();

        string OpenFileDialog();

        string SaveFileDialog(string defaultFileName);

        void ShowWarnMessageBox(string message);

        void ShowErrorMessageBox(string message);

        void ShowMessageBox(string message, string title = "Info", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information);
    }
}