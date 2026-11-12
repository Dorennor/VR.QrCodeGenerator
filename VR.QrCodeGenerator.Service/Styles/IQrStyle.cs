using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// Turns a QR code's modules into the final drawing. A custom style should leave the code's own modules unchanged
    /// and decorate around it, and make sure the result still scans (see <see cref="KurkodStyle"/>).
    /// </summary>
    public interface IQrStyle
    {
        EnumQrStyle Style { get; }

        /// <param name="code">The QR code's modules [row, column], true for dark, without a quiet zone.</param>
        /// <param name="content">The encoded text, for styles that vary with it or check that the result scans.</param>
        /// <returns>The drawing [row, column], without a quiet zone. It does not have to be square.</returns>
        bool[,] Apply(bool[,] code, string content);
    }
}
