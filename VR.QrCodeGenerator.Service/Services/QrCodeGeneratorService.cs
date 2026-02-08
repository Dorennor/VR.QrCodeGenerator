using QRCoder;

using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    public class QrCodeGeneratorService : IQrCodeGeneratorService
    {
        public byte[] GenerateAndSaveQr(string url, QrCodeOptions options)
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