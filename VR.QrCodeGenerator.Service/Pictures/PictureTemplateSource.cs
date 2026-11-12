namespace VR.QrCodeGenerator.Service.Pictures
{
    /// <summary>
    /// Which part of a picture becomes the shape around the code, and how it is filled.
    /// </summary>
    public enum PictureTemplateSource
    {
        /// <summary>
        /// The whole picture (its opaque part), dithered from its tones. Best for photos and detailed art.
        /// </summary>
        WholePicture,

        /// <summary>
        /// Everything that differs from the background, which is read from the picture's edges (a color, or
        /// transparency), dithered from its tones with a dark outline.
        /// </summary>
        Subject,

        /// <summary>
        /// The pixels close to the QR code color, for example the black lines of line art, filled with QR-like
        /// texture like Kurkod. Shapes they enclose are filled too.
        /// </summary>
        CodeColor
    }
}
