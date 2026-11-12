namespace VR.QrCodeGenerator.Service.Generation
{
    /// <summary>
    /// Helpers for module matrices: bool[row, column], true for a dark module. Matrices do not have to be square.
    /// </summary>
    public static class ModuleMatrix
    {
        public static int Rows(this bool[,] modules) => modules.GetLength(0);

        public static int Columns(this bool[,] modules) => modules.GetLength(1);

        /// <summary>
        /// A copy with <paramref name="margin"/> light modules on every side.
        /// </summary>
        public static bool[,] AddMargin(bool[,] modules, int margin)
        {
            var result = new bool[modules.Rows() + 2 * margin, modules.Columns() + 2 * margin];

            for (var row = 0; row < modules.Rows(); row++)
            {
                for (var column = 0; column < modules.Columns(); column++)
                    result[row + margin, column + margin] = modules[row, column];
            }

            return result;
        }

        /// <summary>
        /// A copy without the light rows and columns around the dark modules.
        /// </summary>
        public static bool[,] Crop(bool[,] modules)
        {
            int top = modules.Rows(), bottom = -1, left = modules.Columns(), right = -1;

            for (var row = 0; row < modules.Rows(); row++)
            {
                for (var column = 0; column < modules.Columns(); column++)
                {
                    if (!modules[row, column])
                        continue;

                    top = Math.Min(top, row);
                    bottom = Math.Max(bottom, row);
                    left = Math.Min(left, column);
                    right = Math.Max(right, column);
                }
            }

            if (bottom < 0)
                return new bool[0, 0];

            var cropped = new bool[bottom - top + 1, right - left + 1];

            for (var row = top; row <= bottom; row++)
            {
                for (var column = left; column <= right; column++)
                    cropped[row - top, column - left] = modules[row, column];
            }

            return cropped;
        }
    }
}
