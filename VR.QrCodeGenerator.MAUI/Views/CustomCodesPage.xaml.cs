using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Views
{
    /// <summary>
    /// QR codes in a fun style, such as Kurkod.
    /// </summary>
    public partial class CustomCodesPage : ContentPage
    {
        public CustomCodesPage(CustomCodesViewModel viewModel)
        {
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
        }
    }
}
