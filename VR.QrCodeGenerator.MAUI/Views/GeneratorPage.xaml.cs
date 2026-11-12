using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Views
{
    /// <summary>
    /// Plain QR codes from text or a link.
    /// </summary>
    public partial class GeneratorPage : ContentPage
    {
        public GeneratorPage(MainViewModel viewModel)
        {
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
        }
    }
}
