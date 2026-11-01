using System.Windows;

using VR.QrCodeGenerator.Model.Enums;

using Wpf.Ui.Appearance;

namespace VR.QrCodeGenerator.WPF
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Follow the Windows light/dark setting while the app is running.
            Loaded += (_, _) => SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica, true);

            ContentOptionsComboBox.ItemsSource = Enum.GetValues<EnumContentType>();
            ContentOptionsComboBox.SelectedIndex = 0;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues<EnumImageFormat>();
            FileFormatOptionsComboBox.SelectedIndex = 0;
        }

        private void FgColorPickerClose_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            FgColorButton.IsChecked = false;
        }

        private void BgColorPickerClose_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            BgColorButton.IsChecked = false;
        }
    }
}