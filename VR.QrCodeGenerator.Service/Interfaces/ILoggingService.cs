using System.Runtime.CompilerServices;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface ILoggingService
    {
        void Info(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");

        void Warn(string message, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");

        void Error(string message, Exception exception = null, [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1, [CallerFilePath] string callerFilePath = "");
    }
}