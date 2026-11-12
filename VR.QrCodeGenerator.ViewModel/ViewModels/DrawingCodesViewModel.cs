using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// The generator with a drawing: pen lines become solid and enclosed areas get the QR-like texture (see
    /// <see cref="MaskShape"/>). The code can be moved and resized on the canvas. Everything else works as on the
    /// Generator page.
    /// </summary>
    public partial class DrawingCodesViewModel : MainViewModel
    {
        public const double DefaultCodeSize = 0.4;

        /// <summary>
        /// The smallest and largest code, as a share of the canvas.
        /// </summary>
        public const double MinCodeSize = 0.15;

        public const double MaxCodeSize = 0.7;

        private static readonly ShapePoint Middle = new(0.5, 0.5);

        /// <summary>
        /// The canvas [row, column], true where the pen drew. Set by the view whenever the drawing changes; null or
        /// empty gives the plain code.
        /// </summary>
        [ObservableProperty]
        private bool[,] _drawing;

        /// <summary>
        /// The side of the code's square, as a share of the canvas.
        /// </summary>
        [ObservableProperty]
        private double _codeSize = DefaultCodeSize;

        /// <summary>
        /// Where the code's centre is, as fractions of the canvas. Moved on the canvas in "Move code" mode.
        /// </summary>
        [ObservableProperty]
        private ShapePoint _codeCenter = Middle;

        /// <summary>
        /// How finely the drawing is drawn around the code. Fine by default: smoother lines than whole modules.
        /// </summary>
        [ObservableProperty]
        private DetailOption _selectedDetail = DetailOption.All[1];

        public DrawingCodesViewModel(IDialogService dialogService, IPlatformService platformService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
            : base(dialogService, platformService, qrCodeGeneratorService, loggingService, settings)
        {
        }

        public static IReadOnlyList<DetailOption> Details => DetailOption.All;

        public override string PageTitle => "Draw your QR code";

        public override string PageDescription => "Draw around the blue square, where the code goes. Closed shapes are filled with a QR-like pattern, lines stay solid. Hover over an option for help.";

        [RelayCommand]
        public void CenterCode() => CodeCenter = Middle;

        protected override IQrStyle CreateStyle()
        {
            if (Drawing is null || Drawing.Length == 0)
                return QrStyles.Get(Model.Enums.EnumQrStyle.Classic);

            // A copy: the drawing can change after Generate, but Save must match the preview.
            return new ShapeStyle(new MaskShape((bool[,])Drawing.Clone(), CodeSize, codeCenter: CodeCenter), SelectedDetail.CellsPerModule);
        }
    }
}
