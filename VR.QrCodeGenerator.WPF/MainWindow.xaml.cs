using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using QRCoder;

using VR.QrCodeGenerator.WPF.Enums;
using VR.QrCodeGenerator.WPF.Helpers;
using VR.QrCodeGenerator.WPF.Models;
using VR.QrCodeGenerator.WPF.Models.Settings;
using VR.QrCodeGenerator.WPF.Services;

namespace VR.QrCodeGenerator.WPF
{
    public partial class MainWindow : Window
    {
        private readonly QrCodeOptions _options;
        private byte[] _qrCode;
        private string _qrCodePath;

        public MainWindow(Settings settings)
        {
            InitializeComponent();

            UrlTextBox.Text = string.Empty;

            GenerateQrCodeButton.IsEnabled = false;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;

            _options = new QrCodeOptions();

            ContentOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumContentType));
            ContentOptionsComboBox.SelectedIndex = 0;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues(typeof(EnumImageFormat));
            FileFormatOptionsComboBox.SelectedIndex = 0;
        }

        private void GenerateQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            bool isUriMode = ContentOptionsComboBox.SelectedItem is EnumContentType uri && uri == EnumContentType.Uri;
            bool isTextMode = ContentOptionsComboBox.SelectedItem is EnumContentType text && text == EnumContentType.Text;

            if ((isUriMode && UriHelper.IsValidUrl(UrlTextBox.Text)) || isTextMode)
            {
                try
                {
                    var options = new QrCodeOptions
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

                    if (FgColorButton.Background is SolidColorBrush fg)
                        options.ForegroundColor = [fg.Color.R, fg.Color.G, fg.Color.B];

                    if (BgColorButton.Background is SolidColorBrush bg)
                        options.BackgroundColor = [bg.Color.R, bg.Color.G, bg.Color.B];

                    _qrCode = QrCodeHelper.GenerateAndSaveQr(UrlTextBox.Text, options);
                }
                catch (Exception ex)
                {
                    DialogService.ShowErrorMessageBox("Error during qr code generation process.");
                    return;
                }

                try
                {
                    ImageHelper.DisplayImageFromBytes(_qrCode, QrCodeImage);
                }
                catch (Exception ex)
                {
                    DialogService.ShowErrorMessageBox("Error during displaying qr code process");
                }

                try
                {
                    _qrCodePath = OutputHelper.SaveToFile(_qrCode, imageFormat: (EnumImageFormat)FileFormatOptionsComboBox.SelectedItem);

                    CopyToClipboardButton.IsEnabled = true;
                    ResetQrCodeButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    DialogService.ShowErrorMessageBox("Error during saving qr code process");
                }
            }
            else
            {
                DialogService.ShowWarnMessageBox("Uri is broken");
            }
        }

        private void FgColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var dlg = new System.Windows.Forms.ColorDialog();

            //if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    var c = System.Drawing.Color.FromArgb(dlg.Color.ToArgb());
            //    FgColorButton.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            //}
        }

        private void BgColorButton_OnClick(object sender, RoutedEventArgs e)
        {
            //var dlg = new System.Windows.Forms.ColorDialog();

            //if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    var c = System.Drawing.Color.FromArgb(dlg.Color.ToArgb());
            //    BgColorButton.Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            //}
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

        private void CopyToClipboardButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            OutputHelper.CopyQrToClipboard(_qrCodePath);
        }

        private void ResetQrCodeButton_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            QrCodeImage.Source = null;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;
        }

        private void FgColorPicker_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> eventArgs)
        {
            if (eventArgs.NewValue.HasValue)
                FgColorButton.Background = new SolidColorBrush(eventArgs.NewValue.Value);
        }

        private void FgColorPickerClose_OnClick(object sender, RoutedEventArgs e)
        {
            FgColorButton.IsChecked = false;
        }

        private void BgColorPicker_OnSelectedColorChangedColorPicker_OnSelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> eventArgs)
        {
            if (eventArgs.NewValue.HasValue)
                BgColorButton.Background = new SolidColorBrush(eventArgs.NewValue.Value);
        }

        private void BgColorPickerClose_OnClick(object sender, RoutedEventArgs e)
        {
            BgColorButton.IsChecked = false;
        }
    }
}