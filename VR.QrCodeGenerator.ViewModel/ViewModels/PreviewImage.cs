using SkiaSharp;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// A PNG the page shows, such as the QR code preview or a picture template. Each UI turns it into its own image
    /// type with a converter (WPF: BitmapSource, MAUI: ImageSource), so the view models work on every platform.
    /// </summary>
    public sealed class PreviewImage
    {
        private PreviewImage(byte[] png, int pixelWidth, int pixelHeight)
        {
            Png = png;
            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;
        }

        public byte[] Png { get; }

        public int PixelWidth { get; }

        public int PixelHeight { get; }

        /// <summary>
        /// Reads only the PNG's header for its size; the pixels are decoded by the UI.
        /// </summary>
        /// <exception cref="InvalidDataException">The data is not an image.</exception>
        public static PreviewImage FromPng(byte[] png)
        {
            ArgumentNullException.ThrowIfNull(png);

            using var codec = SKCodec.Create(new SKMemoryStream(png)) ?? throw new InvalidDataException("The data is not an image.");

            return new PreviewImage(png, codec.Info.Width, codec.Info.Height);
        }
    }
}