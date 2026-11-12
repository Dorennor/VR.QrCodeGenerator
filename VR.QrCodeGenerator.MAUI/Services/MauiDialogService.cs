using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.MAUI.Services
{
    /// <summary>
    /// Messages as alerts, pictures from the system picker and folders from the system folder picker.
    /// </summary>
    public sealed class MauiDialogService : IDialogService
    {
        public async Task<string> OpenFolderDialogAsync(string initialDirectory = null)
        {
            var folder = await MainActivity.PickFolderAsync();

            return folder?.ToString() ?? string.Empty;
        }

        /// <remarks>
        /// Only pictures are ever chosen, so the Windows filter is not needed: the picker offers images.
        /// </remarks>
        public async Task<string> OpenFileDialogAsync(string filter = null)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Choose a picture", FileTypes = FilePickerFileType.Images });

            return result?.FullPath ?? string.Empty;
        }

        public void ShowWarnMessageBox(string message) => Show("Warning", message);

        public void ShowErrorMessageBox(string message) => Show("Error", message);

        public static Task ShowAsync(string title, string message) =>
            MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
                    await page.DisplayAlertAsync(title, message, "OK");
            });

        // The view models do not wait for the user to close a message, as with a Windows message box.
        private static void Show(string title, string message) => _ = ShowAsync(title, message);
    }
}