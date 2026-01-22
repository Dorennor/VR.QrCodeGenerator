using System;

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using QRCoder;

using VR.QrCodeGenerator.WinUI.Enums;
using VR.QrCodeGenerator.WinUI.Helpers;
using VR.QrCodeGenerator.WinUI.Models;
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
        private byte[] _qrCode;
        private string _qrCodePath;

        private AppWindow AppMainWindow
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

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            UrlTextBox.Text = string.Empty;

            GenerateQrCodeButton.IsEnabled = false;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;

            _settings = settings;
            _loggingService = new LoggingService(_settings);
            _dialogService = new DialogService();
            _dialogService.Initialize(this);

            ContentOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumContentType));
            ContentOptionsComboBox.SelectedIndex = 0;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumImageFormat));
            FileFormatOptionsComboBox.SelectedIndex = 0;
        }

        private void Minimize_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (this.AppMainWindow.Presenter is OverlappedPresenter presenter)
                presenter.Minimize();
        }

        private void Maximize_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (this.AppMainWindow.Presenter is OverlappedPresenter presenter)
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

        private async void GenerateQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            bool isUriMode = ContentOptionsComboBox.SelectionBoxItem is EnumContentType.Uri;
            bool isTextMode = ContentOptionsComboBox.SelectionBoxItem is EnumContentType.Text;

            if ((isUriMode && UriHelper.IsValidUrl(UrlTextBox.Text)) || isTextMode)
            {
                try
                {
                    QrCodeOptions options = new QrCodeOptions
                    {
                        PixelsPerModule = (int)PixelSizeSlider.Value
                    };

                    if (EccComboBox.SelectedItem is ComboBoxItem { Tag: string eccTag })
                    {
                        options.EccLevel = eccTag switch
                        {
                            "L" => QRCodeGenerator.ECCLevel.L,
                            "M" => QRCodeGenerator.ECCLevel.M,
                            "H" => QRCodeGenerator.ECCLevel.H,
                            _ => QRCodeGenerator.ECCLevel.Q
                        };
                    }

                    var fg = FgColorPicker.Color;
                    var bg = BgColorPicker.Color;

                    options.ForegroundColor = new byte[] { fg.R, fg.G, fg.B };
                    options.BackgroundColor = new byte[] { bg.R, bg.G, bg.B };

                    _qrCode = QrCodeHelper.GenerateAndSaveQr(UrlTextBox.Text, options);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error during qr code generation process.", ex);
                    await _dialogService.ShowErrorMessageBoxAsync("Error during qr code generation process.");

                    return;
                }

                try
                {
                    await ImageHelper.DisplayImageFromBytes(_qrCode, QrCodeImage);
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error during displaying qr code process", ex);
                    await _dialogService.ShowErrorMessageBoxAsync("Error during displaying qr code process");
                }

                try
                {
                    if (_settings.PathsSettings.UseCustomPath)
                        _qrCodePath = await OutputHelper.SaveToFile(_qrCode, _settings.PathsSettings.OutputPath, (EnumImageFormat)FileFormatOptionsComboBox.SelectedItem);
                    else
                        _qrCodePath = await OutputHelper.SaveToFile(_qrCode, imageFormat: (EnumImageFormat)FileFormatOptionsComboBox.SelectedItem);

                    CopyToClipboardButton.IsEnabled = true;
                    ResetQrCodeButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    _loggingService.Error("Error during saving qr code process", ex);
                    await _dialogService.ShowErrorMessageBoxAsync("Error during saving qr code process");
                }
            }
            else
            {
                await _dialogService.ShowWarnMessageBoxAsync("Uri is broken");
            }
        }

        private void FgColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            FgColorButton.Background = new SolidColorBrush(args.NewColor);
        }

        private void BgColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            BgColorButton.Background = new SolidColorBrush(args.NewColor);
        }

        private void UrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(UrlTextBox.Text))
            {
                GenerateQrCodeButton.IsEnabled = false;
                CopyToClipboardButton.IsEnabled = false;
                ResetQrCodeButton.IsEnabled = false;
            }
            else
            {
                GenerateQrCodeButton.IsEnabled = true;
            }
        }

        private async void CopyToClipboardButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            await OutputHelper.CopyQrToClipboard(_qrCode, _qrCodePath);
        }

        private void ResetQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            QrCodeImage.Source = null;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;
        }
    }
}