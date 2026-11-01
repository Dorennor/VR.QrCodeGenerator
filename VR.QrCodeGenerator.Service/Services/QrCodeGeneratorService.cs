using System.Globalization;
using System.Text;

using QRCoder;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Exceptions;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.Service.Services
{
    public class QrCodeGeneratorService : IQrCodeGeneratorService
    {
        // QRCoder's module matrix always includes a quiet zone of this many modules on every side.
        private const int QuietZoneModules = 4;

        private const int JpegQuality = 95;

        public byte[] Generate(string content, QrCodeOptions options, EnumImageFormat format = EnumImageFormat.Png)
        {
            options ??= new QrCodeOptions();

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, options.EccLevel))
                {
                    var modules = qrCodeData.ModuleMatrix.Count - (options.DrawQuietZones ? 0 : 2 * QuietZoneModules);

                    var width = options.PixelsPerModule * modules ?? options.Width;
                    var height = options.PixelsPerModule * modules ?? options.Height;

                    if (Math.Min(width, height) < modules)
                        throw new QrCodeTooSmallException(modules);

                    switch (format)
                    {
                        case EnumImageFormat.Png:
                            return RenderRaster(qrCodeData, options, width, height, SKEncodedImageFormat.Png, 100);

                        case EnumImageFormat.Jpg:
                            return RenderRaster(qrCodeData, options, width, height, SKEncodedImageFormat.Jpeg, JpegQuality);

                        case EnumImageFormat.Svg:
                            return RenderSvg(qrCodeData, options, width, height, modules);

                        default:
                            throw new ArgumentOutOfRangeException(nameof(format), "Image format is not supported!");
                    }
                }
            }
        }

        /// <summary>
        /// Draws the code as the largest centered square that fits, on the background color. A QR code must stay square
        /// to scan, so a width and height that differ add margin instead of stretching it. Nearest neighbour sampling
        /// keeps the edges of the squares sharp; with an exact pixel size the source already has the final size.
        /// </summary>
        private static byte[] RenderRaster(QRCodeData qrCodeData, QrCodeOptions options, int width, int height, SKEncodedImageFormat encoding, int quality)
        {
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] sourcePng = qrCode.GetGraphic(options.PixelsPerModule ?? 1, options.ForegroundColor, options.BackgroundColor, options.DrawQuietZones);

                using (SKBitmap source = SKBitmap.Decode(sourcePng))
                using (SKImage sourceImage = SKImage.FromBitmap(source))
                using (SKSurface surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var side = Math.Min(width, height);
                    var left = (width - side) / 2;
                    var top = (height - side) / 2;

                    surface.Canvas.Clear(new SKColor(options.BackgroundColor[0], options.BackgroundColor[1], options.BackgroundColor[2]));
                    surface.Canvas.DrawImage(sourceImage, new SKRect(left, top, left + side, top + side), new SKSamplingOptions(SKFilterMode.Nearest));

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(encoding, quality))
                    {
                        return data.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Writes a true vector image: one path with a run per row of dark modules. The view box adds the same margin as
        /// <see cref="RenderRaster"/> when the width and height differ.
        /// </summary>
        private static byte[] RenderSvg(QRCodeData qrCodeData, QrCodeOptions options, int width, int height, int modules)
        {
            var skipped = options.DrawQuietZones ? 0 : QuietZoneModules;
            var moduleSize = (double)Math.Min(width, height) / modules;
            var viewWidth = width / moduleSize;
            var viewHeight = height / moduleSize;

            var path = new StringBuilder();

            for (var row = 0; row < modules; row++)
            {
                var bits = qrCodeData.ModuleMatrix[row + skipped];

                for (var column = 0; column < modules; column++)
                {
                    if (!bits[column + skipped])
                        continue;

                    var start = column;

                    while (column + 1 < modules && bits[column + 1 + skipped])
                        column++;

                    path.Append(CultureInfo.InvariantCulture, $"M{start} {row}h{column - start + 1}v1h-{column - start + 1}z");
                }
            }

            var svg = string.Create(CultureInfo.InvariantCulture,
                $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {viewWidth:0.####} {viewHeight:0.####}" shape-rendering="crispEdges">
                <rect width="100%" height="100%" fill="{ToHex(options.BackgroundColor)}"/>
                <path transform="translate({(viewWidth - modules) / 2:0.####} {(viewHeight - modules) / 2:0.####})" fill="{ToHex(options.ForegroundColor)}" d="{path}"/>
                </svg>
                """);

            return new UTF8Encoding(false).GetBytes(svg);
        }

        private static string ToHex(byte[] color) => $"#{color[0]:X2}{color[1]:X2}{color[2]:X2}";
    }
}