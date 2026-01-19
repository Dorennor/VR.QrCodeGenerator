using System;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using VR.QrCodeGenerator.WinUI.Helpers;
using VR.QrCodeGenerator.WinUI.Models.Settings;
using VR.QrCodeGenerator.WinUI.Services;

using WinRT.Interop;

namespace VR.QrCodeGenerator.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private readonly Settings _settings;
        private readonly DialogService _dialogService;
        private readonly LoggingService _loggingService;

        private AppWindow AppWindow
        {
            get
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);

                return AppWindow.GetFromWindowId(windowId);
            }
        }

        public MainWindow(Settings settings)
        {
            this.InitializeComponent();

            UrlTextBox.Text = string.Empty;
            GenerateQrCode.IsEnabled = false;
            ExtendsContentIntoTitleBar = true;

            _settings = settings;
            _loggingService = new LoggingService(_settings);
            _dialogService = new DialogService();
            _dialogService.Initialize(this);

            SetTitleBar(AppTitleBar);
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (this.AppWindow.Presenter is OverlappedPresenter presenter)
                presenter.Minimize();
        }

        private void Maximize_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (this.AppWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                {
                    presenter.Restore();
                    MaximizeIcon.Glyph = "\uE922";
                }
                else
                {
                    presenter.Maximize();
                    MaximizeIcon.Glyph = "\uE923";
                }
            }
        }

        private void Close_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            this.Close();
        }

        private async void GenerateQrCode_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (UriHelper.IsValidUrl(UrlTextBox.Text))
            {
                byte[] qrCode = null;

                try
                {
                    qrCode = QrCodeHelper.GenerateAndSaveQr(UrlTextBox.Text);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error during qr code generation process.", ex);
                    await _dialogService.ShowErrorMessageBoxAsync("Error during qr code generation process.");
                }

                try
                {
                    await ImageHelper.DisplayImageFromBytes(qrCode, QrCodeImage);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error during displaying qr code process", ex);
                    await _dialogService.ShowErrorMessageBoxAsync("Error during displaying qr code process");
                }
            }
            else
            {
                await _dialogService.ShowWarnMessageBoxAsync("Uri is broken");
            }
        }

        private void UrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(UrlTextBox.Text))
                GenerateQrCode.IsEnabled = false;
            else
                GenerateQrCode.IsEnabled = true;
        }
    }
}