using VR.QrCodeGenerator.Service.Generation;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// The plain QR code.
    /// </summary>
    public sealed class ClassicStyle : IQrStyle
    {
        public QrDrawing Apply(bool[,] code, string content) => new QrDrawing(code);
    }
}
