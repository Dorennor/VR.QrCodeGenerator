namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// A shape from a drawing or a picture: the ink (pen lines, or the picture's pixels in the code's color) becomes
    /// solid or textured, and areas it encloses are filled with the QR-like texture. The code sits in the centre, in a
    /// square of <see cref="CodeFraction"/> of the shorter side.
    /// </summary>
    /// <remarks>
    /// The code's square counts as a wall when finding enclosed areas, so a head drawn on top of the code fills too.
    /// Small gaps in a hand-drawn outline are closed first, so an almost closed line still fills.
    /// </remarks>
    public sealed class MaskShape : IShape
    {
        /// <summary>
        /// Share of the module a pen line must cover to make it dark. Low, so thin lines are not lost on big modules.
        /// </summary>
        private const double SolidCoverage = 0.3;

        private const double TexturedCoverage = 0.5;

        private readonly int _width;
        private readonly int _height;
        private readonly double _codeLeft;
        private readonly double _codeTop;
        private readonly double _codeSide;

        private readonly ShapePart _inkPart;
        private readonly bool _codeClosesAreas;
        private readonly double _largestFilledArea;

        // Summed-area tables of the pen lines and the enclosed areas: the coverage of any rectangle in constant time.
        private readonly int[,] _strokeSums;
        private readonly int[,] _enclosedSums;

        /// <param name="strokes">The drawing [row, column], true where the pen drew.</param>
        /// <param name="codeFraction">The side of the code's square, as a share of the drawing's shorter side.</param>
        /// <param name="gapToClose">Gaps in lines up to about twice this many pixels are closed when finding enclosed areas.</param>
        /// <param name="inkPart">
        /// How the ink itself is drawn: <see cref="ShapePart.Solid"/> keeps pen lines crisp (drawings);
        /// <see cref="ShapePart.Textured"/> fills dark areas with the QR-like texture, like the Kurkod body (pictures).
        /// </param>
        /// <param name="codeClosesAreas">
        /// Whether the code's square counts as a wall, so a head drawn on top of the code fills. Off for a picture's
        /// subject, where it would also close off background, such as the space under an arch.
        /// </param>
        /// <param name="largestFilledArea">
        /// Enclosed areas bigger than this share of the drawing stay empty: 1 fills every enclosed area (outlines);
        /// a small value fills only holes inside a shape that is already solid (a picture's subject).
        /// </param>
        public MaskShape(bool[,] strokes, double codeFraction, int gapToClose = 6, ShapePart inkPart = ShapePart.Solid, bool codeClosesAreas = true, double largestFilledArea = 1)
        {
            if (inkPart is not (ShapePart.Solid or ShapePart.Textured))
                throw new ArgumentOutOfRangeException(nameof(inkPart), inkPart, "Ink is drawn solid or textured.");

            _inkPart = inkPart;
            _codeClosesAreas = codeClosesAreas;
            _largestFilledArea = largestFilledArea;
            ArgumentNullException.ThrowIfNull(strokes);

            if (codeFraction <= 0 || codeFraction > 1)
                throw new ArgumentOutOfRangeException(nameof(codeFraction), codeFraction, "The code must take a share of the drawing between 0 and 1.");

            _height = strokes.GetLength(0);
            _width = strokes.GetLength(1);

            CodeFraction = codeFraction;
            _codeSide = codeFraction * Math.Min(_width, _height);
            _codeLeft = (_width - _codeSide) / 2;
            _codeTop = (_height - _codeSide) / 2;

            Margins = new ShapeMargins(
                _codeLeft / _codeSide,
                _codeTop / _codeSide,
                (_width - _codeLeft - _codeSide) / _codeSide,
                (_height - _codeTop - _codeSide) / _codeSide);

            _strokeSums = SummedArea(strokes);
            _enclosedSums = SummedArea(FindEnclosed(strokes, gapToClose));
        }

        public double CodeFraction { get; }

        public ShapeMargins Margins { get; }

        /// <summary>
        /// None: the user draws the eyes.
        /// </summary>
        public IReadOnlyList<ShapePoint> Eyes { get; } = [];

        public ShapePart Classify(double u, double v, int size)
        {
            // The module's square in drawing pixels.
            var moduleSide = _codeSide / size;
            var left = _codeLeft + u * _codeSide - moduleSide / 2;
            var top = _codeTop + v * _codeSide - moduleSide / 2;

            var x0 = Math.Clamp((int)Math.Floor(left), 0, _width);
            var y0 = Math.Clamp((int)Math.Floor(top), 0, _height);
            var x1 = Math.Clamp((int)Math.Ceiling(left + moduleSide), 0, _width);
            var y1 = Math.Clamp((int)Math.Ceiling(top + moduleSide), 0, _height);

            var area = (x1 - x0) * (y1 - y0);

            if (area <= 0)
                return ShapePart.None;

            if (Sum(_strokeSums, x0, y0, x1, y1) >= SolidCoverage * area)
                return _inkPart;

            if (Sum(_enclosedSums, x0, y0, x1, y1) >= TexturedCoverage * area)
                return ShapePart.Textured;

            return ShapePart.None;
        }

        /// <summary>
        /// Pixels that are neither pen nor code and cannot be reached from the edge of the drawing without crossing a
        /// line (after closing small gaps) or the code's square.
        /// </summary>
        private bool[,] FindEnclosed(bool[,] strokes, int gapToClose)
        {
            var walls = Erode(Dilate(strokes, gapToClose), gapToClose);
            var codeX0 = (int)Math.Floor(_codeLeft);
            var codeY0 = (int)Math.Floor(_codeTop);
            var codeX1 = (int)Math.Ceiling(_codeLeft + _codeSide);
            var codeY1 = (int)Math.Ceiling(_codeTop + _codeSide);

            bool InCode(int x, int y) => x >= codeX0 && x < codeX1 && y >= codeY0 && y < codeY1;

            bool Wall(int x, int y) => walls[y, x] || (_codeClosesAreas && InCode(x, y));

            // Flood fill from every open pixel on the edge: what it reaches is outside.
            var outside = new bool[_height, _width];
            var queue = new Queue<(int X, int Y)>();

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= _width || y >= _height || outside[y, x] || Wall(x, y))
                    return;

                outside[y, x] = true;
                queue.Enqueue((x, y));
            }

            for (var x = 0; x < _width; x++)
            {
                Visit(x, 0);
                Visit(x, _height - 1);
            }

            for (var y = 0; y < _height; y++)
            {
                Visit(0, y);
                Visit(_width - 1, y);
            }

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                Visit(x + 1, y);
                Visit(x - 1, y);
                Visit(x, y + 1);
                Visit(x, y - 1);
            }

            var enclosed = new bool[_height, _width];

            for (var y = 0; y < _height; y++)
            {
                for (var x = 0; x < _width; x++)
                    enclosed[y, x] = !outside[y, x] && !strokes[y, x] && !InCode(x, y);
            }

            if (_largestFilledArea < 1)
                RemoveAreasLargerThan(enclosed, (int)(_largestFilledArea * _width * _height));

            return enclosed;
        }

        /// <summary>
        /// Clears every connected area of <paramref name="mask"/> with more than <paramref name="maximumSize"/> pixels.
        /// </summary>
        private static void RemoveAreasLargerThan(bool[,] mask, int maximumSize)
        {
            int height = mask.GetLength(0), width = mask.GetLength(1);
            var seen = new bool[height, width];
            var area = new List<(int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();

            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    if (!mask[row, column] || seen[row, column])
                        continue;

                    area.Clear();
                    seen[row, column] = true;
                    queue.Enqueue((column, row));

                    while (queue.Count > 0)
                    {
                        var (x, y) = queue.Dequeue();
                        area.Add((x, y));

                        foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                        {
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height || seen[ny, nx] || !mask[ny, nx])
                                continue;

                            seen[ny, nx] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }

                    if (area.Count > maximumSize)
                    {
                        foreach (var (x, y) in area)
                            mask[y, x] = false;
                    }
                }
            }
        }

        private static bool[,] Dilate(bool[,] mask, int radius) => SquareFilter(mask, radius, any: true);

        private static bool[,] Erode(bool[,] mask, int radius) => SquareFilter(mask, radius, any: false);

        /// <summary>
        /// Dilation (any pixel in the square is set) or erosion (all are), as two one-dimensional passes.
        /// </summary>
        private static bool[,] SquareFilter(bool[,] mask, int radius, bool any)
        {
            int height = mask.GetLength(0), width = mask.GetLength(1);
            var horizontal = new bool[height, width];
            var result = new bool[height, width];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    horizontal[y, x] = Window(x, width, i => mask[y, i]);
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    result[y, x] = Window(y, height, i => horizontal[i, x]);
            }

            return result;

            bool Window(int centre, int length, Func<int, bool> value)
            {
                for (var i = Math.Max(0, centre - radius); i <= Math.Min(length - 1, centre + radius); i++)
                {
                    if (value(i) == any)
                        return any;
                }

                return !any;
            }
        }

        private static int[,] SummedArea(bool[,] mask)
        {
            int height = mask.GetLength(0), width = mask.GetLength(1);
            var sums = new int[height + 1, width + 1];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                    sums[y + 1, x + 1] = (mask[y, x] ? 1 : 0) + sums[y, x + 1] + sums[y + 1, x] - sums[y, x];
            }

            return sums;
        }

        private static int Sum(int[,] sums, int x0, int y0, int x1, int y1) => sums[y1, x1] - sums[y0, x1] - sums[y1, x0] + sums[y0, x0];
    }
}
