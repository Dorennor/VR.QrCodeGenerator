using System.IO;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;
using VR.QrCodeGenerator.WPF.Services;

namespace VR.QrCodeGenerator.WPF
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private readonly Settings _settings;

        public App()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _settings = configuration.Get<Settings>();

            _host = Host.CreateDefaultBuilder().ConfigureServices(ConfigureServices).Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Settings>(_settings);

            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<IQrCodeGeneratorService, QrCodeGeneratorService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>(serviceProvider => new MainWindow(serviceProvider.GetService<MainViewModel>(), serviceProvider.GetService<IDialogService>())
            {
                DataContext = serviceProvider.GetService<MainViewModel>()
            });
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(e);
        }
    }
}