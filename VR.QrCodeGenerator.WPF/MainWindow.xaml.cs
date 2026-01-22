using System.Globalization;
using System.Windows;
using System.Windows.Controls;

using QRCoder;

using VR.QrCodeGenerator.WPF.Enums;
using VR.QrCodeGenerator.WPF.Helpers;
using VR.QrCodeGenerator.WPF.Models;
using VR.QrCodeGenerator.WPF.Models.Settings;
using VR.QrCodeGenerator.WPF.Services;

using Wpf.Ui.Controls;

namespace VR.QrCodeGenerator.WPF
{
    public partial class MainWindow : FluentWindow
    {
        private byte[] _qrCode;
        private string _qrCodePath;

        public MainWindow(Settings settings)
        {
            // 1. Force English Language for Color Picker Text
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            InitializeComponent();

            LoggingService.Initialize(settings);

            // 2. Force Dark Theme
            //Wpf.Ui.Appearance.ApplicationThemeManager.Apply(Wpf.Ui.Appearance.ApplicationTheme.Dark);

            // 3. Initialize UI State
            UrlTextBox.Text = string.Empty;
            GenerateQrCodeButton.IsEnabled = false;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;

            ContentOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumContentType));
            ContentOptionsComboBox.SelectedIndex = 0;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumImageFormat));
            FileFormatOptionsComboBox.SelectedIndex = 0;
        }

        private void GenerateQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            bool isUriMode = ContentOptionsComboBox.SelectedItem is EnumContentType.Uri;
            bool isTextMode = ContentOptionsComboBox.SelectedItem is EnumContentType.Text;

            if ((isUriMode && UriHelper.IsValidUrl(UrlTextBox.Text)) || isTextMode)
            {
                try
                {
                    var options = new QrCodeOptions
                    {
                        PixelsPerModule = (int)PixelSizeSlider.Value
                    };

                    // Map ECC
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

                    // Map Colors (Directly from Picker)
                    if (FgColorPicker.SelectedColor.HasValue)
                    {
                        var c = FgColorPicker.SelectedColor.Value;
                        options.ForegroundColor = new byte[] { c.R, c.G, c.B };
                    }
                    else
                    {
                        options.ForegroundColor = new byte[] { 0, 0, 0 }; // Default Black
                    }

                    if (BgColorPicker.SelectedColor.HasValue)
                    {
                        var c = BgColorPicker.SelectedColor.Value;
                        options.BackgroundColor = new byte[] { c.R, c.G, c.B };
                    }
                    else
                    {
                        options.BackgroundColor = new byte[] { 255, 255, 255 }; // Default White
                    }

                    // Generate
                    _qrCode = QrCodeHelper.GenerateAndSaveQr(UrlTextBox.Text, options);

                    // Display
                    ImageHelper.DisplayImageFromBytes(_qrCode, QrCodeImage);

                    // Save
                    _qrCodePath = OutputHelper.SaveToFile(_qrCode, imageFormat: (EnumImageFormat)FileFormatOptionsComboBox.SelectedItem);

                    CopyToClipboardButton.IsEnabled = true;
                    ResetQrCodeButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    DialogService.ShowWarnMessageBox($"Error: {ex.Message}");
                }
            }
            else
            {
                DialogService.ShowWarnMessageBox("Uri is broken");
            }
        }

        private void UrlTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            bool hasText = !string.IsNullOrEmpty(UrlTextBox.Text);
            GenerateQrCodeButton.IsEnabled = hasText;

            if (!hasText)
            {
                CopyToClipboardButton.IsEnabled = false;
                ResetQrCodeButton.IsEnabled = false;
            }
        }

        private void CopyToClipboardButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (_qrCode != null) OutputHelper.CopyQrToClipboard(_qrCodePath);
        }

        private void ResetQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            UrlTextBox.Text = string.Empty;
            QrCodeImage.Source = null;
            _qrCode = null;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;
        }
    }
}