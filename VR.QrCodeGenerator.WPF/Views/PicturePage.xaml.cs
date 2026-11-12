using System.Windows;
using System.Windows.Controls;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    /// <summary>
    /// A picture that becomes the QR code. A picture can be chosen with the button or dropped onto the picture card.
    /// </summary>
    public partial class PicturePage : Page
    {
        private readonly PictureCodesViewModel _viewModel;

        public PicturePage(PictureCodesViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            InitializeComponent();
        }

        private void Picture_OnDragOver(object sender, DragEventArgs eventArgs)
        {
            eventArgs.Effects = eventArgs.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            eventArgs.Handled = true;
        }

        private void Picture_OnDrop(object sender, DragEventArgs eventArgs)
        {
            if (eventArgs.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                _viewModel.LoadPicture(files[0]);
        }
    }
}
