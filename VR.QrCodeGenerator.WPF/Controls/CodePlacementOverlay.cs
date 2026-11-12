using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using VR.QrCodeGenerator.Service.Styles.Shapes;

using Wpf.Ui.Controls;

namespace VR.QrCodeGenerator.WPF.Controls
{
    /// <summary>
    /// The blue square that shows where the QR code goes on a drawing or picture, which the user can drag to move,
    /// resize by its corner handle or the mouse wheel, or place by clicking. Its coordinates are the area's pixels
    /// (<see cref="AreaWidth"/> x <see cref="AreaHeight"/>), so put it over the drawing or picture at that size, for
    /// example in the same Viewbox.
    /// </summary>
    public sealed class CodePlacementOverlay : Canvas
    {
        public static readonly DependencyProperty AreaWidthProperty = Register(nameof(AreaWidth), 1);
        public static readonly DependencyProperty AreaHeightProperty = Register(nameof(AreaHeight), 1);
        public static readonly DependencyProperty CodeSizeProperty = Register(nameof(CodeSize), 0.4, twoWay: true);
        public static readonly DependencyProperty CodeCenterProperty = Register(nameof(CodeCenter), new ShapePoint(0.5, 0.5), twoWay: true);
        public static readonly DependencyProperty MinCodeSizeProperty = Register(nameof(MinCodeSize), 0.15);
        public static readonly DependencyProperty MaxCodeSizeProperty = Register(nameof(MaxCodeSize), 0.7);

        /// <summary>
        /// Off: the square only shows where the code goes and lets the mouse through (for drawing under it).
        /// </summary>
        public static readonly DependencyProperty IsInteractiveProperty = Register(nameof(IsInteractive), true);

