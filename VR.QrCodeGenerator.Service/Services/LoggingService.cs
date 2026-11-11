using System.IO;
using System.Runtime.CompilerServices;

using Serilog;
using Serilog.Context;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    public class LoggingService : ILoggingService
    {
        public LoggingService(Settings settings)
        {
            ApplySettings(settings.LoggingSettings);
        }

        public void ApplySettings(LoggingSettings settings)
        {
            settings ??= new LoggingSettings();

            const string fullTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            var loggerConfiguration = new LoggerConfiguration();

            if (settings.Debug)
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();

            if (settings.Console)
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: fullTemplate,
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code
                );
            }

            if (settings.File)
            {
                loggerConfiguration.WriteTo.File(
                    Path.Combine(AppPaths.LogsDirectory, $"{appName}_log.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: fullTemplate
                );
            }

            // The file sink locks its file: close the old logger before the new one opens the same file.
            Log.CloseAndFlush();
            Log.Logger = loggerConfiguration.CreateLogger();
        }

        public void Info(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "")
        {
            PushPropertiesAndLog(callerMemberName, callerLineNumber, callerFilePath, () => Log.Information(message));
        }

        public void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "")
        {
            PushPropertiesAndLog(callerMemberName, callerLineNumber, callerFilePath, () => Log.Warning(message));
        }

        public void Error(string message, Exception exception = null, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "")
        {
            PushPropertiesAndLog(callerMemberName, callerLineNumber, callerFilePath, () => Log.Error(exception, message));
        }

        private static void PushPropertiesAndLog(string memberName, int lineNumber, string filePath, Action logAction)
        {
            var fileName = Path.GetFileName(filePath);

            using (LogContext.PushProperty("CallerMemberName", memberName))
            {
                using (LogContext.PushProperty("CallerLineNumber", lineNumber))
                {
                    using (LogContext.PushProperty("CallerFilePath", fileName))
                    {
                        logAction();
                    }
                }
            }
        }
    }
}