using CommunityToolkit.Mvvm.ComponentModel;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// The generator with a drawing: the code sits in the centre of the canvas, pen lines become solid and enclosed
    /// areas get the QR-like texture (see <see cref="MaskShape"/>). Everything else works as on the Generator page.
    /// </summary>
    public partial class DrawingCodesViewModel : MainViewModel
    {
        /// <summary>
        /// The side of the code's square in the centre of the canvas, as a share of the canvas. The canvas shows this
        /// square so the user draws around it.
        /// </summary>
        public const double CodeFraction = 0.4;

        /// <summary>
        /// The canvas [row, column], true where the pen drew. Set by the view whenever the drawing changes; null or
        /// empty gives the plain code.
        /// </summary>
        [ObservableProperty]
        private bool[,] _drawing;

        public DrawingCodesViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
            : base(dialogService, qrCodeGeneratorService, loggingService, settings)
        {
        }

        public override string PageTitle => "Draw your QR code";

        public override string PageDescription => "Draw around the dashed square, where the code goes. Closed shapes are filled with a QR-like texture, lines stay solid.";

        protected override IQrStyle CreateStyle()
        {
            if (Drawing is null || Drawing.Length == 0)
                return QrStyles.Get(Model.Enums.EnumQrStyle.Classic);

            // A copy: the drawing can change after Generate, but Save must match the preview.
            return new ShapeStyle(new MaskShape((bool[,])Drawing.Clone(), CodeFraction));
        }
    }
}
