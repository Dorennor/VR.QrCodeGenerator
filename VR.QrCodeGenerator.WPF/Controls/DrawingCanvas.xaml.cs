using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VR.QrCodeGenerator.WPF.Controls
{
    /// <summary>
    /// A square sheet for drawing with one black pen of a fixed size, with a dashed square in the middle where the code
    /// goes. <see cref="Drawing"/> holds the drawing as a mask, one value per pixel, and updates after every change.
    /// </summary>
    public partial class DrawingCanvas : UserControl
    {
        public static readonly DependencyProperty DrawingProperty =
            DependencyProperty.Register(nameof(Drawing), typeof(bool[,]), typeof(DrawingCanvas),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty CodeFractionProperty =
            DependencyProperty.Register(nameof(CodeFraction), typeof(double), typeof(DrawingCanvas),
                                        new PropertyMetadata(0.4, (sender, _) => ((DrawingCanvas)sender).UpdateCodeGuide()));

        public static readonly DependencyProperty CanvasSizeProperty =
            DependencyProperty.Register(nameof(CanvasSize), typeof(double), typeof(DrawingCanvas),
                                        new PropertyMetadata(380.0, (sender, _) => ((DrawingCanvas)sender).UpdateCodeGuide()));

        /// <summary>
        /// Pen size in canvas pixels; about two modules of a typical code.
        /// </summary>
        public const double PenSize = 10;

        public DrawingCanvas()
        {
            InitializeComponent();

            Ink.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = Colors.Black,
                Width = PenSize,
                Height = PenSize,
                StylusTip = StylusTip.Ellipse,
                FitToCurve = true,
                IgnorePressure = true
            };

            Ink.Strokes.StrokesChanged += (_, _) => UpdateDrawing();

            UpdateCodeGuide();
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
        public double CodeFraction
        {
            get => (double)GetValue(CodeFractionProperty);
            set => SetValue(CodeFractionProperty, value);
        }

        /// <summary>
        /// The side of the square canvas, in pixels.
        /// </summary>
        public double CanvasSize
        {
            get => (double)GetValue(CanvasSizeProperty);
            set => SetValue(CanvasSizeProperty, value);
        }

        private void UpdateCodeGuide()
        {
            if (Surface is null)
                return;

            Surface.Width = Surface.Height = CanvasSize;
            CodeGuide.Width = CodeGuide.Height = CanvasSize * CodeFraction;
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

        /// <summary>
        /// Renders the strokes on white and turns every dark pixel into a true.
        /// </summary>
        private void UpdateDrawing()
        {
            var size = (int)Math.Round(CanvasSize);

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
