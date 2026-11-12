using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Converters
{
    /// <summary>
    /// A view model's <see cref="PreviewImage"/> as a bitmap for an Image; null stays null.
    /// </summary>
    [ValueConversion(typeof(PreviewImage), typeof(BitmapSource))]
    public sealed class PreviewImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is PreviewImage image ? LoadBitmap(image.Png) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

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