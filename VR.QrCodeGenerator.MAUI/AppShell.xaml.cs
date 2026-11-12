using VR.QrCodeGenerator.MAUI.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI
{
    public partial class AppShell : Shell
    {
        private readonly PictureCodesViewModel _pictureViewModel;

        public AppShell(PictureCodesViewModel pictureViewModel)
        {
            InitializeComponent();

            _pictureViewModel = pictureViewModel;

            // A picture shared from another app (the Android version of dropping a file) opens on the Picture tab.
            SharedPictureInbox.Received += OnPictureShared;
            Loaded += (_, _) => OnPictureShared();
        }

        private void OnPictureShared()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (SharedPictureInbox.TakePending() is not { } path)
                    return;

                await GoToAsync("//picture");
                _pictureViewModel.LoadPicture(path);
            });
        }
    }
}