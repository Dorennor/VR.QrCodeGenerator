using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// A style that draws a shape around the code (see <see cref="ShapedCodeBuilder"/>). Every animal style is one of
    /// these with its own <see cref="IShape"/>, and so is a user's drawing (<see cref="MaskShape"/>).
    /// </summary>
    public sealed class ShapeStyle : IQrStyle
    {
        private readonly IShape _shape;

        public ShapeStyle(IShape shape)
        {
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));
        }

        public bool[,] Apply(bool[,] code, string content) => ShapedCodeBuilder.Build(code, content, _shape);
    }
}
