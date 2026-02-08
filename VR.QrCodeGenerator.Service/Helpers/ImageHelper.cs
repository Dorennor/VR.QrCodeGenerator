using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace VR.QrCodeGenerator.Service.Helpers
{
    public static class ImageHelper
    {
        public static void DisplayImageFromBytes(byte[] imageData, Image qrCodeImage)
        {
            if (imageData == null || imageData.Length == 0)
                return;

            using (var memoryStream = new MemoryStream(imageData))
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();

                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                bitmap.EndInit();
                bitmap.Freeze();

                qrCodeImage.Source = bitmap;
            }
        }
    }
}