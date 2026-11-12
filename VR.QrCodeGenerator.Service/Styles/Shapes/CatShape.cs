using static VR.QrCodeGenerator.Service.Styles.Shapes.ShapeGeometry;

namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// A sitting cat seen from the front. The code is its body; the head with pointed ears, whiskers and a nose sits on
    /// top, a curled tail on the right, two front paws below.
    /// </summary>
    public sealed class CatShape : IShape
    {
        private static readonly ShapePoint[] LeftEar = [new(0.22, -0.40), new(0.27, -0.80), new(0.47, -0.54)];

        private static readonly ShapePoint[] RightEar = [new(0.78, -0.40), new(0.73, -0.80), new(0.53, -0.54)];

        private static readonly ShapePoint[] Nose = [new(0.44, -0.22), new(0.56, -0.22), new(0.50, -0.15)];

        // Curls up from the bottom right corner of the body.
        private static readonly ShapePoint[] Tail = [new(0.95, 1.02), new(1.18, 0.98), new(1.32, 0.80), new(1.34, 0.56), new(1.26, 0.38), new(1.14, 0.32)];

        private static readonly ShapePoint[][] Whiskers =
        [
            [new(0.30, -0.20), new(-0.02, -0.28)],
            [new(0.30, -0.15), new(-0.02, -0.12)],
            [new(0.70, -0.20), new(1.02, -0.28)],
            [new(0.70, -0.15), new(1.02, -0.12)]
        ];

        public ShapeMargins Margins { get; } = new(0.20, 0.82, 0.44, 0.20);

        public IReadOnlyList<ShapePoint> Eyes { get; } = [new(0.37, -0.36), new(0.63, -0.36)];

        public ShapePart Classify(double u, double v, int size)
        {
            if (InPolygon(u, v, Nose) || InPolygon(u, v, LeftEar) || InPolygon(u, v, RightEar))
                return ShapePart.Solid;

            if (InEllipse(u, v, 0.50, -0.18, 0.13, 0.08))                  // muzzle, so the nose stands out
                return ShapePart.Light;

            // Whiskers reach past the head, so they are tested before it and stay one module thin.
            foreach (var whisker in Whiskers)
            {
                if (NearPolyline(u, v, ModulesWide(1, size), whisker) && !InCircle(u, v, 0.5, -0.30, 0.25))
                    return ShapePart.Solid;
            }

            if (InEllipse(u, v, 0.32, 1.06, 0.13, 0.08) || InEllipse(u, v, 0.68, 1.06, 0.13, 0.08))   // paws
                return ShapePart.Solid;

            if (InCircle(u, v, 0.5, -0.30, 0.30)                           // head
                || InEllipse(u, v, 0.5, 0.52, 0.62, 0.60)                  // body
                || NearPolyline(u, v, 0.07, Tail))
                return ShapePart.Textured;

            return ShapePart.None;
        }
    }
}
