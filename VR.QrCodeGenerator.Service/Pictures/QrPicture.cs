using System.IO;

using SkiaSharp;

namespace VR.QrCodeGenerator.Service.Pictures
{
    /// <summary>
    /// A picture used as the template of a QR code, like a drawing: its subject (or its pixels in the code's color)
    /// becomes the shape around the code. Reads PNG, JPG, BMP, GIF (first frame), WEBP and ICO.
    /// </summary>
    /// <remarks>Immutable after loading, so a template made from it stays valid while the user picks another picture.</remarks>
    public sealed class QrPicture
    {
        /// <summary>
        /// The file types <see cref="Load"/> reads, as an open-file dialog filter.
        /// </summary>
        public const string FileFilter = "Pictures|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.ico";

        /// <summary>
        /// Templates are made at most this many pixels on the long side: more than the finest shape cells of a large
        /// code need, and still quick to process.
        /// </summary>
        public const int TemplateSide = 720;

        /// <summary>
        /// In <see cref="PictureTemplateSource.Subject"/> mode, how far (in RGB distance) a pixel must be from the
        /// background at the lowest and highest sensitivity.
        /// </summary>
        private const double SubjectThresholdAtLowest = 90;

        private const double SubjectThresholdAtHighest = 10;

        /// <summary>
        /// Shapes smaller than this share of the template are dropped: stray sketch lines, signatures, dust.
        /// </summary>
        private const double SmallestPiece = 0.002;

        private readonly SKBitmap _bitmap;

        private QrPicture(SKBitmap bitmap)
        {
            _bitmap = bitmap;
        }

        public int Width => _bitmap.Width;

        public int Height => _bitmap.Height;

