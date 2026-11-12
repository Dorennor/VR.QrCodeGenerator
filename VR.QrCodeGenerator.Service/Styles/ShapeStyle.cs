using VR.QrCodeGenerator.Service.Generation;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// A style that draws a shape around the code (see <see cref="ShapedCodeBuilder"/>). Every animal style is one of
    /// these with its own <see cref="IShape"/>, and so are a user's drawing and picture (<see cref="MaskShape"/>).
    /// </summary>
    public sealed class ShapeStyle : IQrStyle
    {
        private readonly IShape _shape;
        private readonly int _cellsPerModule;

        /// <param name="cellsPerModule">
        /// The shape's detail: 1 draws it in whole modules like the code (the Kurkod look); 2 or 3 draw it in smaller
        /// cells for smoother edges. The code itself is the same either way.
        /// </param>
        public ShapeStyle(IShape shape, int cellsPerModule = 1)
        {
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));

            if (cellsPerModule is < 1 or > 4)
                throw new ArgumentOutOfRangeException(nameof(cellsPerModule), cellsPerModule, "Use 1 to 4 cells per module.");

            _cellsPerModule = cellsPerModule;
        }

        public QrDrawing Apply(bool[,] code, string content) => ShapedCodeBuilder.Build(code, content, _shape, _cellsPerModule);
    }
}
