using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SkiaSharp;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Pictures;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// The generator with a picture around the code: the whole picture or its subject, dithered from its tones so its
    /// details show in black and white, or (for line art) its parts in the QR code color filled with QR-like texture.
    /// The code can be dragged anywhere on the picture. Everything else works as on the Generator page.
    /// </summary>
    public partial class PictureCodesViewModel : MainViewModel
    {
        public const double DefaultCodeSize = 0.3;

        public const double DefaultSensitivity = 0.5;

        public const int DefaultPixelsPerModule = 8;

        /// <summary>
        /// A subject is already solid, so only holes in it up to this share of the picture are filled (white fur inside
        /// an outline), not the background it surrounds.
        /// </summary>
        private const double LargestHoleInSubject = 0.02;

        private static readonly ShapePoint Middle = new(0.5, 0.5);

        private readonly IDialogService _dialogService;
        private readonly ILoggingService _loggingService;

        private QrPicture _picture;
        private bool[,] _template;
        private float[,] _darkness;

        /// <summary>
        /// What the code will be drawn from: the picture's tones (or ink) where it becomes the shape, white elsewhere.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPicture))]
        [NotifyCanExecuteChangedFor(nameof(RemovePictureCommand))]
        private BitmapSource _templatePreview;

        [ObservableProperty]
        private string _pictureName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UsesTones))]
        [NotifyPropertyChangedFor(nameof(UsesSensitivity))]
        [NotifyPropertyChangedFor(nameof(CanBlendCode))]
        private TemplateSourceOption _selectedSource = Sources[0];

        /// <summary>
        /// How much of the picture counts as the shape, 0.2 to 0.8. Subject and QR code color only.
        /// </summary>
        [ObservableProperty]
        private double _sensitivity = DefaultSensitivity;

        /// <summary>
        /// -0.5 to 0.5: lighter or darker tones before dithering. Whole picture and subject only.
        /// </summary>
        [ObservableProperty]
        private double _brightness;

        /// <summary>
        /// 0.5 to 2: less or more contrast before dithering. Whole picture and subject only.
        /// </summary>
        [ObservableProperty]
        private double _contrast = 1;

        /// <summary>
        /// The side of the code's square, as a share of the picture's shorter side: smaller leaves more of the
        /// picture around it, with more detail.
        /// </summary>
        [ObservableProperty]
        private double _codeSize = DefaultCodeSize;

        /// <summary>
        /// Where the code's centre is, as fractions of the picture's width and height. Dragged on the template preview.
        /// </summary>
        [ObservableProperty]
        private ShapePoint _codeCenter = Middle;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanBlendCode))]
        private DetailOption _selectedDetail = Details[^1];

        [ObservableProperty]
        private CodeBlendOption _selectedCodeBlend = CodeBlends[2];

        /// <summary>
        /// The smallest and largest code, as a share of the picture's shorter side, for the slider and resizing on the
        /// template.
        /// </summary>
        public const double MinCodeSize = 0.15;

        public const double MaxCodeSize = 0.7;

        public static IReadOnlyList<CodeBlendOption> CodeBlends { get; } =
        [
            new(null, "Off", "The code stays a solid black and white square."),
            new(0.6, "Subtle", "The picture shows lightly through the code. The safest to scan."),
            new(0.35, "Balanced", "The picture shows clearly through the code's squares; their centres stay exact."),
            new(0.15, "Strong", "The code almost disappears into the picture. Test it with your phone.")
        ];

        /// <summary>
        /// The code can only take on the picture's tones when it has them and each square has a border of dots around
        /// its centre (Extra fine or Ultra detail).
        /// </summary>
        public bool CanBlendCode => UsesTones && SelectedDetail.CellsPerModule >= 3;

        public PictureCodesViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
            : base(dialogService, qrCodeGeneratorService, loggingService, settings)
        {
            _dialogService = dialogService;
            _loggingService = loggingService;

            // A picture has hundreds of dots across: an exact size gives every dot whole pixels (2 at Ultra detail), so
            // the image is always big enough and the dots stay even. A fixed width and height would scale them unevenly.
            UseExactPixelSize = true;
            SelectedPixelsPerModule = DefaultPixelsPerModule;

            // The template depends on these; the code color too in QR code color mode.
            PropertyChanged += OnTemplateInputChanged;
        }

        public static IReadOnlyList<TemplateSourceOption> Sources { get; } =
        [
            new(PictureTemplateSource.WholePicture, "Whole picture", "The whole picture around the code, dithered into black and white dots. Best for photos and detailed art."),
            new(PictureTemplateSource.Subject, "Subject (automatic)", "Only what differs from the picture's background, dithered, with an outline. For a figure on a plain background."),
            new(PictureTemplateSource.CodeColor, "QR code color", "Only the parts in the QR code color, filled with QR-like texture like Kurkod. For line art and silhouettes.")
        ];

        public static IReadOnlyList<DetailOption> Details => DetailOption.All;

        public override string PageTitle => "Picture QR code";

        public override string PageDescription => "Choose a picture and it is drawn around the code in black and white dots. Drag the blue square to place the code.";

        public bool HasPicture => TemplatePreview is not null;

        /// <summary>
        /// The picture's tones are dithered (whole picture, subject), so brightness and contrast apply.
        /// </summary>
        public bool UsesTones => SelectedSource.Source != PictureTemplateSource.CodeColor;

        /// <summary>
        /// Part of the picture is picked (subject, QR code color), so the sensitivity applies.
        /// </summary>
        public bool UsesSensitivity => SelectedSource.Source != PictureTemplateSource.WholePicture;

        /// <summary>
        /// The template's size in pixels; the code placement overlay uses the same coordinates.
        /// </summary>
        public int TemplateWidth => _picture?.Width ?? 1;

        public int TemplateHeight => _picture?.Height ?? 1;

        protected override string GenerateBlockedReason => _picture is null ? "Choose a picture first." : null;

        protected override IQrStyle CreateStyle()
        {
            // Copies: the template can change after Generate, but Save must match the preview.
            var template = (bool[,])_template.Clone();
            var gapToClose = (int)Math.Round(6.0 * Math.Max(_picture.Width, _picture.Height) / 360);

            IShape shape = SelectedSource.Source switch
            {
                // Nothing to fill: the whole picture is the shape already.
                PictureTemplateSource.WholePicture => new PictureShape(
                    new MaskShape(template, CodeSize, gapToClose, ShapePart.Textured, codeClosesAreas: false, largestFilledArea: 0, codeCenter: CodeCenter),
                    (float[,])_darkness.Clone(),
                    drawOutline: false,
                    codeBlendBias: CanBlendCode ? SelectedCodeBlend.Bias : null),

                PictureTemplateSource.Subject => new PictureShape(
                    new MaskShape(template, CodeSize, gapToClose, ShapePart.Textured, codeClosesAreas: false, largestFilledArea: LargestHoleInSubject, codeCenter: CodeCenter),
                    (float[,])_darkness.Clone(),
                    drawOutline: true,
                    codeBlendBias: CanBlendCode ? SelectedCodeBlend.Bias : null),

                _ => new MaskShape(template, CodeSize, gapToClose, ShapePart.Textured, codeCenter: CodeCenter)
            };

            return new ShapeStyle(shape, SelectedDetail.CellsPerModule);
        }

        [RelayCommand]
        public void ChoosePicture()
        {
            var path = _dialogService.OpenFileDialog(QrPicture.FileFilter);

            if (!string.IsNullOrWhiteSpace(path))
                LoadPicture(path);
        }

        /// <summary>
        /// Loads the picture at <paramref name="path"/>; on failure keeps the current one and tells the user.
        /// </summary>
        public void LoadPicture(string path)
        {
            try
            {
                _picture = QrPicture.Load(File.ReadAllBytes(path));
                PictureName = Path.GetFileName(path);

                // A new picture starts with the code in the middle.
                OnPropertyChanged(nameof(TemplateWidth));
                OnPropertyChanged(nameof(TemplateHeight));
                CodeCenter = Middle;

                UpdateTemplate();
            }
            catch (Exception ex)
            {
                _loggingService.Error($"Error during loading picture {path}.", ex);
                _dialogService.ShowErrorMessageBox($"Could not open {Path.GetFileName(path)} as a picture. Choose a PNG, JPG, BMP, GIF, WEBP or ICO file.");
            }
        }

        [RelayCommand(CanExecute = nameof(HasPicture))]
        public void RemovePicture()
        {
            _picture = null;
            _template = null;
            _darkness = null;
            TemplatePreview = null;
            PictureName = null;
        }

        [RelayCommand]
        public void CenterCode() => CodeCenter = Middle;

        private void OnTemplateInputChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName is nameof(Sensitivity) or nameof(Brightness) or nameof(Contrast) or nameof(SelectedSource)
                or nameof(SelectedForegroundColor) or nameof(SelectedBackgroundColor))
                UpdateTemplate();
        }

        private void UpdateTemplate()
        {
            if (_picture is null)
                return;

            var ink = ToSkColor(SelectedForegroundColor);

            _template = _picture.ToTemplate(SelectedSource.Source, ink, ToSkColor(SelectedBackgroundColor), Sensitivity);
            _darkness = UsesTones ? _picture.ToDarkness(Brightness, Contrast) : null;
            TemplatePreview = ImageHelper.LoadBitmap(QrPicture.TemplatePreviewPng(_template, _darkness, ink));
        }

        private static SKColor ToSkColor(Color color) => new SKColor(color.R, color.G, color.B);

        public sealed record TemplateSourceOption(PictureTemplateSource Source, string Name, string Description);

        /// <param name="Bias">How far a blended module's border leans to its own color, or null for a solid code.</param>
        public sealed record CodeBlendOption(double? Bias, string Name, string Description);
    }
}
