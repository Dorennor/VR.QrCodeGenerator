using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using VR.QrCodeGenerator.WinUI.Enums;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace VR.QrCodeGenerator.WinUI.Services
{
    public class DialogService
    {
        private Window _mainWindow;

        public void Initialize(Window window)
        {
            _mainWindow = window;
        }

        public async Task<string> OpenFolderDialogAsync()
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("DialogService must be initialized with a Window before use.");

            var folderPicker = new FolderPicker();

            var hwnd = WindowNative.GetWindowHandle(_mainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            var folder = await folderPicker.PickSingleFolderAsync();
            return folder?.Path;
        }

        public async Task<string> OpenFileDialogAsync()
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("DialogService must be initialized with a Window before use.");

            var openPicker = new FileOpenPicker();

            var hwnd = WindowNative.GetWindowHandle(_mainWindow);
            InitializeWithWindow.Initialize(openPicker, hwnd);

            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            openPicker.FileTypeFilter.Add(".json");

            var file = await openPicker.PickSingleFileAsync();
            return file?.Path;
        }

        public async Task<string> SaveFileDialogAsync(string defaultFileName)
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("DialogService must be initialized with a Window before use.");

            var savePicker = new FileSavePicker();

            var hwnd = WindowNative.GetWindowHandle(_mainWindow);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON file", new List<string>() { ".json" });
            savePicker.SuggestedFileName = defaultFileName;

            var file = await savePicker.PickSaveFileAsync();
            return file?.Path;
        }

        public async Task ShowWarnMessageBoxAsync(string message)
        {
            await ShowMessageBoxAsync(message, "Warn", EnumMessageBoxButton.OK, EnumMessageBoxImage.Warning);
        }

        public async Task ShowErrorMessageBoxAsync(string message)
        {
            await ShowMessageBoxAsync(message, "Error", EnumMessageBoxButton.OK, EnumMessageBoxImage.Error);
        }

        public async Task ShowMessageBoxAsync(string message, string title = "Info",
            EnumMessageBoxButton buttons = EnumMessageBoxButton.OK,
            EnumMessageBoxImage icon = EnumMessageBoxImage.Information)
        {
            if (_mainWindow == null)
                throw new InvalidOperationException("DialogService must be initialized with a Window before use.");

            var dialog = new ContentDialog
            {
                XamlRoot = _mainWindow.Content.XamlRoot,
                Content = message,
                Title = title,
                DefaultButton = ContentDialogButton.Primary
            };

            switch (buttons)
            {
                case EnumMessageBoxButton.OK:
                    dialog.CloseButtonText = "OK";
                    break;

                case EnumMessageBoxButton.OKCancel:
                    dialog.PrimaryButtonText = "OK";
                    dialog.CloseButtonText = "Cancel";

                    break;

                case EnumMessageBoxButton.YesNo:
                    dialog.PrimaryButtonText = "Yes";
                    dialog.CloseButtonText = "No";

                    break;

                case EnumMessageBoxButton.YesNoCancel:
                    dialog.PrimaryButtonText = "Yes";
                    dialog.SecondaryButtonText = "No";
                    dialog.CloseButtonText = "Cancel";

                    break;
            }

            switch (icon)
            {
                case EnumMessageBoxImage.Error:
                    dialog.Title = $"❌ {title}";
                    break;

                case EnumMessageBoxImage.Warning:
                    dialog.Title = $"⚠️ {title}";
                    break;

                case EnumMessageBoxImage.Question:
                    dialog.Title = $"❓ {title}";
                    break;

                case EnumMessageBoxImage.Information:
                    dialog.Title = $"ℹ️ {title}";
                    break;
            }

            await dialog.ShowAsync();
        }
    }
}