        /// <summary>
        /// One wheel notch changes the code by this share of the area's shorter side.
        /// </summary>
        public const double WheelStep = 0.02;

        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));

        private readonly Border _square;
        private readonly Border _handle;

        // While dragging: resizing by the handle, or moving with the point where the square was grabbed.
        private bool _resizing;
        private Vector _grabOffset;

        public CodePlacementOverlay()
        {
            _square = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x59, 0x00, 0x78, 0xD4)),
                BorderBrush = Accent,
                IsHitTestVisible = false,
                Child = new Viewbox { Margin = new Thickness(8), Child = new SymbolIcon { Symbol = SymbolRegular.QrCode24, Foreground = Accent } }
            };

            _handle = new Border
            {
                Background = Brushes.White,
                BorderBrush = Accent,
                CornerRadius = new CornerRadius(3),
                IsHitTestVisible = false
            };

            Children.Add(_square);
            Children.Add(_handle);

            UpdateSquare();
        }

        public int AreaWidth { get => (int)GetValue(AreaWidthProperty); set => SetValue(AreaWidthProperty, value); }

        public int AreaHeight { get => (int)GetValue(AreaHeightProperty); set => SetValue(AreaHeightProperty, value); }

        /// <summary>
        /// The side of the code's square, as a share of the area's shorter side.
        /// </summary>
        public double CodeSize { get => (double)GetValue(CodeSizeProperty); set => SetValue(CodeSizeProperty, value); }

        /// <summary>
        /// The code's centre, as fractions of the area's width and height.
        /// </summary>
        public ShapePoint CodeCenter { get => (ShapePoint)GetValue(CodeCenterProperty); set => SetValue(CodeCenterProperty, value); }

        public double MinCodeSize { get => (double)GetValue(MinCodeSizeProperty); set => SetValue(MinCodeSizeProperty, value); }

        public double MaxCodeSize { get => (double)GetValue(MaxCodeSizeProperty); set => SetValue(MaxCodeSizeProperty, value); }

        public bool IsInteractive { get => (bool)GetValue(IsInteractiveProperty); set => SetValue(IsInteractiveProperty, value); }

        /// <summary>
        /// Makes the code bigger (positive) or smaller (negative) by <paramref name="step"/> of the area's shorter side.
        /// </summary>
        public void ResizeBy(double step) => CodeSize = Math.Clamp(CodeSize + step, MinCodeSize, MaxCodeSize);

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseLeftButtonDown(eventArgs);

            if (!IsInteractive)
                return;

            var position = eventArgs.GetPosition(this);
            var (left, top, side) = Square;

            // The corner handle resizes; it is checked first, since it overlaps the square's corner.
            _resizing = OnHandle(position);

            if (!_resizing)
            {
                var onCode = position.X >= left && position.X < left + side && position.Y >= top && position.Y < top + side;

                // Pressed on the code: drag it from where it was grabbed. Elsewhere: put its centre there and keep dragging.
                _grabOffset = onCode ? position - new Point(left + side / 2, top + side / 2) : new Vector();

                if (!onCode)
                    MoveTo(position);
            }

            CaptureMouse();
            eventArgs.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs eventArgs)
        {
            base.OnMouseMove(eventArgs);

            if (!IsInteractive)
                return;

            var position = eventArgs.GetPosition(this);

            if (!IsMouseCaptured || eventArgs.LeftButton != MouseButtonState.Pressed)
            {
                Cursor = OnHandle(position) ? Cursors.SizeNWSE : Cursors.SizeAll;
                return;
            }

            if (_resizing)
                CodeSize = CodePlacement.ResizeTo(AreaWidth, AreaHeight, CodeSize, CodeCenter, position.X, position.Y, MinCodeSize, MaxCodeSize);
            else
                MoveTo(position - _grabOffset);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs eventArgs)
        {
            base.OnMouseLeftButtonUp(eventArgs);

            _resizing = false;
            ReleaseMouseCapture();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs eventArgs)
        {
            base.OnMouseWheel(eventArgs);

            if (!IsInteractive)
                return;

            ResizeBy(Math.Sign(eventArgs.Delta) * WheelStep);
            eventArgs.Handled = true;
        }

        private (double Left, double Top, double Side) Square => MaskShape.CodeSquare(Math.Max(1, AreaWidth), Math.Max(1, AreaHeight), CodeSize, CodeCenter);

        private double HandleSize => CodePlacement.HandleSize(Math.Max(1, AreaWidth), Math.Max(1, AreaHeight));

        private bool OnHandle(Point position)
        {
            var (left, top, side) = Square;
            var handle = HandleSize;
            var handleLeft = left + side - handle / 2;
            var handleTop = top + side - handle / 2;

            return position.X >= handleLeft && position.X < handleLeft + handle && position.Y >= handleTop && position.Y < handleTop + handle;
        }

        private void MoveTo(Point centre) => CodeCenter = CodePlacement.MoveTo(Math.Max(1, AreaWidth), Math.Max(1, AreaHeight), CodeSize, centre.X, centre.Y);

        private void UpdateSquare()
        {
            Width = Math.Max(1, AreaWidth);
            Height = Math.Max(1, AreaHeight);

            // Transparent where interactive, so the whole area takes the mouse; nothing otherwise, so it passes through.
            Background = IsInteractive ? Brushes.Transparent : null;
            IsHitTestVisible = IsInteractive;

            var (left, top, side) = Square;
            var handle = HandleSize;
            var line = Math.Max(1.5, Math.Max(Width, Height) / 180);

            SetLeft(_square, left);
            SetTop(_square, top);
            _square.Width = _square.Height = side;
            _square.BorderThickness = new Thickness(line);

            SetLeft(_handle, left + side - handle / 2);
            SetTop(_handle, top + side - handle / 2);
            _handle.Width = _handle.Height = handle;
            _handle.BorderThickness = new Thickness(line * 1.5);
            _handle.Visibility = IsInteractive ? Visibility.Visible : Visibility.Collapsed;
        }

        private static DependencyProperty Register<T>(string name, T defaultValue, bool twoWay = false)
        {
            var options = FrameworkPropertyMetadataOptions.AffectsRender | (twoWay ? FrameworkPropertyMetadataOptions.BindsTwoWayByDefault : FrameworkPropertyMetadataOptions.None);

            return DependencyProperty.Register(name, typeof(T), typeof(CodePlacementOverlay),
                                               new FrameworkPropertyMetadata(defaultValue, options, (sender, _) => ((CodePlacementOverlay)sender).UpdateSquare()));
        }
    }
}
