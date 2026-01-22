using QRCoder;

namespace VR.QrCodeGenerator.WPF.Models
{
    public class QrCodeOptions
    {
        public QRCodeGenerator.ECCLevel EccLevel { get; set; } = QRCodeGenerator.ECCLevel.Q;

        public int PixelsPerModule { get; set; } = 20;

        public byte[] ForegroundColor { get; set; } = "\0\0\0"u8.ToArray();

        public byte[] BackgroundColor { get; set; } = [255, 255, 255];

        public bool DrawQuietZones { get; set; } = true;
    }
}