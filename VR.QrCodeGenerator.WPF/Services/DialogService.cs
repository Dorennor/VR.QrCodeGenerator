using System.Windows;

using Microsoft.Win32;

namespace VR.QrCodeGenerator.WPF.Services
{
    public static class DialogService
    {
        public static string OpenFolderDialog()
        {
            var openFolderDialog = new OpenFolderDialog();

            var result = openFolderDialog.ShowDialog();

            if (result.HasValue && result.Value)
                return openFolderDialog.FolderName;
            else
                return string.Empty;
        }

        public static string OpenFileDialog()
        {
            var openFileDialog = new OpenFileDialog();

            var result = openFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
                return openFileDialog.FileName;
            else
                return string.Empty;
        }

        public static string SaveFileDialog(string defaultFileName)
        {
            var saveFileDialog = new SaveFileDialog();

            var result = saveFileDialog.ShowDialog();

            if (result.HasValue && result.Value)
                return saveFileDialog.FileName;
            else
                return string.Empty;
        }

        public static void ShowWarnMessageBox(string message)
        {
            ShowMessageBox(message, "Warn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static void ShowErrorMessageBox(string message)
        {
            ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static void ShowMessageBox(string message, string title = "Info", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
        {
            MessageBox.Show(message, title, buttons, icon);
        }
    }
}