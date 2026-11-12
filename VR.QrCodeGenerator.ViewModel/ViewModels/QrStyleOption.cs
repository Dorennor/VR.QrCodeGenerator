using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// One entry of the style combobox on the Custom codes page.
    /// </summary>
    public sealed record QrStyleOption(EnumQrStyle Style, string Name, string Description);
}
