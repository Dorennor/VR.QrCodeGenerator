using System.Windows;
using System.Windows.Controls;

using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.WPF.Controls
{
    /// <summary>
    /// The generator layout shared by the Generator, Custom codes and Drawing pages. The view model decides the title;
    /// <see cref="TopContent"/> adds a page's own section (style picker, canvas) above the options.
    /// </summary>
    public partial class QrGeneratorView : UserControl
    {
        public static readonly DependencyProperty TopContentProperty =
            DependencyProperty.Register(nameof(TopContent), typeof(object), typeof(QrGeneratorView));

        public QrGeneratorView()
        {
            InitializeComponent();

            // The selected values come from the view model through the SelectedItem bindings.
            ContentOptionsComboBox.ItemsSource = Enum.GetValues<EnumContentType>();
            FileFormatOptionsComboBox.ItemsSource = Enum.GetValues<EnumImageFormat>();
        }

        /// <summary>
        /// Shown at the top of the options column. It inherits the page's view model as its DataContext.
        /// </summary>
        public object TopContent
        {
            get => GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
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
