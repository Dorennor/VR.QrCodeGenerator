using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// The plain QR code.
    /// </summary>
    public sealed class ClassicStyle : IQrStyle
    {
        public EnumQrStyle Style => EnumQrStyle.Classic;

        public bool[,] Apply(bool[,] code, string content) => code;
    }
}
