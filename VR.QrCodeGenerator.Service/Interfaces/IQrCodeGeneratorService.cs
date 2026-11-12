using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Styles;

namespace VR.QrCodeGenerator.Service.Interfaces
{
    public interface IQrCodeGeneratorService
    {
        /// <summary>
        /// Renders the content as a file in the given format, in the built-in style from <see cref="QrCodeOptions.Style"/>,
        /// and returns its bytes. Nothing is written to disk.
        /// </summary>
        byte[] Generate(string content, QrCodeOptions options, EnumImageFormat format = EnumImageFormat.Png);

        /// <summary>
        /// The same with a style object, for styles made at runtime (for example from a drawing);
        /// <see cref="QrCodeOptions.Style"/> is ignored.
        /// </summary>
        byte[] Generate(string content, QrCodeOptions options, IQrStyle style, EnumImageFormat format = EnumImageFormat.Png);
    }
}
