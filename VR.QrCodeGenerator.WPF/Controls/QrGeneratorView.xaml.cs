using System.Windows;
using System.Windows.Controls;

using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.WPF.Controls
{
    public partial class QrGeneratorView : UserControl
    {
        public QrGeneratorView()
        {
            InitializeComponent();

            // The selected values come from the view model through the SelectedItem bindings.
            ContentOptionsComboBox.ItemsSource = Enum.GetValues<EnumContentType>();
            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues<EnumImageFormat>();
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
