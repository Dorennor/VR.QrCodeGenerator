using System.Runtime.CompilerServices;

using VR.QrCodeGenerator.Model.Models.Settings;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface ILoggingService
    {
        /// <summary>
        /// Rebuilds the logger so changed logging settings take effect without restarting the app.
        /// </summary>
        void ApplySettings(LoggingSettings settings);

        void Info(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");

        void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");

        void Error(string message, Exception exception = null, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");
    }
}