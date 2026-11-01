using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IQrCodeGeneratorService
    {
        /// <summary>
        /// Renders the content as a file in the given format and returns its bytes. Nothing is written to disk.
        /// </summary>
        byte[] Generate(string content, QrCodeOptions options, EnumImageFormat format = EnumImageFormat.Png);
    }
}
