using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using Windows.Storage.Streams;

namespace VR.QrCodeGenerator.WinUI.Helpers
{
    public static class ImageHelper
    {
        public static async Task DisplayImageFromBytes(byte[] imageData, Image qrCodeImage)
        {
            if (imageData == null || imageData.Length == 0)
                return;

            var bitmapImage = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(imageData.AsBuffer());
                stream.Seek(0);

                await bitmapImage.SetSourceAsync(stream);
            }

            qrCodeImage.Source = bitmapImage;
        }
    }
}