using QRCoder;

using VR.QrCodeGenerator.WPF.Models;

namespace VR.QrCodeGenerator.WPF.Helpers
{
    public static class QrCodeHelper
    {
        public static byte[] GenerateAndSaveQr(string url, QrCodeOptions options)
        {
            options ??= new QrCodeOptions();

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, options.EccLevel))
                {
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(
                            options.PixelsPerModule,
                            options.ForegroundColor,
                            options.BackgroundColor,
                            options.DrawQuietZones
                        );

                        return qrCodeImage;
                    }
                }
            }
        }
    }
}