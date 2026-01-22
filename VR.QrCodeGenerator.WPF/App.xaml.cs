using System.IO;
using System.Windows;

using Microsoft.Extensions.Configuration;

using VR.QrCodeGenerator.WPF.Models.Settings;

namespace VR.QrCodeGenerator.WPF
{
    public partial class App : Application
    {
        private readonly Settings _settings;

        public App()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _settings = configuration.Get<Settings>();
        }

        protected override void OnStartup(StartupEventArgs eventArgs)
        {
            var mainWindow = new MainWindow(_settings);
            mainWindow.Show();

            base.OnStartup(eventArgs);
        }
    }
}