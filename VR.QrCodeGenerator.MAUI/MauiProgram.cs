using SkiaSharp.Views.Maui.Controls.Hosting;

using VR.QrCodeGenerator.MAUI.Services;
using VR.QrCodeGenerator.MAUI.Views;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            CrashLog.Register();

            var builder = MauiApp.CreateBuilder();

            builder.UseMauiApp<App>()
                   .UseSkiaSharp();

            // The same services and view models as the Windows app; only the platform services differ.
            var settingsService = new SettingsService();
            var settings = settingsService.Load();

            var services = builder.Services;

            services.AddSingleton<Settings>(settings);
            services.AddSingleton<ISettingsService>(settingsService);
            services.AddSingleton<ILoggingService, LoggingService>();
            services.AddSingleton<IDialogService, MauiDialogService>();
            services.AddSingleton<IPlatformService, AndroidPlatformService>();
            services.AddSingleton<IQrCodeGeneratorService, QrCodeGeneratorService>();

            services.AddSingleton<MainViewModel>();
            services.AddSingleton<CustomCodesViewModel>();
            services.AddSingleton<DrawingCodesViewModel>();
            services.AddSingleton<PictureCodesViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Singletons: switching tabs keeps each generator's content and preview, as on Windows.
            services.AddSingleton<GeneratorPage>();
            services.AddSingleton<CustomCodesPage>();
            services.AddSingleton<DrawingPage>();
            services.AddSingleton<PicturePage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}