using ZXing;
using ZXing.Common;

namespace VR.QrCodeGenerator.Service.Helpers
{
    /// <summary>
    /// Builds a "Kurkod": a QR code that is the body of a chicken. The head, comb, beak, tail, legs and a rounded body
    /// are drawn around the code in the same blocky, QR-like texture. The QR code itself is never changed, so no error
    /// correction is spent; only the space around it is used.
    /// </summary>
    /// <remarks>
    /// Scanners locate a code by its three finder patterns (the big squares) and need light space beside them, so the
    /// drawing keeps a gap there. Every result is decoded before it is returned; when it does not scan the gap grows,
    /// ending with a full standard quiet zone around the code, which always scans.
    /// </remarks>
    public static class KurkodBuilder
    {
        // Room around the code for the chicken, as a fraction of the code's size.
        private const double MarginLeft = 0.34;
        private const double MarginTop = 0.52;
        private const double MarginRight = 0.42;
        private const double MarginBottom = 0.42;

        private const double TextureDensity = 0.5;

        // Standard quiet zone of a QR code, in modules.
        private const int QuietZoneModules = 4;

        private enum Part
        {
            None,
            Textured,
            Solid
        }

        private static readonly (double U, double V)[] NeckPolygon = [(-0.06, -0.14), (0.22, -0.14), (0.32, 0.02), (-0.10, 0.02)];

        private static readonly (double U, double V)[] BeakPolygon = [(-0.08, -0.31), (-0.08, -0.19), (-0.27, -0.25)];

        private static readonly (double U, double V)[] TailPolygon =
        [
            (0.80, 0.22), (0.94, -0.08), (1.06, -0.36), (1.18, -0.30), (1.24, -0.16), (1.30, -0.26),
            (1.36, -0.04), (1.40, 0.14), (1.34, 0.38), (1.14, 0.52)
        ];

        /// <summary>
        /// Returns the chicken as a module matrix [row, column] without a quiet zone. True is a dark module.
        /// </summary>
        /// <param name="qr">The QR code's modules [row, column], without a quiet zone.</param>
        /// <param name="content">The encoded text: seeds the texture (the same content always gives the same chicken) and checks the result scans.</param>
        public static bool[,] Build(bool[,] qr, string content)
        {
            ArgumentNullException.ThrowIfNull(qr);

            var seed = StableHash(content ?? string.Empty);

            // A clean render decodes even with a 1-module gap, but a phone camera sees blur and perspective: start with 2.
            foreach (var finderGap in new[] { 2, 3 })
            {
                var chicken = Draw(qr, seed, finderGap, keepWholeQuietZone: false);

                if (Scans(chicken, content))
                    return chicken;
            }

            return Draw(qr, seed, QuietZoneModules, keepWholeQuietZone: true);
        }

        private static bool[,] Draw(bool[,] qr, int seed, int gap, bool keepWholeQuietZone)
        {
            var size = qr.GetLength(0);

            var left = (int)Math.Ceiling(MarginLeft * size);
            var top = (int)Math.Ceiling(MarginTop * size);
            var rows = top + size + (int)Math.Ceiling(MarginBottom * size);
            var columns = left + size + (int)Math.Ceiling(MarginRight * size);

            // Which part of the chicken each module belongs to; module centres are measured in code widths from its corner.
            var parts = new Part[rows, columns];

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                    parts[row, column] = Classify((column - left + 0.5) / size, (row - top + 0.5) / size, size);
            }

            bool InQr(int row, int column) => row >= top && row < top + size && column >= left && column < left + size;

            bool Blocked(int row, int column)
            {
                if (keepWholeQuietZone)
                    return Distance(row, column, top, left, size) <= gap;

                // The three finder patterns are 7 x 7 modules in the top-left, top-right and bottom-left corners.
                return Distance(row, column, top, left, 7) <= gap
                       || Distance(row, column, top, left + size - 7, 7) <= gap
                       || Distance(row, column, top + size - 7, left, 7) <= gap;
            }

            bool IsOutline(int row, int column)
            {
                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    var r = row + dr;
                    var c = column + dc;

                    if (r < 0 || c < 0 || r >= rows || c >= columns || (parts[r, c] == Part.None && !InQr(r, c)))
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
                    if (InQr(row, column))
                    {
                        result[row, column] = qr[row - top, column - left];
                        continue;
                    }

                    // Drawn for every module so the texture does not shift when the gap changes.
                    var textureDark = random.NextDouble() < TextureDensity;

                    if (Blocked(row, column))
                        continue;

                    result[row, column] = parts[row, column] switch
                    {
                        Part.Solid => true,
                        Part.Textured => IsOutline(row, column) || textureDark,
                        _ => false
                    };
                }
            }

            DrawEye(result, parts, top, left, size, InQr, Blocked);

