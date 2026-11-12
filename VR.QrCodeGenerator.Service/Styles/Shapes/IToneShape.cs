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
        /// Whether, and how much, the code itself takes on the picture: each module keeps its centre exact (where
        /// scanners read it) and its border follows the dithered picture, pulled this far towards the module's own color
        /// (0 is the picture as it is, 1 the solid code). Null keeps the code solid. Needs at least 3 cells per module;
        /// finder, timing and alignment patterns and format information always stay solid.
        /// </summary>
        double? CodeBlendBias { get; }

        /// <summary>
        /// How dark the picture is over the cell centred at (u, v), from 0 (white) to 1 (black).
        /// </summary>
        /// <param name="size">The code's size in cells, so the cell's area can be averaged.</param>
        double Darkness(double u, double v, int size);
    }
}
