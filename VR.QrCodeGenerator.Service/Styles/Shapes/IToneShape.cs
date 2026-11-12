namespace VR.QrCodeGenerator.Service.Styles.Shapes
{
    /// <summary>
    /// A shape that also has tones: how dark a picture is at each point. <see cref="ShapedCodeBuilder"/> then fills
    /// its <see cref="ShapePart.Textured"/> cells by dithering those tones instead of with random texture, so the
    /// picture's details (faces, edges, shading) show in black and white.
    /// </summary>
    public interface IToneShape : IShape
    {
        /// <summary>
        /// Whether textured parts get a dark outline where they meet the background, as random texture does. Off for a
        /// whole picture, whose edge is just the picture's frame.
        /// </summary>
        bool DrawOutline { get; }

        /// <summary>
        /// How dark the picture is over the cell centred at (u, v), from 0 (white) to 1 (black).
        /// </summary>
        /// <param name="size">The code's size in cells, so the cell's area can be averaged.</param>
        double Darkness(double u, double v, int size);
    }
}
