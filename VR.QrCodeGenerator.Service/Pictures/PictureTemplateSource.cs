namespace VR.QrCodeGenerator.Service.Pictures
{
    /// <summary>
    /// Which pixels of a picture become the shape around the code.
    /// </summary>
    public enum PictureTemplateSource
    {
        /// <summary>
        /// Everything that differs from the background, which is read from the picture's edges (a color, or
        /// transparency). Works for light subjects on white as well as dark ones.
        /// </summary>
        Subject,

        /// <summary>
        /// The pixels close to the QR code color, for example only the black lines of line art.
        /// </summary>
        CodeColor
    }
}
