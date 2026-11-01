using QRCoder;

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

        public int Height { get; set; } = 500;

        public int Width { get; set; } = 500;
    }
}