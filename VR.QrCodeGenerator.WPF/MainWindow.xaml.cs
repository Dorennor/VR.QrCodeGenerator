using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogService;

        public MainWindow(MainViewModel mainViewModel, IDialogService dialogService)
        {
            _dialogService = dialogService;
            _mainViewModel = mainViewModel;
            _mainViewModel?.AfterQrCodeGenerated += DisplayGeneratedQrCode;

            InitializeComponent();

            GenerateQrCodeButton.IsEnabled = false;
            CopyToClipboardButton.IsEnabled = false;
            ResetQrCodeButton.IsEnabled = false;

            ContentOptionsComboBox.ItemsSource = Enum.GetValues<EnumContentType>();
            ContentOptionsComboBox.SelectedIndex = 0;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues<EnumImageFormat>();
            FileFormatOptionsComboBox.SelectedIndex = 0;

            //FgColorButton.Background = new SolidColorBrush(Colors.Black);
            //BgColorButton.Background = new SolidColorBrush(Colors.White);
        }

        public void DisplayGeneratedQrCode()
        {
            try
            {
                ImageHelper.DisplayImageFromBytes(_mainViewModel.QrCode, QrCodeImage);

                CopyToClipboardButton.IsEnabled = true;
                ResetQrCodeButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorMessageBox("Error during displaying qr code process");
            }
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