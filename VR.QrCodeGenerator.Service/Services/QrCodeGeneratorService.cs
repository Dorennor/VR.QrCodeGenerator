using System.Globalization;
using System.Text;

using QRCoder;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Exceptions;
using VR.QrCodeGenerator.Service.Helpers;
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

            var modules = BuildModules(content, options);
            var rows = modules.GetLength(0);
            var columns = modules.GetLength(1);

            var width = options.PixelsPerModule * columns ?? options.Width;
            var height = options.PixelsPerModule * rows ?? options.Height;

            // Every module needs at least one pixel, or the result could never be scanned.
            if (width < columns || height < rows)
                throw new QrCodeTooSmallException(Math.Max(columns, rows));

            switch (format)
            {
                case EnumImageFormat.Png:
                    return RenderRaster(modules, options, width, height, SKEncodedImageFormat.Png, 100);

                case EnumImageFormat.Jpg:
                    return RenderRaster(modules, options, width, height, SKEncodedImageFormat.Jpeg, JpegQuality);

                case EnumImageFormat.Svg:
                    return RenderSvg(modules, options, width, height);

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), "Image format is not supported!");
            }
        }

        /// <summary>
        /// The final module matrix [row, column], true for dark: the QR code, drawn as a chicken when asked, with the
        /// quiet zone when asked. Not always square: a chicken is wider than tall.
        /// </summary>
        private static bool[,] BuildModules(string content, QrCodeOptions options)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, options.EccLevel))
            {
                var size = qrCodeData.ModuleMatrix.Count - 2 * QuietZoneModules;
                var code = new bool[size, size];

                for (var row = 0; row < size; row++)
                {
                    for (var column = 0; column < size; column++)
                        code[row, column] = qrCodeData.ModuleMatrix[row + QuietZoneModules][column + QuietZoneModules];
                }

                if (options.ChickenShape)
                    code = KurkodBuilder.Build(code, content);

                return options.DrawQuietZones ? AddMargin(code, QuietZoneModules) : code;
            }
        }

        private static bool[,] AddMargin(bool[,] modules, int margin)
        {
            var result = new bool[modules.GetLength(0) + 2 * margin, modules.GetLength(1) + 2 * margin];

            for (var row = 0; row < modules.GetLength(0); row++)
            {
                for (var column = 0; column < modules.GetLength(1); column++)
                    result[row + margin, column + margin] = modules[row, column];
            }

            return result;
        }

        /// <summary>
        /// Draws the modules as large as fits, centered on the background color. The aspect ratio is kept (a QR code must
        /// stay square to scan), so a different width and height add margin instead of stretching. Nearest neighbour
        /// sampling keeps the edges of the squares sharp; with an exact pixel size every module is exactly that size.
        /// </summary>
        private static byte[] RenderRaster(bool[,] modules, QrCodeOptions options, int width, int height, SKEncodedImageFormat encoding, int quality)
        {
            var rows = modules.GetLength(0);
            var columns = modules.GetLength(1);

            var foreground = new SKColor(options.ForegroundColor[0], options.ForegroundColor[1], options.ForegroundColor[2]);
            var background = new SKColor(options.BackgroundColor[0], options.BackgroundColor[1], options.BackgroundColor[2]);

            using (SKBitmap source = new SKBitmap(columns, rows))
            {
                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                        source.SetPixel(column, row, modules[row, column] ? foreground : background);
                }

                using (SKImage sourceImage = SKImage.FromBitmap(source))
                using (SKSurface surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    var (left, top, drawWidth, drawHeight) = Fit(columns, rows, width, height);

                    surface.Canvas.Clear(background);
                    surface.Canvas.DrawImage(sourceImage, new SKRect(left, top, left + drawWidth, top + drawHeight), new SKSamplingOptions(SKFilterMode.Nearest));

                    using (SKImage image = surface.Snapshot())
                    using (SKData data = image.Encode(encoding, quality))
                    {
                        return data.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// The largest rectangle with the matrix's aspect ratio that fits the image, centered, in whole pixels.
        /// </summary>
        private static (int Left, int Top, int Width, int Height) Fit(int columns, int rows, int width, int height)
        {
            var scale = Math.Min((double)width / columns, (double)height / rows);
            var drawWidth = Math.Min(width, (int)Math.Round(columns * scale));
            var drawHeight = Math.Min(height, (int)Math.Round(rows * scale));

            return ((width - drawWidth) / 2, (height - drawHeight) / 2, drawWidth, drawHeight);
        }

        /// <summary>
        /// Writes a true vector image: one path with a run per row of dark modules. The view box adds the same margin as
        /// <see cref="RenderRaster"/> when the aspect ratios differ.
        /// </summary>
        private static byte[] RenderSvg(bool[,] modules, QrCodeOptions options, int width, int height)
        {
            var rows = modules.GetLength(0);
            var columns = modules.GetLength(1);

            var moduleSize = Math.Min((double)width / columns, (double)height / rows);
            var viewWidth = width / moduleSize;
            var viewHeight = height / moduleSize;

            var path = new StringBuilder();

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (!modules[row, column])
                        continue;

                    var start = column;

                    while (column + 1 < columns && modules[row, column + 1])
                        column++;

                    path.Append(CultureInfo.InvariantCulture, $"M{start} {row}h{column - start + 1}v1h-{column - start + 1}z");
                }
            }

            var svg = string.Create(CultureInfo.InvariantCulture,
                $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {viewWidth:0.####} {viewHeight:0.####}" shape-rendering="crispEdges">
                <rect width="100%" height="100%" fill="{ToHex(options.BackgroundColor)}"/>
                <path transform="translate({(viewWidth - columns) / 2:0.####} {(viewHeight - rows) / 2:0.####})" fill="{ToHex(options.ForegroundColor)}" d="{path}"/>
                </svg>
                """);

            return new UTF8Encoding(false).GetBytes(svg);
        }

        private static string ToHex(byte[] color) => $"#{color[0]:X2}{color[1]:X2}{color[2]:X2}";
    }
}
