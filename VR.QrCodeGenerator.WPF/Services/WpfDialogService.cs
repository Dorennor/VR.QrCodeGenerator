using System.Windows;

using Microsoft.Win32;

using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.WPF.Services
{
    public class WpfDialogService : IDialogService
    {
        public Task<string> OpenFolderDialogAsync(string initialDirectory = null)
        {
            var openFolderDialog = new OpenFolderDialog();

            if (!string.IsNullOrWhiteSpace(initialDirectory) && System.IO.Directory.Exists(initialDirectory))
                openFolderDialog.InitialDirectory = initialDirectory;
            var result = openFolderDialog.ShowDialog();

            return Task.FromResult(result == true ? openFolderDialog.FolderName : string.Empty);
        }

        public Task<string> OpenFileDialogAsync(string filter = null)
        {
            var openFileDialog = new OpenFileDialog();

            if (!string.IsNullOrEmpty(filter))
                openFileDialog.Filter = filter + "|All files|*.*";
            var result = openFileDialog.ShowDialog();

            return Task.FromResult(result == true ? openFileDialog.FileName : string.Empty);
        }

        public void ShowWarnMessageBox(string message)
        {
            MessageBox.Show(message, "Warn", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowErrorMessageBox(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}