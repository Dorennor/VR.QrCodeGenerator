using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Generation;

namespace VR.QrCodeGenerator.Service.Rendering
{
    /// <summary>
    /// PNG and JPG through SkiaSharp. The drawing is painted one pixel per cell, then scaled with nearest neighbour
    /// sampling, which keeps the edges of the squares sharp; with an exact pixel size every module is exactly that size.
    /// </summary>
    public sealed class RasterImageRenderer : IQrImageRenderer
    {
        private readonly SKEncodedImageFormat _encoding;
        private readonly int _quality;

        public RasterImageRenderer(EnumImageFormat format, SKEncodedImageFormat encoding, int quality)
        {
            Format = format;
            _encoding = encoding;
            _quality = quality;
        }

        public EnumImageFormat Format { get; }

        public byte[] Render(QrDrawing drawing, QrCodeOptions options, int width, int height)
        {
            var cells = drawing.Cells;

            var foreground = new SKColor(options.ForegroundColor[0], options.ForegroundColor[1], options.ForegroundColor[2]);
            var background = new SKColor(options.BackgroundColor[0], options.BackgroundColor[1], options.BackgroundColor[2]);

            using (SKBitmap source = new SKBitmap(cells.Columns(), cells.Rows()))
            {
                for (var row = 0; row < cells.Rows(); row++)
                {
                    for (var column = 0; column < cells.Columns(); column++)
                        source.SetPixel(column, row, cells[row, column] ? foreground : background);
                }

                using (SKImage sourceImage = SKImage.FromBitmap(source))
                using (SKSurface surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var (left, top, drawWidth, drawHeight) = ImageLayout.Fit(cells.Columns(), cells.Rows(), width, height);

                    surface.Canvas.Clear(background);
                    surface.Canvas.DrawImage(sourceImage, new SKRect(left, top, left + drawWidth, top + drawHeight), new SKSamplingOptions(SKFilterMode.Nearest));

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(_encoding, _quality))
                    {
                        return data.ToArray();
                    }
                }
            }
        }
    }
}
