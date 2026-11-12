using System.Windows.Controls;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    /// <summary>
    /// Plain QR codes.
    /// </summary>
    public partial class GeneratorPage : Page
    {
        public GeneratorPage(MainViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
        }
    }
}
