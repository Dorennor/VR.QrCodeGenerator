using static VR.QrCodeGenerator.Service.Styles.Shapes.ShapeGeometry;

namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// Kurkod: a chicken in profile, facing left. The code is its body; head, comb, beak and wattle sit top left, the
    /// tail top right, the legs below.
    /// </summary>
    public sealed class ChickenShape : IShape
    {
        private static readonly ShapePoint[] Neck = [new(-0.06, -0.14), new(0.22, -0.14), new(0.32, 0.02), new(-0.10, 0.02)];

        private static readonly ShapePoint[] Beak = [new(-0.08, -0.31), new(-0.08, -0.19), new(-0.27, -0.25)];

        private static readonly ShapePoint[] Tail =
        [
            new(0.80, 0.22), new(0.94, -0.08), new(1.06, -0.36), new(1.18, -0.30), new(1.24, -0.16), new(1.30, -0.26),
            new(1.36, -0.04), new(1.40, 0.14), new(1.34, 0.38), new(1.14, 0.52)
        ];

        public ShapeMargins Margins { get; } = new(0.34, 0.52, 0.42, 0.42);

        public IReadOnlyList<ShapePoint> Eyes { get; } = [new(0.01, -0.28)];

        public ShapePart Classify(double u, double v, int size)
        {
            // Legs and toes: straight lines one module thick (more on big codes), on the module grid.
            var thickness = Math.Max(1, (int)Math.Round(0.035 * size));

            foreach (var leg in new[] { 0.36, 0.62 })
            {
                var legStart = Math.Floor(leg * size) / size;

                if (InBand(u, leg, thickness, size) && v >= 1.0 && v < 1.30 + (double)thickness / size)
                    return ShapePart.Solid;

                if (InBand(v, 1.30, thickness, size) && u >= legStart - 0.11 && u < legStart + 0.06)
                    return ShapePart.Solid;
            }

            if (InPolygon(u, v, Beak)
                || InCircle(u, v, -0.06, -0.12, 0.05)        // wattle
                || InCircle(u, v, -0.03, -0.42, 0.06)        // comb
                || InCircle(u, v, 0.07, -0.46, 0.07)
                || InCircle(u, v, 0.18, -0.42, 0.06))
                return ShapePart.Solid;

            if (InCircle(u, v, 0.06, -0.25, 0.17)            // head
                || InPolygon(u, v, Neck)
                || InPolygon(u, v, Tail)
                || InEllipse(u, v, 0.5, 0.5, 0.66, 0.62))    // body, rounder than the square code
                return ShapePart.Textured;

            return ShapePart.None;
        }
    }
}
