using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;

namespace VR.QrCodeGenerator.Service.Rendering
{
    /// <summary>
    /// Last step of generation: the final module matrix as an image file. One renderer per <see cref="EnumImageFormat"/>.
    /// </summary>
    public interface IQrImageRenderer
    {
        EnumImageFormat Format { get; }

        /// <param name="modules">The final matrix [row, column], true for dark, including any quiet zone. May be non-square.</param>
        /// <param name="options">Colors.</param>
        /// <param name="width">Image width in pixels; at least one pixel per module column.</param>
        /// <param name="height">Image height in pixels; at least one pixel per module row.</param>
        byte[] Render(bool[,] modules, QrCodeOptions options, int width, int height);
    }
}
