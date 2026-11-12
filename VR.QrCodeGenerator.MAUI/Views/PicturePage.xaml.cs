using System.ComponentModel;

using VR.QrCodeGenerator.MAUI.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Views
{
    /// <summary>
    /// A picture drawn around the QR code. A picture is chosen with the button or shared from another app (the Android
    /// version of dropping a file); the code is moved and resized on the picture with PicturePlacementView.
    /// </summary>
    public partial class PicturePage : ContentPage
    {
        private const string PictureHelp =
            "This is your picture as it will appear around the QR code. The blue square is where the QR code goes.\n\n" +
            "• Drag the blue square to move the code.\n" +
            "• Drag its white corner, or pinch with two fingers, to make it bigger or smaller.\n" +
            "• Touch anywhere on the picture to put the code there.\n\n" +
            "You can also share a picture with this app from your gallery: it opens here.";

        private const string DefaultCaption = "The picture is drawn around the code. The blue square is the code.";

        private readonly PictureCodesViewModel _viewModel;

        public PicturePage(PictureCodesViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            BindingContext = _viewModel;

            InitializeComponent();

            _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
            UpdateCaption();
        }

        private void ViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == nameof(PictureCodesViewModel.PictureName))
                UpdateCaption();
        }

        private void UpdateCaption() =>
            PictureCaption.Text = string.IsNullOrEmpty(_viewModel.PictureName) ? DefaultCaption : $"{_viewModel.PictureName}: drag the blue square to move the code";

        private async void PictureHelp_OnTapped(object sender, TappedEventArgs eventArgs) => await MauiDialogService.ShowAsync("Picture", PictureHelp);
    }
}