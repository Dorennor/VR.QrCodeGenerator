using System.Windows;
using System.Windows.Controls;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    public partial class GeneratorPage : Page
    {
        public GeneratorPage(MainViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();

            ContentOptionsComboBox.ItemsSource = Enum.GetValues<EnumContentType>();
            ContentOptionsComboBox.SelectedItem = viewModel.SelectedContentType;

            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues<EnumImageFormat>();
            FileFormatOptionsComboBox.SelectedItem = viewModel.SelectedImageFormat;
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
