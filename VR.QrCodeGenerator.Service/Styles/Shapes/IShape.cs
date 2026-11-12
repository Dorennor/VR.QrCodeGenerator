namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// What fills each module around a QR code.
    /// </summary>
    public enum ShapePart
    {
        /// <summary>Outside the shape: background.</summary>
        None,

        /// <summary>QR-like random texture with a dark outline, like the body of the Kurkod chicken.</summary>
        Textured,

        /// <summary>Always dark: beaks, ears, legs, whiskers.</summary>
        Solid,

        /// <summary>Always light, with a dark outline: a muzzle, a belly patch.</summary>
        Light
    }

    /// <summary>
    /// A point in code widths, measured from the QR code's top-left corner: (0, 0) to (1, 1) is the code itself,
    /// negative values are left of or above it.
    /// </summary>
    public readonly record struct ShapePoint(double U, double V);

    /// <summary>
    /// Room around the code for the shape, as a fraction of the code's size.
    /// </summary>
    public readonly record struct ShapeMargins(double Left, double Top, double Right, double Bottom);

    /// <summary>
    /// A silhouette drawn around a QR code by <see cref="ShapedCodeBuilder"/>. The code is always kept intact; a shape
    /// only decides what goes around it. Coordinates are in code widths (see <see cref="ShapePoint"/>), so the same
    /// shape fits every code size.
    /// </summary>
    /// <remarks>
    /// Shapes can be geometric (<see cref="ChickenShape"/>, <see cref="CatShape"/>, <see cref="DogShape"/>) or, for
    /// example, come from a picture: <see cref="Classify"/> then looks up the pixel under (u, v).
    /// </remarks>
    public interface IShape
    {
        ShapeMargins Margins { get; }

        /// <summary>
        /// Centres of the eyes, drawn as a dark pupil in a light square. Empty for none.
        /// </summary>
        IReadOnlyList<ShapePoint> Eyes { get; }

        /// <summary>
        /// What fills the module whose centre is at (u, v).
        /// </summary>
        /// <param name="size">The code's size in modules, for parts that should be a whole number of modules thick.</param>
        ShapePart Classify(double u, double v, int size);
    }
}
