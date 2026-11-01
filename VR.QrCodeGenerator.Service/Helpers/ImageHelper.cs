using System.IO;
using System.Windows.Media.Imaging;

namespace VR.QrCodeGenerator.Service.Helpers
{
    public static class ImageHelper
    {
        public static BitmapSource LoadBitmap(byte[] imageData)
        {
            using (var memoryStream = new MemoryStream(imageData))
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();

                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
        }
    }
}
