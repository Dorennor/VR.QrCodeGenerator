namespace VR.QrCodeGenerator.Service.Exceptions
{
    /// <summary>
    /// The requested image cannot give every QR square at least one pixel, so the result could never be scanned.
    /// </summary>
    public class QrCodeTooSmallException : Exception
    {
        public QrCodeTooSmallException(int minimumSize)
            : base($"The image is too small for this content. Use a height and width of at least {minimumSize} pixels.")
        {
            MinimumSize = minimumSize;
        }

        public int MinimumSize { get; }
    }
}