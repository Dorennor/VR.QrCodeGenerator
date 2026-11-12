namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// A picture as a shape with tones: <see cref="MaskShape"/> decides where the shape is (the whole picture, or its
    /// subject), and the picture's darkness decides each cell, dithered by <see cref="ShapedCodeBuilder"/>.
    /// </summary>
    public sealed class PictureShape : IToneShape
    {
        private readonly MaskShape _mask;
        private readonly int _width;
        private readonly int _height;

        // Summed-area table of the darkness: the average over any cell in constant time.
        private readonly double[,] _darknessSums;

        /// <param name="mask">Where the shape is, and where the code goes; the same size as <paramref name="darkness"/>.</param>
        /// <param name="darkness">The picture's darkness [row, column], 0 (white) to 1 (black).</param>
        /// <param name="drawOutline">A dark outline around the shape: on for a subject, off for a whole picture.</param>
        /// <param name="codeBlendBias">Let the code take on the picture too, or null for a solid code (see <see cref="IToneShape.CodeBlendBias"/>).</param>
        public PictureShape(MaskShape mask, float[,] darkness, bool drawOutline, double? codeBlendBias = null)
        {
            _mask = mask ?? throw new ArgumentNullException(nameof(mask));
            ArgumentNullException.ThrowIfNull(darkness);

            _height = darkness.GetLength(0);
            _width = darkness.GetLength(1);
            DrawOutline = drawOutline;
            CodeBlendBias = codeBlendBias is null ? null : Math.Clamp(codeBlendBias.Value, 0, 1);

            _darknessSums = new double[_height + 1, _width + 1];

            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                    _darknessSums[y + 1, x + 1] = darkness[y, x] + _darknessSums[y, x + 1] + _darknessSums[y + 1, x] - _darknessSums[y, x];
            }
        }

        public ShapeMargins Margins => _mask.Margins;

        public IReadOnlyList<ShapePoint> Eyes => _mask.Eyes;

        public bool DrawOutline { get; }

        public double? CodeBlendBias { get; }

        public ShapePart Classify(double u, double v, int size) => _mask.Classify(u, v, size);

        public double Darkness(double u, double v, int size)
        {
            var (x0, y0, x1, y1) = _mask.PixelRect(u, v, size);

            // A cell smaller than a pixel still reads the pixel it lies in.
            if (x1 <= x0)
                x1 = Math.Min(_width, x0 + 1);

            if (y1 <= y0)
                y1 = Math.Min(_height, y0 + 1);

            var area = (x1 - x0) * (y1 - y0);

            if (area <= 0)
                return 0;

            var sum = _darknessSums[y1, x1] - _darknessSums[y0, x1] - _darknessSums[y1, x0] + _darknessSums[y0, x0];

            return sum / area;
        }
    }
}
