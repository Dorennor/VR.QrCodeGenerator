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
    /// The generator with a picture as the template, like a drawing: the picture's subject (or its pixels in the QR
    /// code color) becomes a shape filled with the QR-like texture, with the code in the centre. Everything else works
    /// as on the Generator page.
    /// </summary>
    public partial class PictureCodesViewModel : MainViewModel
    {
        public const double DefaultCodeSize = 0.4;

        public const double DefaultSensitivity = 0.5;

        /// <summary>
        /// A subject is already solid, so only holes in it up to this share of the picture are filled (white fur inside
        /// an outline), not the background it surrounds.
        /// </summary>
        private const double LargestHoleInSubject = 0.02;

        private readonly IDialogService _dialogService;
        private readonly ILoggingService _loggingService;

        private QrPicture _picture;
        private bool[,] _template;

        /// <summary>
        /// The template the code will use: the part of the picture that becomes the shape, with the code's square marked.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPicture))]
        [NotifyCanExecuteChangedFor(nameof(RemovePictureCommand))]
        private BitmapSource _templatePreview;

        [ObservableProperty]
        private string _pictureName;

        [ObservableProperty]
        private TemplateSourceOption _selectedSource = Sources[0];

        /// <summary>
        /// How much of the picture counts as the shape, 0.2 to 0.8.
        /// </summary>
        [ObservableProperty]
        private double _sensitivity = DefaultSensitivity;

        /// <summary>
        /// The side of the code's square in the centre of the picture, as a share of its shorter side: smaller leaves
        /// more of the picture for the shape, and so more detail.
        /// </summary>
        [ObservableProperty]
        private double _codeSize = DefaultCodeSize;

        [ObservableProperty]
        private DetailOption _selectedDetail = Details[1];

        public PictureCodesViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
            : base(dialogService, qrCodeGeneratorService, loggingService, settings)
        {
            _dialogService = dialogService;
            _loggingService = loggingService;

            // The template depends on these; the code color too in QR code color mode.
            PropertyChanged += OnTemplateInputChanged;
        }

        public static IReadOnlyList<TemplateSourceOption> Sources { get; } =
        [
            new(PictureTemplateSource.Subject, "Subject (automatic)", "Everything that differs from the picture's background. Works for light and dark subjects."),
            new(PictureTemplateSource.CodeColor, "QR code color", "Only the parts in the QR code color, for example the black lines of line art. Shapes they enclose are filled.")
        ];

        public static IReadOnlyList<DetailOption> Details { get; } =
        [
            new(1, "Normal", "The shape in squares as big as the code's, like Kurkod."),
            new(2, "Fine", "Squares half the size: smoother edges."),
            new(3, "Extra fine", "Squares a third of the size: the smoothest edges.")
        ];

        public override string PageTitle => "Picture QR code";

        public override string PageDescription => "Choose a picture and it becomes the shape around the code, like a drawing. The code sits in the centre.";

        public bool HasPicture => TemplatePreview is not null;

        protected override string GenerateBlockedReason => _picture is null ? "Choose a picture first." : null;

        protected override IQrStyle CreateStyle()
        {
            // A copy: the template can change after Generate, but Save must match the preview.
            var template = (bool[,])_template.Clone();
            var gapToClose = (int)Math.Round(6.0 * Math.Max(_picture.Width, _picture.Height) / 360);

            var shape = SelectedSource.Source == PictureTemplateSource.Subject
                ? new MaskShape(template, CodeSize, gapToClose, ShapePart.Textured, codeClosesAreas: false, largestFilledArea: LargestHoleInSubject)
                : new MaskShape(template, CodeSize, gapToClose, ShapePart.Textured);

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
            TemplatePreview = null;
            PictureName = null;
        }

        private void OnTemplateInputChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName is nameof(Sensitivity) or nameof(SelectedSource) or nameof(CodeSize) or nameof(SelectedForegroundColor) or nameof(SelectedBackgroundColor))
                UpdateTemplate();
        }

        private void UpdateTemplate()
        {
            if (_picture is null)
                return;

            var ink = ToSkColor(SelectedForegroundColor);

            _template = _picture.ToTemplate(SelectedSource.Source, ink, ToSkColor(SelectedBackgroundColor), Sensitivity);
            TemplatePreview = ImageHelper.LoadBitmap(QrPicture.TemplatePreviewPng(_template, CodeSize, ink));
        }

        private static SKColor ToSkColor(Color color) => new SKColor(color.R, color.G, color.B);

        public sealed record TemplateSourceOption(PictureTemplateSource Source, string Name, string Description);

        public sealed record DetailOption(int CellsPerModule, string Name, string Description);
    }
}
