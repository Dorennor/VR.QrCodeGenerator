using QRCoder;

using SkiaSharp;

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

                        return ResizeImage(qrCodeImage, options.Width, options.Height);
                    }
                }
            }
        }

        private static byte[] ResizeImage(byte[] imageBytes, int targetWidth, int targetHeight)
        {
            using (SKBitmap originalBitmap = SKBitmap.Decode(imageBytes))
            {
                var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

                using (SKBitmap resizedBitmap = originalBitmap.Resize(new SKImageInfo(targetWidth, targetHeight), sampling))
                {
                    if (resizedBitmap == null)
                        return imageBytes;

                    using (SKImage image = SKImage.FromBitmap(resizedBitmap))
                    {
                        using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            return data.ToArray();
                        }
                    }
                }
            }
        }
    }
}