using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
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
            DispatcherUnhandledException += (sender, eventArgs) =>
            {
                HandleGlobalException(eventArgs.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                HandleGlobalException(eventArgs.ExceptionObject as Exception);
            };

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

        protected override async void OnStartup(StartupEventArgs eventArgs)
        {
            await _host.StartAsync();

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
                var logPath = Path.Combine(AppContext.BaseDirectory, "logs", $"{appName}_catastrophic.log");

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
            catch (SecurityException)
            {
                // ignored
            }
        }
    }
}