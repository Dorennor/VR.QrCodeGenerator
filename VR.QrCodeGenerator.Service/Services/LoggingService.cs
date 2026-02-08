using System.IO;
using System.Runtime.CompilerServices;

using Serilog;
using Serilog.Context;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    public class LoggingService : ILoggingService
    {
        public LoggingService(Settings settings)
        {
            const string fullTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CallerMemberName}:{CallerLineNumber}] {Message:lj}{NewLine}{Exception}";
            var appName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            var loggerConfiguration = new LoggerConfiguration().Enrich.FromLogContext();

            if (settings.LoggingSettings.Debug)
                loggerConfiguration.MinimumLevel.Debug();
            else
                loggerConfiguration.MinimumLevel.Information();

            if (settings.LoggingSettings.Console)
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: fullTemplate,
                    theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code
                );
            }

            if (settings.LoggingSettings.File)
            {
                loggerConfiguration.WriteTo.File(
                    Path.Combine(AppContext.BaseDirectory, $"logs/{appName}_log.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: fullTemplate
                );
            }

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