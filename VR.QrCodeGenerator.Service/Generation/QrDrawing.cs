namespace VR.QrCodeGenerator.Service.Generation
{
    /// <summary>
    /// What a style produces and a renderer draws: a grid of dark and light cells. A cell is a whole module for most
    /// styles; a detailed shape style splits each module into 2 x 2 or 3 x 3 cells so the shape around the code has
    /// smoother edges. The code's own modules then cover whole blocks of cells and are unchanged.
    /// </summary>
    public sealed class QrDrawing
    {
        /// <param name="cells">The cells [row, column], true for dark. Need not be square.</param>
        /// <param name="cellsPerModule">How many cells make one module side.</param>
        public QrDrawing(bool[,] cells, int cellsPerModule = 1)
        {
            ArgumentNullException.ThrowIfNull(cells);

            if (cellsPerModule < 1)
                throw new ArgumentOutOfRangeException(nameof(cellsPerModule), cellsPerModule, "A module is at least one cell.");

            Cells = cells;
            CellsPerModule = cellsPerModule;
        }

        public bool[,] Cells { get; }

        public int CellsPerModule { get; }

        public int Rows => Cells.Rows();

        public int Columns => Cells.Columns();

        /// <summary>
        /// A copy with <paramref name="modules"/> modules of light cells on every side (the quiet zone).
        /// </summary>
        public QrDrawing WithMargin(int modules) => new QrDrawing(ModuleMatrix.AddMargin(Cells, modules * CellsPerModule), CellsPerModule);

        /// <summary>
        /// Each module as <paramref name="cellsPerModule"/> x <paramref name="cellsPerModule"/> cells.
        /// </summary>
        public static bool[,] Expand(bool[,] modules, int cellsPerModule)
        {
            if (cellsPerModule == 1)
                return modules;

            var cells = new bool[modules.Rows() * cellsPerModule, modules.Columns() * cellsPerModule];

            for (var row = 0; row < cells.Rows(); row++)
            {
                for (var column = 0; column < cells.Columns(); column++)
                    cells[row, column] = modules[row / cellsPerModule, column / cellsPerModule];
            }

            return cells;
        }
    }
}
