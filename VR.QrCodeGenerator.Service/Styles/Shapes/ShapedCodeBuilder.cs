using VR.QrCodeGenerator.Service.Generation;
using VR.QrCodeGenerator.Service.Verification;

namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// Draws an <see cref="IShape"/> around a QR code in a blocky, QR-like style, like the Kurkod meme. The QR code
    /// itself is never changed, so no error correction is spent; only the space around it is used.
    /// </summary>
    /// <remarks>
    /// Scanners locate a code by its three finder patterns (the big squares) and need light space beside them, so the
    /// drawing keeps a gap there. Every result is decoded before it is returned; when it does not scan the gap grows,
    /// ending with a full standard quiet zone around the code, which always scans.
    /// </remarks>
    public static class ShapedCodeBuilder
    {
        private const double TextureDensity = 0.5;

        /// <summary>
        /// How far the dithering threshold moves around 0.5 with the seed: small, so the picture looks the same, but
        /// enough that a retry is a different drawing.
        /// </summary>
        private const double DitherJitter = 0.2;

        // A clean render decodes even with a 1-module gap, but a phone camera sees blur and perspective: start with 2.
        private static readonly int[] FinderGaps = [2, 3];

        // Random texture can, rarely, still confuse a scanner; another texture usually does not.
        private const int TexturesPerGap = 3;

        /// <param name="code">The QR code's modules [row, column], without a quiet zone.</param>
        /// <param name="content">The encoded text: seeds the texture (the same content always gives the same drawing) and checks the result scans.</param>
        /// <param name="cellsPerModule">
        /// 1 draws the shape in whole modules; 2 or 3 in smaller cells for smoother edges and finer texture. Gaps and
        /// the quiet zone stay the same in modules.
        /// </param>
        /// <returns>
        /// The drawing without a quiet zone, cropped to the shape; only ever a drawing that scans. Tries a gap beside
        /// the finder patterns, then other textures, then wider gaps, then a full quiet zone around the code. If nothing
        /// scans, which tests have not seen happen, the plain code is returned.
        /// </returns>
        public static QrDrawing Build(bool[,] code, string content, IShape shape, int cellsPerModule = 1)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(shape);

            var seed = StableHash(content ?? string.Empty);
            var cells = QrDrawing.Expand(code, cellsPerModule);
            var blended = BlendedCode.For(code, shape, cellsPerModule);

            // A blended code first, at the asked strength and then pulled further towards the module's color; then the
            // solid code.
            var blendBiases = blended is null ? [] : FirmerBiases.Where(bias => bias > blended.Bias).Prepend(blended.Bias);
            var attempts = blendBiases.Select(bias => (Gap: FinderGaps[0], WholeQuietZone: false, Blend: blended with { Bias = bias }))
                           .Concat(FinderGaps.Select(gap => (Gap: gap, WholeQuietZone: false, Blend: (BlendedCode)null)))
                           .Append((Gap: QrMatrixEncoder.QuietZoneModules, WholeQuietZone: true, Blend: null));

            foreach (var (gap, wholeQuietZone, blend) in attempts)
            {
                for (var texture = 0; texture < TexturesPerGap; texture++)
                {
                    var drawing = new QrDrawing(Draw(cells, cellsPerModule, shape, unchecked(seed + texture * 7919), gap * cellsPerModule, wholeQuietZone, blend), cellsPerModule);

                    if (QrScanVerifier.Scans(drawing, content))
                        return drawing;
                }
            }

            return new QrDrawing(code);
        }

        /// <summary>
        /// When a blended code does not scan at the asked strength, these firmer pulls towards the module's own color
        /// are tried before the solid code.
        /// </summary>
        private static readonly double[] FirmerBiases = [0.35, 0.6];

        /// <summary>
        /// The code drawn into the picture: which modules stay solid and how far borders lean to the module's color.
        /// </summary>
        private sealed record BlendedCode(bool[,] FixedModules, int CentreStart, int CentreSize, double Bias)
        {
            /// <summary>
            /// Null when the shape has no tones, does not ask for blending, or the cells are too coarse for a border.
            /// </summary>
            public static BlendedCode For(bool[,] code, IShape shape, int cellsPerModule)
            {
                if (shape is not IToneShape { CodeBlendBias: { } bias } || cellsPerModule < 3)
                    return null;

                // A one-cell border all round: the centre is 1 x 1 of 3 x 3, 2 x 2 of 4 x 4.
                return new BlendedCode(FunctionPatterns.Mask(code.Rows()), 1, cellsPerModule - 2, bias);
            }

            public bool IsExact(int moduleRow, int moduleColumn, int cellRow, int cellColumn) =>
                FixedModules[moduleRow, moduleColumn]
                || (cellRow >= CentreStart && cellRow < CentreStart + CentreSize && cellColumn >= CentreStart && cellColumn < CentreStart + CentreSize);
        }

        /// <param name="code">The code in cells: each module as cellsPerModule x cellsPerModule cells.</param>
        /// <param name="gap">The gap beside the finder patterns, in cells.</param>
        /// <param name="blend">How to draw the code into the picture, or null for the solid code.</param>
        private static bool[,] Draw(bool[,] code, int cellsPerModule, IShape shape, int seed, int gap, bool keepWholeQuietZone, BlendedCode blend)
        {
            // From here on everything is in cells: the code is `size` cells wide, a finder pattern 7 modules.
            var size = code.Rows();
            var finder = 7 * cellsPerModule;
            var margins = shape.Margins;

            var left = (int)Math.Ceiling(margins.Left * size);
            var top = (int)Math.Ceiling(margins.Top * size);
            var rows = top + size + (int)Math.Ceiling(margins.Bottom * size);
            var columns = left + size + (int)Math.Ceiling(margins.Right * size);

            // What fills each module; module centres are measured in code widths from the code's corner.
            var parts = new ShapePart[rows, columns];

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                    parts[row, column] = shape.Classify((column - left + 0.5) / size, (row - top + 0.5) / size, size);
            }

            bool InCode(int row, int column) => row >= top && row < top + size && column >= left && column < left + size;

            bool Blocked(int row, int column)
            {
                if (keepWholeQuietZone)
                    return Distance(row, column, top, left, size) <= gap;

                // The three finder patterns are 7 x 7 modules in the top-left, top-right and bottom-left corners.
                return Distance(row, column, top, left, finder) <= gap
                       || Distance(row, column, top, left + size - finder, finder) <= gap
                       || Distance(row, column, top + size - finder, left, finder) <= gap;
            }

            ShapePart PartAt(int row, int column) => row < 0 || column < 0 || row >= rows || column >= columns ? ShapePart.None : parts[row, column];

            // A textured part gets a dark edge where it meets the background, a light part wherever it meets anything else.
            bool OnEdge(int row, int column, ShapePart part)
            {
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    var neighbour = PartAt(row + dr, column + dc);

                    if (InCode(row + dr, column + dc))
                        continue;

                    if (part == ShapePart.Textured ? neighbour == ShapePart.None : neighbour != ShapePart.Light)
                        return true;
                }

                return false;
            }

            var random = new Random(seed);
            var result = new bool[rows, columns];

            // A shape with tones is dithered: each textured cell is dark or light by the picture's darkness, and the
            // rounding error spreads to the next cells (Floyd-Steinberg), so the picture's details and shading survive.
            var tones = shape as IToneShape;
            var error = tones is null ? null : new double[rows + 1, columns + 2];
            var outline = tones?.DrawOutline ?? true;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (InCode(row, column))
                    {
                        var codeRow = row - top;
                        var codeColumn = column - left;
                        var moduleDark = code[codeRow, codeColumn];

                        // A blended module's border follows the picture, leaning to the module's color; its centre and
                        // the function patterns stay exact.
                        result[row, column] = blend is null || blend.IsExact(codeRow / cellsPerModule, codeColumn / cellsPerModule, codeRow % cellsPerModule, codeColumn % cellsPerModule)
                            ? moduleDark
                            : Dither(row, column, (column - left + 0.5) / size, (row - top + 0.5) / size, 0.5, (moduleDark ? 1 : 0, blend.Bias));

                        continue;
                    }

                    // Drawn for every cell so the texture does not shift when the gap changes; with tones it jitters
                    // the threshold a little, so another seed gives a slightly different dither to try.
                    var chance = random.NextDouble();

                    if (Blocked(row, column))
                        continue;

                    result[row, column] = parts[row, column] switch
                    {
                        ShapePart.Solid => true,
                        ShapePart.Textured when outline && OnEdge(row, column, ShapePart.Textured) => true,
                        ShapePart.Textured when tones is not null => Dither(row, column, (column - left + 0.5) / size, (row - top + 0.5) / size, chance),
                        ShapePart.Textured => chance < TextureDensity && !CompletesDarkBlock(result, row, column),
                        ShapePart.Light => OnEdge(row, column, ShapePart.Light),
                        _ => false
                    };
                }
            }

            // lean: a darkness to pull the picture towards, and how far (0 none, 1 all the way), for blended code borders.
            bool Dither(int row, int column, double u, double v, double chance, (double Target, double Amount) lean = default)
            {
                var darkness = tones.Darkness(u, v, size);
                var value = darkness + (lean.Target - darkness) * lean.Amount + error[row, column + 1];
                var dark = value > 0.5 + (chance - 0.5) * DitherJitter;
                var rest = value - (dark ? 1 : 0);

                error[row, column + 2] += rest * 7 / 16;
                error[row + 1, column] += rest * 3 / 16;
                error[row + 1, column + 1] += rest * 5 / 16;
                error[row + 1, column + 2] += rest * 1 / 16;

                return dark;
            }

            foreach (var eye in shape.Eyes)
            {
                var eyeRow = top + (int)Math.Floor(eye.V * size);
                var eyeColumn = left + (int)Math.Floor(eye.U * size);

                DrawEye(result, parts, eyeRow, eyeColumn, size, InCode, Blocked);
            }

            return ModuleMatrix.Crop(result);
        }

        /// <summary>
        /// A dark pupil in a light square, so it stands out from the texture. The light ring is wider than a finder
        /// pattern's (1:1:3:1:1), so scanners cannot mistake the eye for one. Skipped when it would not fit in the shape.
        /// </summary>
        private static void DrawEye(bool[,] result, ShapePart[,] parts, int eyeRow, int eyeColumn, int size, Func<int, int, bool> inCode, Func<int, int, bool> blocked)
        {
            var pupil = size >= 57 ? 1 : 0;
            var radius = pupil + (size >= 33 ? 2 : 1);

            for (var dr = -radius; dr <= radius; dr++)
            {
                for (var dc = -radius; dc <= radius; dc++)
                {
                    var r = eyeRow + dr;
                    var c = eyeColumn + dc;

                    if (r < 0 || c < 0 || r >= result.Rows() || c >= result.Columns() || inCode(r, c) || blocked(r, c) || parts[r, c] == ShapePart.None)
                        return;
                }
            }

            for (var dr = -radius; dr <= radius; dr++)
            {
                for (var dc = -radius; dc <= radius; dc++)
                    result[eyeRow + dr, eyeColumn + dc] = Math.Max(Math.Abs(dr), Math.Abs(dc)) <= pupil;
            }
        }

        /// <summary>
        /// True when a dark module here would finish a 3 x 3 dark block above and left of it. Such a block is the centre
        /// of a finder pattern, so texture never makes one: a look-alike can make a scanner lock onto the wrong spot.
        /// </summary>
        private static bool CompletesDarkBlock(bool[,] result, int row, int column)
        {
            if (row < 2 || column < 2)
                return false;

            for (var r = row - 2; r <= row; r++)
            {
                for (var c = column - 2; c <= column; c++)
                {
                    if ((r != row || c != column) && !result[r, c])
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Chebyshev distance from a module to a square block of modules; 0 inside it.
        /// </summary>
        private static int Distance(int row, int column, int blockTop, int blockLeft, int blockSize)
        {
            var dr = row < blockTop ? blockTop - row : row >= blockTop + blockSize ? row - (blockTop + blockSize - 1) : 0;
            var dc = column < blockLeft ? blockLeft - column : column >= blockLeft + blockSize ? column - (blockLeft + blockSize - 1) : 0;

            return Math.Max(dr, dc);
        }

        /// <summary>
        /// FNV-1a: unlike string.GetHashCode it is the same in every run, so the preview and the saved file match.
        /// </summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = 2166136261;

                foreach (var character in text)
                {
                    hash ^= character;
                    hash *= 16777619;
                }

                return (int)hash;
            }
        }
    }
}
