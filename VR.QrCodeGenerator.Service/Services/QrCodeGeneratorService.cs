using QRCoder;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Exceptions;
using VR.QrCodeGenerator.Service.Generation;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Rendering;
using VR.QrCodeGenerator.Service.Styles;

namespace VR.QrCodeGenerator.Service.Services
{
    /// <summary>
    /// The generation pipeline:
    /// 1. <see cref="QrMatrixEncoder"/>: the content as a QR code's modules.
    /// 2. <see cref="IQrStyle"/>: the final drawing, for example a chicken around the code.
    /// 3. The quiet zone, when asked for.
    /// 4. <see cref="IQrImageRenderer"/>: the image file in the chosen format and size.
    /// </summary>
    public class QrCodeGeneratorService : IQrCodeGeneratorService
    {
        private const int JpegQuality = 95;

        private static readonly IReadOnlyDictionary<EnumImageFormat, IQrImageRenderer> Renderers = new IQrImageRenderer[]
        {
            new RasterImageRenderer(EnumImageFormat.Png, SKEncodedImageFormat.Png, 100),
            new RasterImageRenderer(EnumImageFormat.Jpg, SKEncodedImageFormat.Jpeg, JpegQuality),
            new SvgImageRenderer()
        }.ToDictionary(renderer => renderer.Format);

        private readonly Lock _lastDrawingLock = new();

        // The last drawing and what it was made from. The preview and the saved file are generated separately with the
        // same content, error correction and style; a decorated drawing can take a second or more, so it is made once.
        private (string Content, QRCodeGenerator.ECCLevel EccLevel, IQrStyle Style, QrDrawing Drawing)? _lastDrawing;

        /// <summary>
        /// Steps 1 and 2: the drawing for this content, error correction and style, from the last call when it matches.
        /// Colors, size and format do not change the drawing.
        /// </summary>
        private QrDrawing Draw(string content, QRCodeGenerator.ECCLevel eccLevel, IQrStyle style)
        {
            lock (_lastDrawingLock)
            {
                if (_lastDrawing is { } last && last.Content == content && last.EccLevel == eccLevel && ReferenceEquals(last.Style, style))
                    return last.Drawing;
            }

            var drawing = style.Apply(QrMatrixEncoder.Encode(content, eccLevel), content);

            lock (_lastDrawingLock)
            {
                _lastDrawing = (content, eccLevel, style, drawing);
            }

            return drawing;
        }

        public byte[] Generate(string content, QrCodeOptions options, EnumImageFormat format = EnumImageFormat.Png)
        {
            return Generate(content, options, QrStyles.Get(options?.Style ?? EnumQrStyle.Classic), format);
        }

        public byte[] Generate(string content, QrCodeOptions options, IQrStyle style, EnumImageFormat format = EnumImageFormat.Png)
        {
            ArgumentNullException.ThrowIfNull(style);
            options ??= new QrCodeOptions();

            if (!Renderers.TryGetValue(format, out var renderer))
                throw new ArgumentOutOfRangeException(nameof(format), "Image format is not supported!");

            var drawing = Draw(content, options.EccLevel, style);

            if (options.DrawQuietZones)
                drawing = drawing.WithMargin(QrMatrixEncoder.QuietZoneModules);

            int width = options.Width, height = options.Height;

            if (options.PixelsPerModule is int pixelsPerModule)
            {
                // Whole pixels per cell keep every square sharp; with finer cells a module is the nearest multiple.
                var pixelsPerCell = Math.Max(1, (int)Math.Round((double)pixelsPerModule / drawing.CellsPerModule));

                width = pixelsPerCell * drawing.Columns;
                height = pixelsPerCell * drawing.Rows;
            }

            // Every cell needs at least one pixel, or the result could never be scanned.
            if (width < drawing.Columns || height < drawing.Rows)
                throw new QrCodeTooSmallException(Math.Max(drawing.Columns, drawing.Rows));

            return renderer.Render(drawing, options, width, height);
        }
    }
}
