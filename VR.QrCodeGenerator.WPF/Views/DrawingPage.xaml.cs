using System.Windows.Controls;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    /// <summary>
    /// QR codes shaped like the user's drawing.
    /// </summary>
    public partial class DrawingPage : Page
    {
        public DrawingPage(DrawingCodesViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();
        }
    }
}