        /// <exception cref="InvalidDataException">The bytes are not a picture SkiaSharp can read.</exception>
        public static QrPicture Load(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            // SKBitmap.Decode throws for unknown data instead of returning null, so check the format first.
            using (SKData encoded = SKData.CreateCopy(data))
            using (SKCodec codec = SKCodec.Create(encoded))
            {
                if (codec is null)
                    throw new InvalidDataException("This file is not a picture that can be read.");
            }

            using (SKBitmap decoded = SKBitmap.Decode(data) ?? throw new InvalidDataException("This file is not a picture that can be read."))
            {
                // Scaled to the template size once, so changing the sensitivity or colors later is quick.
                var scale = Math.Min(1.0, (double)TemplateSide / Math.Max(decoded.Width, decoded.Height));
                var info = new SKImageInfo(Math.Max(1, (int)Math.Round(decoded.Width * scale)), Math.Max(1, (int)Math.Round(decoded.Height * scale)), SKColorType.Rgba8888, SKAlphaType.Unpremul);

                return new QrPicture(decoded.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell)) ?? throw new InvalidDataException("This picture could not be prepared."));
            }
        }

        /// <summary>
        /// The template [row, column]: true for the pixels that become the shape. Small separate pieces are dropped.
        /// </summary>
        /// <param name="source">Which pixels count: the subject against its background, or those in the code color.</param>
        /// <param name="ink">The QR code color, for <see cref="PictureTemplateSource.CodeColor"/>.</param>
        /// <param name="paper">The background color of the code, for <see cref="PictureTemplateSource.CodeColor"/>.</param>
        /// <param name="sensitivity">0 to 1: higher takes more of the picture.</param>
        public bool[,] ToTemplate(PictureTemplateSource source, SKColor ink, SKColor paper, double sensitivity = 0.5)
        {
            var template = source == PictureTemplateSource.Subject ? SubjectTemplate(sensitivity) : CodeColorTemplate(ink, paper, sensitivity);

            return RemoveSmallPieces(template, (int)Math.Ceiling(SmallestPiece * Width * Height));
        }

        /// <summary>
        /// Everything that differs from the background. The background is what most of the picture's edge is:
        /// transparent, or else the median color of the edge.
        /// </summary>
        private bool[,] SubjectTemplate(double sensitivity)
        {
            var edge = EdgePixels().ToList();
            var template = new bool[Height, Width];

            // A cut-out (transparent background): the subject is whatever is opaque.
            if (edge.Count(pixel => pixel.Alpha < 128) * 2 > edge.Count)
            {
                for (var row = 0; row < Height; row++)
                {
                    for (var column = 0; column < Width; column++)
                        template[row, column] = _bitmap.GetPixel(column, row).Alpha >= 128;
                }

                return template;
            }

            var opaqueEdge = edge.Where(pixel => pixel.Alpha >= 128).ToList();
            var background = new SKColor(Median(opaqueEdge, pixel => pixel.Red), Median(opaqueEdge, pixel => pixel.Green), Median(opaqueEdge, pixel => pixel.Blue));
            var threshold = SubjectThresholdAtLowest + (SubjectThresholdAtHighest - SubjectThresholdAtLowest) * Math.Clamp(sensitivity, 0, 1);

            for (var row = 0; row < Height; row++)
            {
                for (var column = 0; column < Width; column++)
                {
                    var pixel = _bitmap.GetPixel(column, row);

                    template[row, column] = pixel.Alpha >= 128 && Distance(pixel, background) > threshold;
                }
            }

            return template;
        }

        /// <summary>
        /// The pixels close to <paramref name="ink"/>: within <paramref name="sensitivity"/> of the way from the ink to
        /// <paramref name="paper"/>, so with red ink only reddish pixels count, not black ones. Transparent pixels never count.
        /// </summary>
        private bool[,] CodeColorTemplate(SKColor ink, SKColor paper, double sensitivity)
        {
            var template = new bool[Height, Width];
            var reach = sensitivity * Distance(ink, paper);

            for (var row = 0; row < Height; row++)
            {
                for (var column = 0; column < Width; column++)
                {
                    var pixel = _bitmap.GetPixel(column, row);

                    template[row, column] = pixel.Alpha >= 128 && Distance(pixel, ink) < reach;
                }
            }

            return template;
        }

        private IEnumerable<SKColor> EdgePixels()
        {
            for (var column = 0; column < Width; column++)
            {
                yield return _bitmap.GetPixel(column, 0);
                yield return _bitmap.GetPixel(column, Height - 1);
            }

            for (var row = 1; row < Height - 1; row++)
            {
                yield return _bitmap.GetPixel(0, row);
                yield return _bitmap.GetPixel(Width - 1, row);
            }
        }

        /// <summary>
        /// Drops connected pieces of the template smaller than <paramref name="minimumSize"/> pixels.
        /// </summary>
        private static bool[,] RemoveSmallPieces(bool[,] template, int minimumSize)
        {
            int height = template.GetLength(0), width = template.GetLength(1);
            var seen = new bool[height, width];
            var piece = new List<(int X, int Y)>();
            var queue = new Queue<(int X, int Y)>();

            for (var row = 0; row < height; row++)
            {
                for (var column = 0; column < width; column++)
                {
                    if (!template[row, column] || seen[row, column])
                        continue;

                    piece.Clear();
                    seen[row, column] = true;
                    queue.Enqueue((column, row));

                    while (queue.Count > 0)
                    {
                        var (x, y) = queue.Dequeue();
                        piece.Add((x, y));

                        foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                        {
                            if (nx < 0 || ny < 0 || nx >= width || ny >= height || seen[ny, nx] || !template[ny, nx])
                                continue;

                            seen[ny, nx] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }

                    if (piece.Count < minimumSize)
                    {
                        foreach (var (x, y) in piece)
                            template[y, x] = false;
                    }
                }
            }

            return template;
        }

        /// <summary>
        /// The template as a PNG for the page: the shape in <paramref name="ink"/> on white, with the code's square in
        /// the centre tinted blue, as on the drawing canvas.
        /// </summary>
        public static byte[] TemplatePreviewPng(bool[,] template, double codeFraction, SKColor ink)
        {
            int height = template.GetLength(0), width = template.GetLength(1);
            var side = codeFraction * Math.Min(width, height);
            var codeLeft = (width - side) / 2;
            var codeTop = (height - side) / 2;
            var codeTint = new SKColor(0xDF, 0xEC, 0xF8);

            using (SKBitmap bitmap = new SKBitmap(width, height))
            {
                for (var row = 0; row < height; row++)
                {
                    for (var column = 0; column < width; column++)
                    {
                        var inCode = column >= codeLeft && column < codeLeft + side && row >= codeTop && row < codeTop + side;

                        bitmap.SetPixel(column, row, inCode ? codeTint : template[row, column] ? ink : SKColors.White);
                    }
                }

                using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
                {
                    return data.ToArray();
                }
            }
        }

        private static byte Median(List<SKColor> pixels, Func<SKColor, byte> channel)
        {
            if (pixels.Count == 0)
                return 255;

            var values = pixels.Select(channel).OrderBy(value => value).ToList();

            return values[values.Count / 2];
        }

        private static double Distance(SKColor a, SKColor b)
        {
            double dr = a.Red - b.Red, dg = a.Green - b.Green, db = a.Blue - b.Blue;

            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }
    }
}
