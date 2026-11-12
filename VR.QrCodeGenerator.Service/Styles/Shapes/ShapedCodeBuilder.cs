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
            var attempts = FinderGaps.Select(gap => (Gap: gap, WholeQuietZone: false))
                                     .Append((Gap: QrMatrixEncoder.QuietZoneModules, WholeQuietZone: true));

            foreach (var (gap, wholeQuietZone) in attempts)
            {
                for (var texture = 0; texture < TexturesPerGap; texture++)
                {
                    var drawing = new QrDrawing(Draw(cells, cellsPerModule, shape, unchecked(seed + texture * 7919), gap * cellsPerModule, wholeQuietZone), cellsPerModule);

                    if (QrScanVerifier.Scans(drawing, content))
                        return drawing;
                }
            }

            return new QrDrawing(code);
        }

        /// <param name="code">The code in cells: each module as cellsPerModule x cellsPerModule cells.</param>
        /// <param name="gap">The gap beside the finder patterns, in cells.</param>
        private static bool[,] Draw(bool[,] code, int cellsPerModule, IShape shape, int seed, int gap, bool keepWholeQuietZone)
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

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (InCode(row, column))
                    {
                        result[row, column] = code[row - top, column - left];
                        continue;
                    }

                    // Drawn for every module so the texture does not shift when the gap changes.
                    var textureDark = random.NextDouble() < TextureDensity;

                    if (Blocked(row, column))
                        continue;

                    result[row, column] = parts[row, column] switch
                    {
                        ShapePart.Solid => true,
                        ShapePart.Textured => OnEdge(row, column, ShapePart.Textured) || (textureDark && !CompletesDarkBlock(result, row, column)),
                        ShapePart.Light => OnEdge(row, column, ShapePart.Light),
                        _ => false
                    };
                }
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
