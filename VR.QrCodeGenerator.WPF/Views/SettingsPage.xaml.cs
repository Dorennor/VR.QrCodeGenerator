using System.Windows.Controls;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            DataContext = viewModel;

            InitializeComponent();

            // Each visit shows the settings in use; unsaved edits from an earlier visit are dropped.
            Loaded += (_, _) => viewModel.Load();
        }
    }
}
