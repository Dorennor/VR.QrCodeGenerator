using System.Globalization;
using System.Windows.Data;

using MediaColor = System.Windows.Media.Color;

namespace VR.QrCodeGenerator.WPF.Converters
{
    /// <summary>
    /// The view models' <see cref="System.Drawing.Color"/> as a WPF color, both ways (for brushes and the color canvas).
    /// </summary>
    [ValueConversion(typeof(System.Drawing.Color), typeof(MediaColor))]
    public sealed class DrawingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is System.Drawing.Color color ? MediaColor.FromArgb(color.A, color.R, color.G, color.B) : default(MediaColor);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is MediaColor color ? System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B) : Binding.DoNothing;
    }
}