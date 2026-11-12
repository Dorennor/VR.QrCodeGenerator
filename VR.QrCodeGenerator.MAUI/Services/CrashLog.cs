using Android.Runtime;

using VR.QrCodeGenerator.Service.Helpers;

namespace VR.QrCodeGenerator.MAUI.Services
{
    /// <summary>
    /// Unexpected errors, as on Windows: written to &lt;app&gt;_catastrophic.log in the logs folder (Settings → Share
    /// logs), and on the UI thread the app tells the user and stays open.
    /// </summary>
    public static class CrashLog
    {
        private const string AppName = "VR.QrCodeGenerator";

        public static void Register()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => Write(eventArgs.ExceptionObject as Exception);

            TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            {
                Write(eventArgs.Exception);
                eventArgs.SetObserved();
            };

            // Errors on the UI thread: keep the app open, an unexpected error in one action should not lose the user's work.
            AndroidEnvironment.UnhandledExceptionRaiser += (_, eventArgs) =>
            {
                Write(eventArgs.Exception);
                eventArgs.Handled = true;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (Application.Current?.Windows.FirstOrDefault()?.Page is { } page)
                        await page.DisplayAlertAsync("Error", "Something went wrong. Details are in the log (Settings → Share logs).", "OK");
                });
            };
        }

        public static void Write(Exception exception)
        {
            if (exception is null)
                return;

            try
            {
                var message = $"[{DateTime.Now}] CRASH DETAILS:\n" +
                              $"Message: {exception.Message}\n" +
                              $"Exception: {exception}\n" +
                              $"Stack Trace: {exception.StackTrace}";

                Directory.CreateDirectory(AppPaths.LogsDirectory);
                File.AppendAllText(Path.Combine(AppPaths.LogsDirectory, $"{AppName}_catastrophic.log"), message + "\n" + new string('-', 50) + "\n");
            }
            catch
            {
                // ignored: nothing else can be done here.
            }
        }
    }
}