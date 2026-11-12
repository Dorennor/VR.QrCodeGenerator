using SkiaSharp;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Controls
{
    /// <summary>
    /// The picture template (as the code will be drawn around it) with the code's square on top, to drag, resize and
    /// pinch. Like the Windows Viewbox, the template keeps its own pixels, so the code's position is in template pixels.
    /// </summary>
    public sealed class PicturePlacementView : CodeAreaView
    {
        public static readonly BindableProperty ImageProperty =
            BindableProperty.Create(nameof(Image), typeof(PreviewImage), typeof(PicturePlacementView), propertyChanged: (view, _, value) => ((PicturePlacementView)view).OnImageChanged((PreviewImage)value));

        public static readonly BindableProperty TemplateWidthProperty =
            BindableProperty.Create(nameof(TemplateWidth), typeof(int), typeof(PicturePlacementView), 1, propertyChanged: (view, _, _) => ((PicturePlacementView)view).Redraw());

        public static readonly BindableProperty TemplateHeightProperty =
            BindableProperty.Create(nameof(TemplateHeight), typeof(int), typeof(PicturePlacementView), 1, propertyChanged: (view, _, _) => ((PicturePlacementView)view).Redraw());

        private SKImage _image;

        public PreviewImage Image { get => (PreviewImage)GetValue(ImageProperty); set => SetValue(ImageProperty, value); }

        public int TemplateWidth { get => (int)GetValue(TemplateWidthProperty); set => SetValue(TemplateWidthProperty, value); }

        public int TemplateHeight { get => (int)GetValue(TemplateHeightProperty); set => SetValue(TemplateHeightProperty, value); }

        protected override int AreaWidth => TemplateWidth;

        protected override int AreaHeight => TemplateHeight;

        protected override void DrawArea(SKCanvas canvas)
        {
            if (_image is null)
                return;

            // Nearest neighbour: every template pixel stays a crisp dot, as on Windows.
            canvas.DrawImage(_image, SKRect.Create(TemplateWidth, TemplateHeight), new SKSamplingOptions(SKFilterMode.Nearest));
        }

        protected override void OnSingleStart(double x, double y)
        {
            // Nothing to place the code on yet.
            if (_image is not null)
                base.OnSingleStart(x, y);
        }

        private void OnImageChanged(PreviewImage image)
        {
            _image?.Dispose();
            _image = image is null ? null : SKImage.FromEncodedData(image.Png);

            // The code's square only means something on a picture.
            IsVisible = _image is not null;
            Redraw();
        }
    }
}