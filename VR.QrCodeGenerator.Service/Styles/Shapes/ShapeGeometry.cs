namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// Hit tests for building shapes, in code widths (see <see cref="ShapePoint"/>).
    /// </summary>
    public static class ShapeGeometry
    {
        public static bool InCircle(double u, double v, double centerU, double centerV, double radius) => InEllipse(u, v, centerU, centerV, radius, radius);

        public static bool InEllipse(double u, double v, double centerU, double centerV, double radiusU, double radiusV)
        {
            var du = (u - centerU) / radiusU;
            var dv = (v - centerV) / radiusV;

            return du * du + dv * dv <= 1;
        }

        public static bool InPolygon(double u, double v, IReadOnlyList<ShapePoint> polygon)
        {
            var inside = false;

            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                if ((polygon[i].V > v) != (polygon[j].V > v)
                    && u < (polygon[j].U - polygon[i].U) * (v - polygon[i].V) / (polygon[j].V - polygon[i].V) + polygon[i].U)
                    inside = !inside;
            }

            return inside;
        }

        /// <summary>
        /// Within <paramref name="halfWidth"/> of the line through the points: tails, whiskers.
        /// </summary>
        public static bool NearPolyline(double u, double v, double halfWidth, IReadOnlyList<ShapePoint> points)
        {
            for (var i = 0; i + 1 < points.Count; i++)
            {
                if (DistanceToSegment(u, v, points[i], points[i + 1]) <= halfWidth)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Half the width of a line that is <paramref name="modules"/> modules thick, for <see cref="NearPolyline"/>.
        /// </summary>
        public static double ModulesWide(int modules, int size) => (modules * 0.5 + 0.05) / size;

        /// <summary>
        /// True in a band <paramref name="thickness"/> modules thick that starts at <paramref name="start"/> snapped to
        /// the module grid. For straight legs and toes that must be exactly one module (or more) thick.
        /// </summary>
        public static bool InBand(double value, double start, int thickness, int size)
        {
            var snapped = Math.Floor(start * size) / size;

            return value >= snapped && value < snapped + (double)thickness / size;
        }

        private static double DistanceToSegment(double u, double v, ShapePoint a, ShapePoint b)
        {
            var du = b.U - a.U;
            var dv = b.V - a.V;
            var lengthSquared = du * du + dv * dv;
            var t = lengthSquared == 0 ? 0 : Math.Clamp(((u - a.U) * du + (v - a.V) * dv) / lengthSquared, 0, 1);
            var pu = a.U + t * du - u;
            var pv = a.V + t * dv - v;

            return Math.Sqrt(pu * pu + pv * pv);
        }
    }
}
