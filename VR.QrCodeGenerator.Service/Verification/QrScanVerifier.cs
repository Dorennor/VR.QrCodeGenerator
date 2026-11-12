using VR.QrCodeGenerator.Service.Generation;

using ZXing;
using ZXing.Common;

namespace VR.QrCodeGenerator.Service.Verification
{
    /// <summary>
    /// Decodes a module matrix the way a scanner would see the saved image, to check that a decorated code still scans.
    /// </summary>
    public static class QrScanVerifier
    {
        /// <summary>
        /// Decoding depends on the pixel size (local thresholds, finder search), and a drawing can pass at one size and
        /// fail at another, so it is checked at small, medium and large module sizes.
        /// </summary>
        private static readonly int[] PixelsPerModule = [3, 5, 8];

        /// <summary>
        /// True when the matrix, with a standard quiet zone around it, decodes to exactly <paramref name="content"/> at
        /// every checked size. TryHarder stays off, so only results that scan easily pass.
        /// </summary>
        public static bool Scans(bool[,] modules, string content)
        {
            var padded = ModuleMatrix.AddMargin(modules, QrMatrixEncoder.QuietZoneModules);

            return PixelsPerModule.All(scale => Decode(padded, scale) == content);
        }

        private static string Decode(bool[,] modules, int scale)
        {
            var width = modules.Columns() * scale;
            var height = modules.Rows() * scale;
            var luminance = new byte[width * height];

            Array.Fill(luminance, (byte)255);

            for (var row = 0; row < modules.Rows(); row++)
            {
                for (var column = 0; column < modules.Columns(); column++)
                {
                    if (!modules[row, column])
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
