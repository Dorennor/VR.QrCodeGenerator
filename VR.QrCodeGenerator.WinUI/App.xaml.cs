using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.UI.Xaml;

using VR.QrCodeGenerator.WinUI.Models.Settings;

namespace VR.QrCodeGenerator.WinUI
{
    public partial class App : Application
    {
        private Window _window;
        private readonly Settings _settings;

        public App()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _settings = configuration.Get<Settings>();

            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs eventArgs)
        {
            _window = new MainWindow(_settings);
            _window.Activate();
        }
    }
}