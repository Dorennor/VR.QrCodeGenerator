namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// Moving and resizing the code on a drawing or picture of <c>width</c> x <c>height</c> pixels. The code is a
    /// square of <c>codeSize</c> of the shorter side, centred on <c>center</c> (fractions of the width and height), and
    /// always kept whole inside the area (see <see cref="MaskShape.CodeSquare"/>).
    /// </summary>
    public static class CodePlacement
    {
        /// <summary>
        /// The centre to store when the user moves the code's centre to (<paramref name="x"/>, <paramref name="y"/>)
        /// pixels. Clamped through the square, so dragging past an edge does not store a position the code cannot reach.
        /// </summary>
        public static ShapePoint MoveTo(int width, int height, double codeSize, double x, double y)
        {
            var (left, top, side) = MaskShape.CodeSquare(width, height, codeSize, new ShapePoint(x / width, y / height));

            return new ShapePoint((left + side / 2) / width, (top + side / 2) / height);
        }

        /// <summary>
        /// The code size when its corner is dragged to (<paramref name="x"/>, <paramref name="y"/>) pixels: resized around
        /// its centre, within <paramref name="minimum"/> and <paramref name="maximum"/>.
        /// </summary>
        public static double ResizeTo(int width, int height, double codeSize, ShapePoint center, double x, double y, double minimum, double maximum)
        {
            var (left, top, side) = MaskShape.CodeSquare(width, height, codeSize, center);
            var half = Math.Max(Math.Abs(x - (left + side / 2)), Math.Abs(y - (top + side / 2)));

            return Math.Clamp(2 * half / Math.Min(width, height), minimum, maximum);
        }

        /// <summary>
        /// The size of the resize handle at the code's corner, in pixels of the area: about 4 % of it.
        /// </summary>
        public static double HandleSize(int width, int height) => Math.Max(width, height) / 24.0;
    }
}
