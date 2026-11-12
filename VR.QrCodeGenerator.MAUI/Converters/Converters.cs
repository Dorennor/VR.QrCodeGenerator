using System.Globalization;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Converters
{
    /// <summary>
    /// A view model's <see cref="PreviewImage"/> as an image source; null stays null.
    /// </summary>
    public sealed class PreviewImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is PreviewImage image ? ImageSource.FromStream(() => new MemoryStream(image.Png)) : null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// The view models' <see cref="System.Drawing.Color"/> as a MAUI color.
    /// </summary>
    public sealed class DrawingColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is System.Drawing.Color color ? Color.FromRgba(color.R, color.G, color.B, color.A) : Colors.Transparent;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// The error correction tag (L, M, Q, H) as the index of its entry in the picker.
    /// </summary>
    public sealed class EccTagIndexConverter : IValueConverter
    {
        public static IReadOnlyList<string> Tags { get; } = ["L", "M", "Q", "H"];

        public static IReadOnlyList<string> Names { get; } = ["Low (7%)", "Medium (15%)", "Quality (25%)", "High (30%)"];

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is string tag ? Tags.ToList().IndexOf(tag) : -1;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is int index && index >= 0 && index < Tags.Count ? Tags[index] : Binding.DoNothing;
    }

    /// <summary>
    /// Rounds a slider's value to steps of the parameter (for example 0.05) before it reaches the view model, like the
    /// Windows sliders' ticks. Settings that rebuild the picture template then change only once per step while dragging.
    /// </summary>
    public sealed class SnapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double number || !double.TryParse(parameter?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var step) || step <= 0)
                return value;

            return Math.Round(Math.Round(number / step) * step, 6);
        }
    }

    public sealed class IsNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class IsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is not true;
    }
}