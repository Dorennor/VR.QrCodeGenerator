using SkiaSharp;

namespace VR.QrCodeGenerator.MAUI.Controls
{
    /// <summary>
    /// What one finger does on the drawing sheet.
    /// </summary>
    public enum DrawingTool
    {
        Draw,

        /// <summary>
        /// Touching a line removes the whole line, like EraseByStroke on Windows.
        /// </summary>
        Erase,

        MoveCode
    }

    /// <summary>
    /// The drawing sheet: a white square of <see cref="CanvasPixels"/> pixels with one black round pen, and the code's
    /// square on top. <see cref="Drawing"/> holds the drawing as a mask, one value per sheet pixel, after every change,
    /// exactly as the Windows canvas does, so <c>DrawingCodesViewModel</c> works the same.
    /// </summary>
    public sealed class DrawingSurface : CodeAreaView
    {
        /// <summary>
        /// The sheet's side in its own pixels; the same as on Windows, so a drawing gives the same code.
        /// </summary>
        public const int CanvasPixels = 360;

        public static readonly BindableProperty DrawingProperty =
            BindableProperty.Create(nameof(Drawing), typeof(bool[,]), typeof(DrawingSurface), null, BindingMode.OneWayToSource);

        public static readonly BindableProperty ToolProperty =
            BindableProperty.Create(nameof(Tool), typeof(DrawingTool), typeof(DrawingSurface), DrawingTool.Draw, propertyChanged: (view, _, _) => ((DrawingSurface)view).Redraw());

        /// <summary>
        /// The pen's width in sheet pixels for new lines (4 to 24, like Windows).
        /// </summary>
        public static readonly BindableProperty PenSizeProperty = BindableProperty.Create(nameof(PenSize), typeof(double), typeof(DrawingSurface), 10.0);

        // Lines are found under a finger within this distance on screen, beyond their own width.
        private const double EraseReach = 10;

        private readonly List<Stroke> _strokes = [];
        private Stroke _current;

        public DrawingSurface()
        {
            // Square: the sheet takes the full width and as much height.
            SizeChanged += (_, _) =>
            {
                if (Width > 0 && Math.Abs(HeightRequest - Width) > 0.5)
                    HeightRequest = Width;
            };
        }

        public bool[,] Drawing { get => (bool[,])GetValue(DrawingProperty); set => SetValue(DrawingProperty, value); }

        public DrawingTool Tool { get => (DrawingTool)GetValue(ToolProperty); set => SetValue(ToolProperty, value); }

        public double PenSize { get => (double)GetValue(PenSizeProperty); set => SetValue(PenSizeProperty, value); }

        public bool HasStrokes => _strokes.Count > 0;

        protected override int AreaWidth => CanvasPixels;

        protected override int AreaHeight => CanvasPixels;

        protected override bool IsPlacing => Tool == DrawingTool.MoveCode;

        /// <summary>
        /// Removes the last line.
        /// </summary>
        public void Undo()
        {
            if (_strokes.Count == 0)
                return;

            _strokes.RemoveAt(_strokes.Count - 1);
            OnStrokesChanged();
        }

        public void Clear()
        {
            _strokes.Clear();
            OnStrokesChanged();
        }

        protected override void DrawArea(SKCanvas canvas)
        {
            using (var paper = new SKPaint { Color = SKColors.White })
                canvas.DrawRect(SKRect.Create(CanvasPixels, CanvasPixels), paper);

            foreach (var stroke in _strokes)
                stroke.Draw(canvas);

            _current?.Draw(canvas);
        }

        protected override void OnSingleStart(double x, double y)
        {
            switch (Tool)
            {
                case DrawingTool.Draw:
                    _current = new Stroke((float)Math.Clamp(PenSize, 1, 64));
                    _current.Add(x, y);
                    Redraw();
                    break;

                case DrawingTool.Erase:
                    EraseAt(x, y);
                    break;

                default:
                    StartPlacing(x, y);
                    break;
            }
        }

        protected override void OnSingleMove(double x, double y)
        {
            switch (Tool)
            {
                case DrawingTool.Draw when _current is not null:
                    _current.Add(x, y);
                    Redraw();
                    break;

                case DrawingTool.Erase:
                    EraseAt(x, y);
                    break;

                case DrawingTool.MoveCode:
                    ContinuePlacing(x, y);
                    break;
            }
        }

