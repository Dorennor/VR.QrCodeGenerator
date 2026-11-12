using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.MAUI.Controls
{
    /// <summary>
    /// An area of <see cref="AreaWidth"/> x <see cref="AreaHeight"/> pixels (a drawing sheet or a picture) shown to fit,
    /// with the blue square where the QR code goes: the touch version of CodePlacementOverlay on Windows.
    /// <list type="bullet">
    /// <item>One finger: drag the square to move it, drag its white corner to resize it, or touch elsewhere to put the
    /// code there. A derived view can use one finger for something else (drawing).</item>
    /// <item>Two fingers: pinch to resize the code, at any time (the mouse wheel on Windows).</item>
    /// </list>
    /// The math is shared with Windows: <see cref="CodePlacement"/> and <see cref="MaskShape.CodeSquare"/>.
    /// </summary>
    public abstract class CodeAreaView : ContentView
    {
        public static readonly BindableProperty CodeSizeProperty =
            BindableProperty.Create(nameof(CodeSize), typeof(double), typeof(CodeAreaView), 0.4, BindingMode.TwoWay, propertyChanged: Redraw);

        public static readonly BindableProperty CodeCenterProperty =
            BindableProperty.Create(nameof(CodeCenter), typeof(ShapePoint), typeof(CodeAreaView), new ShapePoint(0.5, 0.5), BindingMode.TwoWay, propertyChanged: Redraw);

        public static readonly BindableProperty MinCodeSizeProperty = BindableProperty.Create(nameof(MinCodeSize), typeof(double), typeof(CodeAreaView), 0.15);

        public static readonly BindableProperty MaxCodeSizeProperty = BindableProperty.Create(nameof(MaxCodeSize), typeof(double), typeof(CodeAreaView), 0.7);

        private static readonly SKColor Accent = new(0x00, 0x78, 0xD4);

        // Touch targets: at least this many device-independent units, whatever the area's zoom.
        private const double HandleTouchSize = 40;
        private const double HandleDrawSize = 22;

        private readonly Dictionary<long, SKPoint> _pointers = [];

        // The one-finger gesture, until every finger is lifted; a second finger turns it into a pinch.
        private long? _singlePointer;
        private bool _pinching;
        private double _pinchStartDistance;
        private double _pinchStartSize;

        // Moving or resizing the code with one finger.
        private bool _resizing;
        private double _grabX, _grabY;

        // From the last paint: area pixels to view pixels.
        private float _scale = 1, _offsetX, _offsetY;

        protected CodeAreaView()
        {
            Surface = new SKCanvasView { EnableTouchEvents = true, IgnorePixelScaling = false };
            Surface.PaintSurface += OnPaintSurface;
            Surface.Touch += OnTouch;

            Content = Surface;
        }

        public double CodeSize { get => (double)GetValue(CodeSizeProperty); set => SetValue(CodeSizeProperty, value); }

        public ShapePoint CodeCenter { get => (ShapePoint)GetValue(CodeCenterProperty); set => SetValue(CodeCenterProperty, value); }

        public double MinCodeSize { get => (double)GetValue(MinCodeSizeProperty); set => SetValue(MinCodeSizeProperty, value); }

        public double MaxCodeSize { get => (double)GetValue(MaxCodeSizeProperty); set => SetValue(MaxCodeSizeProperty, value); }

        protected SKCanvasView Surface { get; }

        /// <summary>
        /// The area's size in its own pixels: the coordinates of the code and of everything drawn on it.
        /// </summary>
        protected abstract int AreaWidth { get; }

        protected abstract int AreaHeight { get; }

        /// <summary>
        /// Whether the white resize corner is shown (and one finger moves the code).
        /// </summary>
        protected virtual bool IsPlacing => true;

        public void Redraw() => Surface.InvalidateSurface();

        /// <summary>
        /// Draws the area's content in area pixels, under the code's square.
        /// </summary>
        protected abstract void DrawArea(SKCanvas canvas);

        /// <summary>
        /// One finger touched the area at (x, y) in area pixels. By default it starts moving or resizing the code.
        /// </summary>
        protected virtual void OnSingleStart(double x, double y) => StartPlacing(x, y);

        protected virtual void OnSingleMove(double x, double y) => ContinuePlacing(x, y);

        protected virtual void OnSingleEnd(double x, double y)
        {
        }

        /// <summary>
        /// A second finger came down: the one-finger gesture is abandoned for a pinch.
        /// </summary>
        protected virtual void OnSingleCancel()
        {
        }

        /// <summary>
        /// A distance of <paramref name="units"/> on screen, in area pixels.
        /// </summary>
        protected double ScreenToArea(double units) => units * DeviceDisplay.Current.MainDisplayInfo.Density / Math.Max(_scale, 0.0001f);

        private static void Redraw(BindableObject view, object oldValue, object newValue) => ((CodeAreaView)view).Redraw();

        private int SafeWidth => Math.Max(1, AreaWidth);

        private int SafeHeight => Math.Max(1, AreaHeight);

        private (double Left, double Top, double Side) Square => MaskShape.CodeSquare(SafeWidth, SafeHeight, CodeSize, CodeCenter);

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs eventArgs)
        {
            var canvas = eventArgs.Surface.Canvas;
            var info = eventArgs.Info;

            canvas.Clear(SKColors.Transparent);

            _scale = Math.Min((float)info.Width / SafeWidth, (float)info.Height / SafeHeight);
            _offsetX = (info.Width - SafeWidth * _scale) / 2;
            _offsetY = (info.Height - SafeHeight * _scale) / 2;

            canvas.Save();
            canvas.Translate(_offsetX, _offsetY);
            canvas.Scale(_scale);

            DrawArea(canvas);
            DrawCode(canvas);

            canvas.Restore();
        }

        private void DrawCode(SKCanvas canvas)
        {
            var (left, top, side) = Square;
            var square = SKRect.Create((float)left, (float)top, (float)side, (float)side);
            var line = (float)Math.Max(1.5 / _scale * DeviceDisplay.Current.MainDisplayInfo.Density, SafeWidth / 180f);

            using var fill = new SKPaint { Color = Accent.WithAlpha(0x59), Style = SKPaintStyle.Fill };
            using var stroke = new SKPaint { Color = Accent, Style = SKPaintStyle.Stroke, StrokeWidth = line, IsAntialias = true };

            canvas.DrawRect(square, fill);
            canvas.DrawRect(square, stroke);

            DrawQrMark(canvas, square, stroke);

            if (!IsPlacing)
                return;

            var handle = (float)HandleDrawArea;
            using var handleFill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var handleStroke = new SKPaint { Color = Accent, Style = SKPaintStyle.Stroke, StrokeWidth = line * 1.5f, IsAntialias = true };
            var handleRect = SKRect.Create(square.Right - handle / 2, square.Bottom - handle / 2, handle, handle);

            canvas.DrawRoundRect(handleRect, handle / 6, handle / 6, handleFill);
            canvas.DrawRoundRect(handleRect, handle / 6, handle / 6, handleStroke);
        }

        /// <summary>
        /// Three finder patterns in the middle of the square, like the QR code icon on Windows.
        /// </summary>
        private static void DrawQrMark(SKCanvas canvas, SKRect square, SKPaint stroke)
        {
            var mark = square.Width * 0.4f;
            var left = square.MidX - mark / 2;
            var top = square.MidY - mark / 2;
            var finder = mark * 0.38f;

            using var paint = new SKPaint { Color = Accent, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(stroke.StrokeWidth, finder / 6), IsAntialias = true };
            using var dot = new SKPaint { Color = Accent, Style = SKPaintStyle.Fill, IsAntialias = true };

            foreach (var (x, y) in new[] { (left, top), (left + mark - finder, top), (left, top + mark - finder) })
            {
                canvas.DrawRect(SKRect.Create(x, y, finder, finder), paint);
                canvas.DrawRect(SKRect.Create(x + finder * 0.3f, y + finder * 0.3f, finder * 0.4f, finder * 0.4f), dot);
            }
        }

        private double HandleDrawArea => Math.Max(CodePlacement.HandleSize(SafeWidth, SafeHeight), ScreenToArea(HandleDrawSize));

        private void OnTouch(object sender, SKTouchEventArgs eventArgs)
        {
            // Keep receiving this touch's moves, and keep the page from scrolling while a finger is on the area.
            eventArgs.Handled = true;

            var x = (eventArgs.Location.X - _offsetX) / _scale;
            var y = (eventArgs.Location.Y - _offsetY) / _scale;

            switch (eventArgs.ActionType)
            {
                case SKTouchAction.Pressed:
                    StopParentScrolling();
                    _pointers[eventArgs.Id] = new SKPoint(x, y);

                    if (_pointers.Count == 1 && !_pinching)
                    {
                        _singlePointer = eventArgs.Id;
                        OnSingleStart(x, y);
                    }
                    else if (_pointers.Count == 2)
                    {
                        if (_singlePointer is not null)
                            OnSingleCancel();

                        _singlePointer = null;
                        _pinching = true;
                        _pinchStartDistance = PointerDistance();
                        _pinchStartSize = CodeSize;
                    }

                    break;

                case SKTouchAction.Moved:
                    if (!_pointers.ContainsKey(eventArgs.Id))
                        break;

                    _pointers[eventArgs.Id] = new SKPoint(x, y);

                    if (_pinching && _pointers.Count >= 2 && _pinchStartDistance > 0)
                        CodeSize = Math.Clamp(_pinchStartSize * PointerDistance() / _pinchStartDistance, MinCodeSize, MaxCodeSize);
                    else if (_singlePointer == eventArgs.Id)
                        OnSingleMove(x, y);

                    break;

                case SKTouchAction.Released:
                case SKTouchAction.Cancelled:
                    _pointers.Remove(eventArgs.Id);

                    if (_singlePointer == eventArgs.Id)
                    {
                        _singlePointer = null;

                        if (eventArgs.ActionType == SKTouchAction.Released)
                            OnSingleEnd(x, y);
                        else
                            OnSingleCancel();
                    }

                    // A pinch lasts until every finger is lifted, so lifting one does not start drawing.
                    if (_pointers.Count == 0)
                        _pinching = false;

                    _resizing = false;
                    break;
            }
        }

        private double PointerDistance()
        {
            var points = _pointers.Values.Take(2).ToArray();

            return points.Length < 2 ? 0 : SKPoint.Distance(points[0], points[1]);
        }

        /// <summary>
        /// Moving or resizing starts: on the corner resizes, on the code drags it from where it was grabbed, elsewhere
        /// puts the code's centre there.
        /// </summary>
        protected void StartPlacing(double x, double y)
        {
            var (left, top, side) = Square;
            var hit = Math.Max(HandleDrawArea, ScreenToArea(HandleTouchSize));

            _resizing = Math.Abs(x - (left + side)) <= hit / 2 && Math.Abs(y - (top + side)) <= hit / 2;

            if (_resizing)
                return;

            var onCode = x >= left && x < left + side && y >= top && y < top + side;

            (_grabX, _grabY) = onCode ? (x - (left + side / 2), y - (top + side / 2)) : (0, 0);

            if (!onCode)
                MoveTo(x, y);
        }

        protected void ContinuePlacing(double x, double y)
        {
            if (_resizing)
                CodeSize = CodePlacement.ResizeTo(SafeWidth, SafeHeight, CodeSize, CodeCenter, x, y, MinCodeSize, MaxCodeSize);
            else
                MoveTo(x - _grabX, y - _grabY);
        }

        private void MoveTo(double x, double y) => CodeCenter = CodePlacement.MoveTo(SafeWidth, SafeHeight, CodeSize, x, y);

        /// <summary>
        /// A finger on the area belongs to it until lifted: without this the page's scroll view takes the move.
        /// </summary>
        private void StopParentScrolling()
        {
            if (Surface.Handler?.PlatformView is Android.Views.View view)
                view.Parent?.RequestDisallowInterceptTouchEvent(true);
        }
    }
}