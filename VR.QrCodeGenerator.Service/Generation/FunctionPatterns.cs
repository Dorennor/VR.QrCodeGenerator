namespace VR.QrCodeGenerator.Service.Generation
{
    /// <summary>
    /// The modules a scanner uses to find and read the code rather than the data: finder patterns with their
    /// separators, timing patterns, alignment patterns, format and version information. Styles that blend a picture
    /// into the code keep these exact.
    /// </summary>
    public static class FunctionPatterns
    {
        // Centre coordinates of the alignment patterns per version (ISO/IEC 18004, Annex E). Index = version.
        private static readonly int[][] AlignmentCentres =
        [
            [], [],
            [6, 18], [6, 22], [6, 26], [6, 30], [6, 34],
            [6, 22, 38], [6, 24, 42], [6, 26, 46], [6, 28, 50], [6, 30, 54], [6, 32, 58], [6, 34, 62],
            [6, 26, 46, 66], [6, 26, 48, 70], [6, 26, 50, 74], [6, 30, 54, 78], [6, 30, 56, 82], [6, 30, 58, 86], [6, 34, 62, 90],
            [6, 28, 50, 72, 94], [6, 26, 50, 74, 98], [6, 30, 54, 78, 102], [6, 28, 54, 80, 106], [6, 32, 58, 84, 110], [6, 30, 58, 86, 114], [6, 34, 62, 90, 118],
            [6, 26, 50, 74, 98, 122], [6, 30, 54, 78, 102, 126], [6, 26, 52, 78, 104, 130], [6, 30, 56, 82, 108, 134], [6, 34, 60, 86, 112, 138], [6, 30, 58, 86, 114, 142], [6, 34, 62, 90, 118, 146],
            [6, 30, 54, 78, 102, 126, 150], [6, 24, 50, 76, 102, 128, 154], [6, 28, 54, 80, 106, 132, 158], [6, 32, 58, 84, 110, 136, 162], [6, 26, 54, 82, 110, 138, 166], [6, 30, 58, 86, 114, 142, 170]
        ];

        /// <param name="size">The code's size in modules, without a quiet zone: 21 to 177.</param>
        /// <returns>[row, column], true for a function module.</returns>
        public static bool[,] Mask(int size)
        {
            var version = (size - 17) / 4;

            if (version < 1 || version > 40 || size != 17 + 4 * version)
                throw new ArgumentOutOfRangeException(nameof(size), size, "Not the size of a QR code.");

            var mask = new bool[size, size];

            void Fill(int top, int left, int height, int width)
            {
                for (var row = Math.Max(0, top); row < Math.Min(size, top + height); row++)
                {
                    for (var column = Math.Max(0, left); column < Math.Min(size, left + width); column++)
                        mask[row, column] = true;
                }
            }

            // Finder patterns with separators, and the format information next to them (9 x 9 / 9 x 8 / 8 x 9).
            Fill(0, 0, 9, 9);
            Fill(0, size - 8, 9, 8);
            Fill(size - 8, 0, 8, 9);

            // Timing patterns.
            Fill(6, 0, 1, size);
            Fill(0, 6, size, 1);

            // Version information, from version 7: 6 x 3 blocks beside the top-right and bottom-left finders.
            if (version >= 7)
            {
                Fill(0, size - 11, 6, 3);
                Fill(size - 11, 0, 3, 6);
            }

            // Alignment patterns: every pair of centres except those under a finder pattern.
            var centres = AlignmentCentres[version];

            foreach (var row in centres)
            {
                foreach (var column in centres)
                {
                    var underFinder = (row == 6 && column == 6) || (row == 6 && column == centres[^1]) || (row == centres[^1] && column == 6);

                    if (!underFinder)
                        Fill(row - 2, column - 2, 5, 5);
                }
            }

            return mask;
        }
    }
}
