using QRCoder;

using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.Model.Models
{
    public class QrCodeOptions
    {
        public QRCodeGenerator.ECCLevel EccLevel { get; set; } = QRCodeGenerator.ECCLevel.Q;

        /// <summary>
        /// When set, every QR square is exactly this many pixels and the image size follows from the content, ignoring
        /// <see cref="Width"/> and <see cref="Height"/>. When null, the code is scaled to fit Width and Height.
        /// </summary>
        public int? PixelsPerModule { get; set; }

        public byte[] ForegroundColor { get; set; } = "\0\0\0"u8.ToArray();

        public byte[] BackgroundColor { get; set; } = [255, 255, 255];

        public bool DrawQuietZones { get; set; } = true;

        /// <summary>
        /// Classic, or a custom style such as Kurkod (a chicken drawn around the code). Custom styles leave the QR code
        /// itself untouched, so it scans as usual, but the image may no longer be square.
        /// </summary>
        public EnumQrStyle Style { get; set; } = EnumQrStyle.Classic;

        public int Height { get; set; } = 500;

        public int Width { get; set; } = 500;
    }
}