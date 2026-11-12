using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Generation;

namespace VR.QrCodeGenerator.Service.Rendering
{
    /// <summary>
    /// Last step of generation: the final drawing as an image file. One renderer per <see cref="EnumImageFormat"/>.
    /// </summary>
    public interface IQrImageRenderer
    {
        EnumImageFormat Format { get; }

        /// <param name="drawing">The final drawing, including any quiet zone. May be non-square.</param>
        /// <param name="options">Colors.</param>
        /// <param name="width">Image width in pixels; at least one pixel per cell column.</param>
        /// <param name="height">Image height in pixels; at least one pixel per cell row.</param>
        byte[] Render(QrDrawing drawing, QrCodeOptions options, int width, int height);
    }
}
