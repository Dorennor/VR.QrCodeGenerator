using QRCoder;

namespace VR.QrCodeGenerator.WinUI.Models
{
    public class QrCodeOptions
    {
        public QRCodeGenerator.ECCLevel EccLevel { get; set; } = QRCodeGenerator.ECCLevel.Q;

        public int PixelsPerModule { get; set; } = 20;

        public byte[] ForegroundColor { get; set; } = new byte[] { 0, 0, 0 };

        public byte[] BackgroundColor { get; set; } = new byte[] { 255, 255, 255 };

        public bool DrawQuietZones { get; set; } = true;
    }
}