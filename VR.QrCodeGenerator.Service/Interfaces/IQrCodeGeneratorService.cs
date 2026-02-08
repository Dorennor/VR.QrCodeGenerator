using VR.QrCodeGenerator.Model.Models;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IQrCodeGeneratorService
    {
        byte[] GenerateAndSaveQr(string url, QrCodeOptions options);
    }
}