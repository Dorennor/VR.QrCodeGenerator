using System.Globalization;
using System.Text;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Generation;

namespace VR.QrCodeGenerator.Service.Rendering
{
    /// <summary>
    /// A true vector image: one path with a run per row of dark cells. The view box adds the same margin as the raster
    /// formats when the aspect ratios differ.
    /// </summary>
    public sealed class SvgImageRenderer : IQrImageRenderer
    {
        public EnumImageFormat Format => EnumImageFormat.Svg;

        public byte[] Render(QrDrawing drawing, QrCodeOptions options, int width, int height)
        {
            var cells = drawing.Cells;

            var rows = cells.Rows();
            var columns = cells.Columns();

            var cellSize = Math.Min((double)width / columns, (double)height / rows);
            var viewWidth = width / cellSize;
            var viewHeight = height / cellSize;

            var path = new StringBuilder();

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    if (!cells[row, column])
                        continue;

                    var start = column;

                    while (column + 1 < columns && cells[row, column + 1])
                        column++;

                    path.Append(CultureInfo.InvariantCulture, $"M{start} {row}h{column - start + 1}v1h-{column - start + 1}z");
                }
            }

            var svg = string.Create(CultureInfo.InvariantCulture,
                $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {viewWidth:0.####} {viewHeight:0.####}" shape-rendering="crispEdges">
                <rect width="100%" height="100%" fill="{ImageLayout.ToHex(options.BackgroundColor)}"/>
                <path transform="translate({(viewWidth - columns) / 2:0.####} {(viewHeight - rows) / 2:0.####})" fill="{ImageLayout.ToHex(options.ForegroundColor)}" d="{path}"/>
                </svg>
                """);

            return new UTF8Encoding(false).GetBytes(svg);
        }
    }
}
