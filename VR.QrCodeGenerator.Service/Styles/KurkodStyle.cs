using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Service.Helpers;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// A chicken drawn around the QR code, like the Kurkod meme. See <see cref="KurkodBuilder"/>.
    /// </summary>
    public sealed class KurkodStyle : IQrStyle
    {
        public EnumQrStyle Style => EnumQrStyle.Kurkod;

        public bool[,] Apply(bool[,] code, string content) => KurkodBuilder.Build(code, content);
    }
}
