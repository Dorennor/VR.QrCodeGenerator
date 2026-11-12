using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.WPF.Controls
{
    /// <summary>
    /// A square sheet for drawing with one black pen, with the QR code's place shown as a blue square that can be moved
    /// and resized in "Move code" mode. <see cref="Drawing"/> holds the drawing as a mask, one value per pixel, and
    /// updates after every change.
    /// </summary>
    public partial class DrawingCanvas : UserControl
    {
        public static readonly DependencyProperty DrawingProperty =
            DependencyProperty.Register(nameof(Drawing), typeof(bool[,]), typeof(DrawingCanvas),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty CodeSizeProperty =
            DependencyProperty.Register(nameof(CodeSize), typeof(double), typeof(DrawingCanvas),
                                        new FrameworkPropertyMetadata(0.4, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty CodeCenterProperty =
            DependencyProperty.Register(nameof(CodeCenter), typeof(ShapePoint), typeof(DrawingCanvas),
                                        new FrameworkPropertyMetadata(new ShapePoint(0.5, 0.5), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty MinCodeSizeProperty =
            DependencyProperty.Register(nameof(MinCodeSize), typeof(double), typeof(DrawingCanvas), new PropertyMetadata(0.15));

        public static readonly DependencyProperty MaxCodeSizeProperty =
            DependencyProperty.Register(nameof(MaxCodeSize), typeof(double), typeof(DrawingCanvas), new PropertyMetadata(0.7));

        public static readonly DependencyProperty CanvasSizeProperty =
            DependencyProperty.Register(nameof(CanvasSize), typeof(double), typeof(DrawingCanvas),
                                        new PropertyMetadata(380.0, (sender, _) => ((DrawingCanvas)sender).OnCanvasSizeChanged()));

        private static readonly DependencyPropertyKey CanvasPixelsKey =
            DependencyProperty.RegisterReadOnly(nameof(CanvasPixels), typeof(int), typeof(DrawingCanvas), new PropertyMetadata(380));

        public static readonly DependencyProperty CanvasPixelsProperty = CanvasPixelsKey.DependencyProperty;

        public DrawingCanvas()
        {
            InitializeComponent();

            Ink.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Black,
                Width = PenSizeSlider.Value,
                Height = PenSizeSlider.Value,
                StylusTip = StylusTip.Ellipse,
                FitToCurve = true,
                IgnorePressure = true
            };

            Ink.Strokes.StrokesChanged += (_, _) => UpdateDrawing();

            OnCanvasSizeChanged();
        }

        /// <summary>
        /// The drawing [row, column], one value per canvas pixel, true where the pen drew.
        /// </summary>
        public bool[,] Drawing
        {
            get => (bool[,])GetValue(DrawingProperty);
            set => SetValue(DrawingProperty, value);
        }

        /// <summary>
        /// The side of the code's square, as a share of the canvas.
        /// </summary>
        public double CodeSize
        {
            get => (double)GetValue(CodeSizeProperty);
            set => SetValue(CodeSizeProperty, value);
        }

        /// <summary>
        /// The code's centre, as fractions of the canvas.
        /// </summary>
        public ShapePoint CodeCenter
        {
            get => (ShapePoint)GetValue(CodeCenterProperty);
            set => SetValue(CodeCenterProperty, value);
        }

        public double MinCodeSize
        {
            get => (double)GetValue(MinCodeSizeProperty);
            set => SetValue(MinCodeSizeProperty, value);
        }

        public double MaxCodeSize
        {
            get => (double)GetValue(MaxCodeSizeProperty);
            set => SetValue(MaxCodeSizeProperty, value);
        }

        /// <summary>
        /// The side of the square canvas, in pixels.
        /// </summary>
        public double CanvasSize
        {
            get => (double)GetValue(CanvasSizeProperty);
            set => SetValue(CanvasSizeProperty, value);
        }

        /// <summary>
        /// <see cref="CanvasSize"/> in whole pixels: the size of <see cref="Drawing"/> and of the code overlay.
        /// </summary>
        public int CanvasPixels => (int)GetValue(CanvasPixelsProperty);

        private void OnCanvasSizeChanged()
        {
            if (Surface is null)
                return;

            Surface.Width = Surface.Height = CanvasSize;
            SetValue(CanvasPixelsKey, (int)Math.Round(CanvasSize));
        }

        /// <summary>
        /// Draw, Erase or Move code: the ink takes the mouse for the first two, the code overlay for the last.
        /// </summary>
        private void Mode_OnChecked(object sender, RoutedEventArgs eventArgs)
        {
            if (Ink is null || CodeOverlay is null)
                return;

            Ink.EditingMode = sender == EraseMode ? InkCanvasEditingMode.EraseByStroke
                            : sender == MoveMode ? InkCanvasEditingMode.None
                            : InkCanvasEditingMode.Ink;

            CodeOverlay.IsInteractive = sender == MoveMode;
        }

        private void PenSize_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> eventArgs)
        {
            if (Ink?.DefaultDrawingAttributes is null)
                return;

            Ink.DefaultDrawingAttributes.Width = eventArgs.NewValue;
            Ink.DefaultDrawingAttributes.Height = eventArgs.NewValue;
        }

        /// <summary>
        /// The mouse wheel over the sheet resizes the code in every mode, so the code can be sized while drawing.
        /// </summary>
        private void Surface_OnPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
        {
            CodeOverlay.ResizeBy(Math.Sign(eventArgs.Delta) * CodePlacementOverlay.WheelStep);
            eventArgs.Handled = true;
        }

        private void Undo_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            if (Ink.Strokes.Count > 0)
                Ink.Strokes.RemoveAt(Ink.Strokes.Count - 1);
        }

        private void Clear_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            Ink.Strokes.Clear();
        }

        private void Center_OnClick(object sender, RoutedEventArgs eventArgs)
        {
            CodeCenter = new ShapePoint(0.5, 0.5);
        }

        /// <summary>
        /// Renders the strokes on white and turns every dark pixel into a true.
        /// </summary>
        private void UpdateDrawing()
        {
            var size = CanvasPixels;

            if (Ink.Strokes.Count == 0)
            {
                Drawing = null;
                return;
            }

            var visual = new DrawingVisual();

            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, size, size));
                Ink.Strokes.Draw(context);
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var pixels = new byte[size * size * 4];
            bitmap.CopyPixels(pixels, size * 4, 0);

            var mask = new bool[size, size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // Blue, green and red of a black pen on white: dark when well below half.
                    var i = (y * size + x) * 4;
                    mask[y, x] = pixels[i] + pixels[i + 1] + pixels[i + 2] < 3 * 128;
                }
            }

            Drawing = mask;
        }
    }
}
