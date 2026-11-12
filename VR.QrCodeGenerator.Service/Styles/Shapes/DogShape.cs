using static VR.QrCodeGenerator.Service.Styles.Shapes.ShapeGeometry;

namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// A sitting dog seen from the front. The code is its body; the head with floppy ears, a light muzzle and a nose
    /// sits on top, a wagging tail on the right, two front paws below.
    /// </summary>
    public sealed class DogShape : IShape
    {
        // Points up and out from the right side of the body.
        private static readonly ShapePoint[] Tail = [new(1.02, 0.42), new(1.16, 0.22), new(1.26, 0.00), new(1.30, -0.16)];

        public ShapeMargins Margins { get; } = new(0.18, 0.62, 0.40, 0.20);

        public IReadOnlyList<ShapePoint> Eyes { get; } = [new(0.39, -0.38), new(0.61, -0.38)];

        public ShapePart Classify(double u, double v, int size)
        {
            if (InEllipse(u, v, 0.50, -0.20, 0.06, 0.04))                                         // nose
                return ShapePart.Solid;

            if (InEllipse(u, v, 0.50, -0.17, 0.15, 0.10))                                         // muzzle
                return ShapePart.Light;

            if (InEllipse(u, v, 0.20, -0.24, 0.09, 0.21) || InEllipse(u, v, 0.80, -0.24, 0.09, 0.21)   // floppy ears
                || InEllipse(u, v, 0.32, 1.06, 0.13, 0.08) || InEllipse(u, v, 0.68, 1.06, 0.13, 0.08))  // paws
                return ShapePart.Solid;

            if (InCircle(u, v, 0.50, -0.31, 0.27)                                                 // head
                || InEllipse(u, v, 0.50, 0.52, 0.62, 0.60)                                        // body
                || NearPolyline(u, v, 0.07, Tail))
                return ShapePart.Textured;

            return ShapePart.None;
        }
    }
}
