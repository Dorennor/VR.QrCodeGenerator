using QRCoder;

namespace VR.QrCodeGenerator.WinUI.Helpers
{
    public static class QrCodeHelper
    {
        public static byte[] GenerateAndSaveQr(string url)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
                {
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeImage = qrCode.GetGraphic(20, false);

                        return qrCodeImage;

                        //string exePath = AppDomain.CurrentDomain.BaseDirectory;
                        //string outputFolder = Path.Combine(exePath, "Output");

                        //// 4. Create "Output" directory if it doesn't exist
                        //if (!Directory.Exists(outputFolder))
                        //{
                        //    Directory.CreateDirectory(outputFolder);
                        //}

                        //// 5. Save the file
                        //// We use a timestamp in the filename so you don't overwrite previous ones
                        //string fileName = $"qr_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        //string fullPath = Path.Combine(outputFolder, fileName);

                        //File.WriteAllBytes(fullPath, qrCodeImage);

                        //Console.WriteLine($"File saved to: {fullPath}");
                    }
                }
            }
        }
    }
}