            return Crop(result);
        }

        /// <summary>
        /// A dark pupil in a light square, so it stands out from the texture of the head. The light ring is wider than
        /// a finder pattern's (1:1:3:1:1), so scanners cannot mistake the eye for one.
        /// </summary>
        private static void DrawEye(bool[,] result, Part[,] parts, int top, int left, int size, Func<int, int, bool> inQr, Func<int, int, bool> blocked)
        {
            var pupil = size >= 57 ? 1 : 0;
            var radius = pupil + (size >= 33 ? 2 : 1);
            var eyeRow = top + (int)Math.Floor(-0.28 * size);
            var eyeColumn = left + (int)Math.Floor(0.01 * size);

            for (var dr = -radius; dr <= radius; dr++)
            {
                for (var dc = -radius; dc <= radius; dc++)
                {
                    var r = eyeRow + dr;
                    var c = eyeColumn + dc;

                    if (r < 0 || c < 0 || r >= result.GetLength(0) || c >= result.GetLength(1) || inQr(r, c) || blocked(r, c) || parts[r, c] == Part.None)
                        return;
                }
            }

            for (var dr = -radius; dr <= radius; dr++)
            {
                for (var dc = -radius; dc <= radius; dc++)
                {
                    result[eyeRow + dr, eyeColumn + dc] = Math.Max(Math.Abs(dr), Math.Abs(dc)) <= pupil;
                }
            }
        }

        private static Part Classify(double u, double v, int size)
        {
            // Legs and toes: solid lines one module (or more on big codes) thick, snapped to the module grid.
            var thickness = Math.Max(1, (int)Math.Round(0.035 * size));

            foreach (var legU in new[] { 0.36, 0.62 })
            {
                var legStart = Math.Floor(legU * size) / size;
                var toesTop = Math.Floor(1.30 * size) / size;

                if (u >= legStart && u < legStart + (double)thickness / size && v >= 1.0 && v < toesTop + (double)thickness / size)
                    return Part.Solid;

                if (v >= toesTop && v < toesTop + (double)thickness / size && u >= legStart - 0.11 && u < legStart + 0.06)
                    return Part.Solid;
            }

            if (InPolygon(u, v, BeakPolygon)
                || InCircle(u, v, -0.06, -0.12, 0.05)                      // wattle
                || InCircle(u, v, -0.03, -0.42, 0.06)                      // comb
                || InCircle(u, v, 0.07, -0.46, 0.07)
                || InCircle(u, v, 0.18, -0.42, 0.06))
                return Part.Solid;

            var textured = InCircle(u, v, 0.06, -0.25, 0.17)               // head
                           || InPolygon(u, v, NeckPolygon)
                           || InPolygon(u, v, TailPolygon)
                           || InEllipse(u, v, 0.5, 0.5, 0.66, 0.62);      // body, rounder than the square code

            return textured ? Part.Textured : Part.None;
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

        private static bool InCircle(double u, double v, double cu, double cv, double radius) => InEllipse(u, v, cu, cv, radius, radius);

        private static bool InEllipse(double u, double v, double cu, double cv, double ru, double rv)
        {
            var du = (u - cu) / ru;
            var dv = (v - cv) / rv;

            return du * du + dv * dv <= 1;
        }

        private static bool InPolygon(double u, double v, (double U, double V)[] polygon)
        {
            var inside = false;

            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].V > v) != (polygon[j].V > v)
                    && u < (polygon[j].U - polygon[i].U) * (v - polygon[i].V) / (polygon[j].V - polygon[i].V) + polygon[i].U)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Trims empty rows and columns so the image ends where the chicken does.
        /// </summary>
        private static bool[,] Crop(bool[,] matrix)
        {
            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);
            int top = rows, bottom = -1, left = columns, right = -1;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (!matrix[row, column])
                        continue;

                    top = Math.Min(top, row);
                    bottom = Math.Max(bottom, row);
                    left = Math.Min(left, column);
                    right = Math.Max(right, column);
                }
            }

            var cropped = new bool[bottom - top + 1, right - left + 1];

            for (var row = top; row <= bottom; row++)
            {
                for (var column = left; column <= right; column++)
                    cropped[row - top, column - left] = matrix[row, column];
            }

            return cropped;
        }

        /// <summary>
        /// Decodes the drawing with a standard quiet zone around it, as a scanner would see the saved image.
        /// TryHarder stays off so only results that scan easily pass.
        /// </summary>
        private static bool Scans(bool[,] matrix, string content)
        {
            const int scale = 3;

            int rows = matrix.GetLength(0), columns = matrix.GetLength(1);
            var width = (columns + 2 * QuietZoneModules) * scale;
            var height = (rows + 2 * QuietZoneModules) * scale;
            var luminance = new byte[width * height];

            Array.Fill(luminance, (byte)255);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (!matrix[row, column])
                        continue;

                    for (var y = 0; y < scale; y++)
                    {
                        var start = ((row + QuietZoneModules) * scale + y) * width + (column + QuietZoneModules) * scale;
                        Array.Fill(luminance, (byte)0, start, scale);
                    }
                }
            }

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = false,
                Options = new DecodingOptions { PossibleFormats = [BarcodeFormat.QR_CODE], TryHarder = false }
            };

            try
            {
                return reader.Decode(new RGBLuminanceSource(luminance, width, height, RGBLuminanceSource.BitmapFormat.Gray8))?.Text == content;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// FNV-1a: unlike string.GetHashCode it is the same in every run, so preview and saved file match.
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
