using VR.QrCodeGenerator.WPF.Views;

using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;

namespace VR.QrCodeGenerator.WPF
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindow(INavigationViewPageProvider pageProvider)
        {
            ArgumentNullException.ThrowIfNull(pageProvider);

            InitializeComponent();

            RootNavigation.SetPageProviderService(pageProvider);

            Loaded += (_, _) =>
            {
                // Follow the Windows light/dark setting while the app is running.
                SystemThemeWatcher.Watch(this, Wpf.Ui.Controls.WindowBackdropType.Mica, true);

                RootNavigation.Navigate(typeof(GeneratorPage));
            };
        }
    }
}
