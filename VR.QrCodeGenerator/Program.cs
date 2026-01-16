using QRCoder;

namespace VR.QrCodeGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("--- Simple QR Code Generator ---");
            Console.Write("Enter the website URL: ");

            string url = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Invalid URL. Exiting.");
                return;
            }

            try
            {
                GenerateAndSaveQr(url);
                Console.WriteLine("Success! QR Code saved in the 'Output' folder.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void GenerateAndSaveQr(string url)
        {
            // 1. Create the QR Code Data
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                // 2. Render to PNG byte array
                // 20 = pixels per module (size), false = no background transparency (white background)
                byte[] qrCodeImage = qrCode.GetGraphic(20, false);

                // 3. Determine Output Path
                // BaseDirectory ensures we are looking at the folder where the .exe actually runs
                string exePath = AppDomain.CurrentDomain.BaseDirectory;
                string outputFolder = Path.Combine(exePath, "Output");

                // 4. Create "Output" directory if it doesn't exist
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // 5. Save the file
                // We use a timestamp in the filename so you don't overwrite previous ones
                string fileName = $"qr_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string fullPath = Path.Combine(outputFolder, fileName);

                File.WriteAllBytes(fullPath, qrCodeImage);

                Console.WriteLine($"File saved to: {fullPath}");
            }
        }
    }
}