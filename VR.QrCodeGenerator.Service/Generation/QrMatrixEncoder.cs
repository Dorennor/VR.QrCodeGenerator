using QRCoder;

namespace VR.QrCodeGenerator.Service.Generation
{
    /// <summary>
    /// Step 1 of generation: the text as a QR code's module matrix.
    /// </summary>
    public static class QrMatrixEncoder
    {
        /// <summary>
        /// Width of the standard quiet zone around a QR code, in modules.
        /// </summary>
        public const int QuietZoneModules = 4;

        /// <returns>The modules [row, column], true for dark, without a quiet zone.</returns>
        /// <exception cref="QRCoder.Exceptions.DataTooLongException">The content does not fit in a QR code at this error correction level.</exception>
        public static bool[,] Encode(string content, QRCodeGenerator.ECCLevel eccLevel)
        {
            using (QRCodeGenerator generator = new QRCodeGenerator())
            using (QRCodeData data = generator.CreateQrCode(content, eccLevel))
            {
                // QRCoder's matrix always includes the quiet zone; it is added back later only when asked for.
                var size = data.ModuleMatrix.Count - 2 * QuietZoneModules;
                var modules = new bool[size, size];

                for (var row = 0; row < size; row++)
                {
                    for (var column = 0; column < size; column++)
                        modules[row, column] = data.ModuleMatrix[row + QuietZoneModules][column + QuietZoneModules];
                }

                return modules;
            }
        }
    }
}
