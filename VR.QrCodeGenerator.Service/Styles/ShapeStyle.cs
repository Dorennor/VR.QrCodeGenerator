using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// A style that draws a shape around the code (see <see cref="ShapedCodeBuilder"/>). Every animal style is one of
    /// these with its own <see cref="IShape"/>.
    /// </summary>
    public sealed class ShapeStyle : IQrStyle
    {
        private readonly IShape _shape;

        public ShapeStyle(EnumQrStyle style, IShape shape)
        {
            Style = style;
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));
        }

        public EnumQrStyle Style { get; }

        public bool[,] Apply(bool[,] code, string content) => ShapedCodeBuilder.Build(code, content, _shape);
    }
}
