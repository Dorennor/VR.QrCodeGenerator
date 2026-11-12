using System.Windows.Controls;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    /// <summary>
    /// QR codes in a custom style, such as Kurkod. Its own view model, so its content and options are separate from the
    /// Generator page.
    /// </summary>
    public partial class CustomCodesPage : Page
    {
        public CustomCodesPage(CustomCodesViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
        }
    }
}
