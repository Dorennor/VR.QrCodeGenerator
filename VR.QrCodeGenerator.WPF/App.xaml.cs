using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;
using VR.QrCodeGenerator.WPF.Services;
using VR.QrCodeGenerator.WPF.Views;

using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;

namespace VR.QrCodeGenerator.WPF
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private readonly Settings _settings;
        private readonly ISettingsService _settingsService;

        public App()
        {
            DispatcherUnhandledException += (sender, eventArgs) =>
            {
                HandleGlobalException(eventArgs.Exception);

                // Keep the app open: an unexpected error in one action should not lose the user's work.
                eventArgs.Handled = true;
                MessageBox.Show($"Something went wrong. Details are in {AppPaths.LogsDirectory}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                HandleGlobalException(eventArgs.ExceptionObject as Exception);
            };

            _settingsService = new SettingsService();
            _settings = _settingsService.Load();

            _host = Host.CreateDefaultBuilder().ConfigureServices(ConfigureServices).Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Settings>(_settings);

            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<ISettingsService>(_settingsService);
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<IQrCodeGeneratorService, QrCodeGeneratorService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Singletons: switching pages keeps the generator's content and preview.
            services.AddSingleton<GeneratorPage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<INavigationViewPageProvider, ServiceProviderPageProvider>();

            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs eventArgs)
        {
            await _host.StartAsync();

            ApplicationThemeManager.ApplySystemTheme(true);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(eventArgs);
        }

        protected override async void OnExit(ExitEventArgs eventArgs)
        {
            using (_host)
            {
                await _host.StopAsync();
            }

            base.OnExit(eventArgs);
        }

        private static void HandleGlobalException(Exception exception)
        {
            if (exception == null)
                return;

            var appName = Assembly.GetExecutingAssembly().GetName().Name ?? "VR.QrCodeGenerator";
            const string logName = "Application";

            var errorMessage = $"[{DateTime.Now}] CRASH DETAILS:\n" +
                               $"Message: {exception.Message}\n" +
                               $"Exception: {exception}\n" +
                               $"Stack Trace: {exception.StackTrace}";

            try
            {
                var logPath = Path.Combine(AppPaths.LogsDirectory, $"{appName}_catastrophic.log");

                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, errorMessage + "\n" + new string('-', 50) + "\n");
            }
            catch
            {
                // ignored
            }

            try
            {
                if (!EventLog.SourceExists(appName))
                {
                    EventLog.CreateEventSource(appName, logName);
                }

                EventLog.WriteEntry(appName, errorMessage, System.Diagnostics.EventLogEntryType.Error, 666);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}