        protected override void OnSingleEnd(double x, double y)
        {
            if (_current is null)
                return;

            _strokes.Add(_current);
            _current = null;
            OnStrokesChanged();
        }

        /// <summary>
        /// A second finger came down to pinch: the line just started is not kept.
        /// </summary>
        protected override void OnSingleCancel()
        {
            _current = null;
            Redraw();
        }

        private void EraseAt(double x, double y)
        {
            var reach = ScreenToArea(EraseReach);
            var removed = _strokes.RemoveAll(stroke => stroke.IsNear((float)x, (float)y, (float)reach));

            if (removed > 0)
                OnStrokesChanged();
        }

        private void OnStrokesChanged()
        {
            Redraw();
            Drawing = Rasterize();
        }

        /// <summary>
        /// Renders the lines on white and turns every dark pixel into a true, like the Windows canvas.
        /// </summary>
        private bool[,] Rasterize()
        {
            if (_strokes.Count == 0)
                return null;

            using var bitmap = new SKBitmap(new SKImageInfo(CanvasPixels, CanvasPixels, SKColorType.Rgba8888, SKAlphaType.Premul));

            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);

                foreach (var stroke in _strokes)
                    stroke.Draw(canvas);
            }

            var pixels = bitmap.GetPixelSpan();
            var mask = new bool[CanvasPixels, CanvasPixels];

            for (var y = 0; y < CanvasPixels; y++)
            {
                for (var x = 0; x < CanvasPixels; x++)
                {
                    // Red, green and blue of a black pen on white: dark when well below half.
                    var i = (y * CanvasPixels + x) * 4;
                    mask[y, x] = pixels[i] + pixels[i + 1] + pixels[i + 2] < 3 * 128;
                }
            }

            return mask;
        }

        /// <summary>
        /// One line: the finger's path, smoothed through the midpoints, in a round black pen.
        /// </summary>
        private sealed class Stroke(float width)
        {
            private readonly List<SKPoint> _points = [];
            private SKPath _path;

            public void Add(double x, double y)
            {
                var point = new SKPoint((float)x, (float)y);

                // Skip points closer than a pixel: they add nothing but work.
                if (_points.Count > 0 && SKPoint.Distance(_points[^1], point) < 1)
                    return;

                _points.Add(point);
                _path = null;
            }

            public void Draw(SKCanvas canvas)
            {
                if (_points.Count == 0)
                    return;

                using var paint = new SKPaint
                {
                    Color = SKColors.Black,
                    IsAntialias = true,
                    Style = _points.Count == 1 ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
                    StrokeWidth = width,
                    StrokeCap = SKStrokeCap.Round,
                    StrokeJoin = SKStrokeJoin.Round
                };

                // A tap is a dot.
                if (_points.Count == 1)
                {
                    canvas.DrawCircle(_points[0], width / 2, paint);
                    return;
                }

                canvas.DrawPath(_path ??= BuildPath(), paint);
            }

            /// <summary>
            /// Whether the line passes within <paramref name="reach"/> of (x, y), beyond its own half width.
            /// </summary>
            public bool IsNear(float x, float y, float reach)
            {
                var point = new SKPoint(x, y);
                var limit = width / 2 + reach;

                if (_points.Count == 1)
                    return SKPoint.Distance(_points[0], point) <= limit;

                for (var i = 1; i < _points.Count; i++)
                {
                    if (DistanceToSegment(point, _points[i - 1], _points[i]) <= limit)
                        return true;
                }

                return false;
            }

            private SKPath BuildPath()
            {
                var path = new SKPath();
                path.MoveTo(_points[0]);

                for (var i = 1; i < _points.Count - 1; i++)
                {
                    var middle = new SKPoint((_points[i].X + _points[i + 1].X) / 2, (_points[i].Y + _points[i + 1].Y) / 2);
                    path.QuadTo(_points[i], middle);
                }

                path.LineTo(_points[^1]);
                return path;
            }

            private static float DistanceToSegment(SKPoint point, SKPoint start, SKPoint end)
            {
                var segment = end - start;
                var lengthSquared = segment.X * segment.X + segment.Y * segment.Y;

                if (lengthSquared == 0)
                    return SKPoint.Distance(point, start);

                var t = Math.Clamp(((point.X - start.X) * segment.X + (point.Y - start.Y) * segment.Y) / lengthSquared, 0, 1);

                return SKPoint.Distance(point, new SKPoint(start.X + t * segment.X, start.Y + t * segment.Y));
            }
        }
    }
}