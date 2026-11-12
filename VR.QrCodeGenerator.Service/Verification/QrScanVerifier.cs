using VR.QrCodeGenerator.Service.Generation;

using ZXing;
using ZXing.Common;

namespace VR.QrCodeGenerator.Service.Verification
{
    /// <summary>
    /// Decodes a drawing the way a scanner would see the saved image, to check that a decorated code still scans.
    /// </summary>
    public static class QrScanVerifier
    {
        /// <summary>
        /// Decoding depends on the pixel size (local thresholds, finder search), and a drawing can pass at one size and
        /// fail at another, so it is checked from small to large module sizes. 12 px caught a code that passed at 3 to 8.
        /// </summary>
        private static readonly int[] PixelsPerModule = [3, 5, 8, 12];

        /// <summary>
        /// True when the drawing, with a standard quiet zone around it, decodes to exactly <paramref name="content"/> at
        /// every checked size. TryHarder stays off, so only results that scan easily pass.
        /// </summary>
        public static bool Scans(QrDrawing drawing, string content)
        {
            var padded = drawing.WithMargin(QrMatrixEncoder.QuietZoneModules);

            // Whole pixels per cell, as close to each module size as the cells allow; each size checked once.
            var scales = PixelsPerModule.Select(pixels => Math.Max(1, (int)Math.Round((double)pixels / drawing.CellsPerModule))).Distinct();

            return scales.All(scale => Decode(padded.Cells, scale) == content);
        }

        private static string Decode(bool[,] cells, int scale)
        {
            var width = cells.Columns() * scale;
            var height = cells.Rows() * scale;
            var luminance = new byte[width * height];

            Array.Fill(luminance, (byte)255);

            for (var row = 0; row < cells.Rows(); row++)
            {
                for (var column = 0; column < cells.Columns(); column++)
                {
                    if (!cells[row, column])
                        continue;

                    for (var y = 0; y < scale; y++)
                        Array.Fill(luminance, (byte)0, (row * scale + y) * width + column * scale, scale);
                }
            }

            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = false,
                Options = new DecodingOptions { PossibleFormats = [BarcodeFormat.QR_CODE], TryHarder = false }
            };

            try
            {
                return reader.Decode(new RGBLuminanceSource(luminance, width, height, RGBLuminanceSource.BitmapFormat.Gray8))?.Text;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
