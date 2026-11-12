namespace VR.QrCodeGenerator.Service.Rendering
{
    public static class ImageLayout
    {
        /// <summary>
        /// The largest rectangle with the matrix's aspect ratio that fits the image, centered, in whole pixels. A QR code
        /// must keep its proportions to scan, so a different aspect ratio adds margin instead of stretching.
        /// </summary>
        public static (int Left, int Top, int Width, int Height) Fit(int columns, int rows, int width, int height)
        {
            var scale = Math.Min((double)width / columns, (double)height / rows);
            var drawWidth = Math.Min(width, (int)Math.Round(columns * scale));
            var drawHeight = Math.Min(height, (int)Math.Round(rows * scale));

            return ((width - drawWidth) / 2, (height - drawHeight) / 2, drawWidth, drawHeight);
        }

        public static string ToHex(byte[] color) => $"#{color[0]:X2}{color[1]:X2}{color[2]:X2}";
    }
}
