using VR.QrCodeGenerator.Service.Generation;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// Turns a QR code's modules into the final drawing. A custom style should leave the code's own modules unchanged
    /// and decorate around it, and make sure the result still scans (see <see cref="ShapeStyle"/>). Built-in styles are
    /// listed in <see cref="QrStyles"/>; others (a drawing, a picture) are made at runtime.
    /// </summary>
    public interface IQrStyle
    {
        /// <param name="code">The QR code's modules [row, column], true for dark, without a quiet zone.</param>
        /// <param name="content">The encoded text, for styles that vary with it or check that the result scans.</param>
        /// <returns>The drawing, without a quiet zone. It does not have to be square.</returns>
        QrDrawing Apply(bool[,] code, string content);
    }
}
