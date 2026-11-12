using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// The generator with custom styles, such as Kurkod. Everything else works as on the Generator page; this page has
    /// its own content, options and preview.
    /// </summary>
    public partial class CustomCodesViewModel : MainViewModel
    {
        /// <summary>
        /// The styles in the combobox, in this order. To add a style, see <see cref="EnumQrStyle"/>.
        /// </summary>
        public static IReadOnlyList<QrStyleOption> Styles { get; } =
        [
            new(EnumQrStyle.Kurkod, "Kurkod 🐔", "A chicken drawn around the QR code, like the Kurkod meme. The code itself is untouched and every result is checked to scan."),
            new(EnumQrStyle.Cat, "Cat 🐱", "A sitting cat with pointed ears, whiskers and a curled tail. The code is its body and still scans."),
            new(EnumQrStyle.Dog, "Dog 🐶", "A sitting dog with floppy ears, a muzzle and a wagging tail. The code is its body and still scans.")
        ];

        public CustomCodesViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
            : base(dialogService, qrCodeGeneratorService, loggingService, settings)
        {
            SelectedStyle = Styles[0];
        }

        public override string PageTitle => "Custom QR codes";

        public override string PageDescription => "QR codes in a fun style. Pick a style, then generate, copy or save as usual.";

        public override IReadOnlyList<QrStyleOption> AvailableStyles => Styles;
    }
}
