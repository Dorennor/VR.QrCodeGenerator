using System.Drawing;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using QRCoder;
using QRCoder.Exceptions;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Exceptions;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Styles;
using VR.QrCodeGenerator.ViewModel.Abstractions;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    public partial class MainViewModel : BasicViewModel
    {
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateQrCodeCommand))]
        private string _qrCodeContent;

        [ObservableProperty]
        private EnumContentType _selectedContentType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UseImageSize))]
        private bool _useExactPixelSize;

        [ObservableProperty]
        private int _selectedPixelsPerModule;

        [ObservableProperty]
        private string _selectedEccTag;

        [ObservableProperty]
        private Color _selectedForegroundColor;

        [ObservableProperty]
        private Color _selectedBackgroundColor;

        [ObservableProperty]
        private EnumImageFormat _selectedImageFormat;

        [ObservableProperty]
        private int _qrCodeHeight;

        [ObservableProperty]
        private int _qrCodeWidth;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyToClipboardCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveQrCodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetQrCodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShareQrCodeCommand))]
        private PreviewImage _qrCodeImage;

        [ObservableProperty]
        private string _statusMessage;

        private readonly IDialogService _dialogService;
        private readonly IPlatformService _platformService;
        private readonly IQrCodeGeneratorService _qrCodeGeneratorService;
        private readonly ILoggingService _loggingService;
        private readonly Settings _settings;

        // Read on every use so a folder changed on the Settings page applies right away.
        private string OutputFolder => _platformService.GetOutputFolder(_settings?.PathsSettings);

        // What the preview shows. Saving uses these, so settings changed since Generate do not affect the saved file.
        private string _generatedContent;

        private QrCodeOptions _generatedOptions;
        private IQrStyle _generatedStyle;
        private byte[] _generatedPng;

        public MainViewModel(IDialogService dialogService, IPlatformService platformService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
        {
            _dialogService = dialogService;
            _platformService = platformService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
            _loggingService = loggingService;
            _settings = settings;

            Initialize();
        }

        public virtual string PageTitle => "QR Code Generator";

        public virtual string PageDescription => "Turn text or a link into a QR code, then copy or save it.";

        /// <summary>
        /// The styles offered on this page. Empty here: plain QR codes only. See <see cref="CustomCodesViewModel"/>.
        /// </summary>
        public virtual IReadOnlyList<QrStyleOption> AvailableStyles { get; } = [];

        public bool ShowsStyles => AvailableStyles.Count > 0;

        /// <summary>
        /// Whether this platform can send the code to another app (Android); the page shows Share only then.
        /// </summary>
        public bool CanShare => _platformService.CanShare;

        /// <summary>
        /// Used from the next Generate on; null means the classic QR code.
        /// </summary>
        [ObservableProperty]
        private QrStyleOption _selectedStyle;

        /// <summary>
        /// The style for the next Generate. Here the selected built-in style; a page can draw its own way, as the
        /// Drawing page does with the user's drawing.
        /// </summary>
        protected virtual IQrStyle CreateStyle() => QrStyles.Get(SelectedStyle?.Style ?? EnumQrStyle.Classic);

        private void Initialize()
        {
            SelectedPixelsPerModule = 20;
            QrCodeContent = string.Empty;
            SelectedEccTag = "Q";
            SelectedForegroundColor = Color.FromArgb(0, 0, 0);
            SelectedBackgroundColor = Color.FromArgb(255, 255, 255);
            QrCodeHeight = 500;
            QrCodeWidth = 500;
        }

        /// <summary>
        /// Height and Width decide the image size unless an exact pixel size does.
        /// </summary>
        public bool UseImageSize => !UseExactPixelSize;

        private bool CanGenerate() => !string.IsNullOrWhiteSpace(QrCodeContent);

        // While busy the preview is about to change, so copying, saving or resetting it would act on the old one.
        private bool HasQrCode() => QrCodeImage != null && !IsBusy;

        /// <summary>
        /// Why this page cannot generate yet (shown as a warning), or null when it can. For example a picture page with
        /// no picture.
        /// </summary>
        protected virtual string GenerateBlockedReason => null;

        /// <summary>
        /// True while a code is generated or saved in the background; the page shows a progress ring.
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(CopyToClipboardCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveQrCodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResetQrCodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(ShareQrCodeCommand))]
        private bool _isBusy;

        /// <summary>
        /// Checks the input and options on the UI thread, then draws and renders the code in the background: a picture
        /// or drawing can take a second or more, and the window stays responsive meanwhile. Generate is disabled until
        /// it finishes.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGenerate))]
        public async Task GenerateQrCodeAsync()
        {
            if (GenerateBlockedReason is { } reason)
            {
                _dialogService.ShowWarnMessageBox(reason);
                return;
            }

            var content = QrCodeContent;

            if (SelectedContentType is EnumContentType.Uri)
            {
                content = content.Trim();

                if (!UriHelper.IsValidUrl(content))
                {
                    _dialogService.ShowWarnMessageBox("Enter a full link that starts with http:// or https://, or switch the content type to Text.");
                    return;
                }
            }

            var options = new QrCodeOptions
            {
                PixelsPerModule = UseExactPixelSize ? SelectedPixelsPerModule : null,
                EccLevel = Enum.Parse<QRCodeGenerator.ECCLevel>(SelectedEccTag),
                ForegroundColor = [SelectedForegroundColor.R, SelectedForegroundColor.G, SelectedForegroundColor.B],
                BackgroundColor = [SelectedBackgroundColor.R, SelectedBackgroundColor.G, SelectedBackgroundColor.B],
                Height = QrCodeHeight,
                Width = QrCodeWidth
            };

            IsBusy = true;
            StatusMessage = "Generating…";

            try
            {
                // The style is made here, on the UI thread, from the page's current state; only the work moves off it.
                var style = CreateStyle();
                var png = await Task.Run(() => _qrCodeGeneratorService.Generate(content, options, style));

                QrCodeImage = PreviewImage.FromPng(png);
                _generatedPng = png;
                _generatedContent = content;
                _generatedOptions = options;
                _generatedStyle = style;
            }
            catch (DataTooLongException ex)
            {
                _loggingService.Warn($"Content too long: {ex.Message}");
                StatusMessage = null;
                _dialogService.ShowWarnMessageBox("This content is too long for a QR code. Shorten it or choose a lower error correction level.");
                return;
            }
            catch (QrCodeTooSmallException ex)
            {
                StatusMessage = null;
                _dialogService.ShowWarnMessageBox(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during qr code generation process.", ex);
                StatusMessage = null;
                _dialogService.ShowErrorMessageBox("Could not generate the QR code.");
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await SaveQrCodeAsync();
        }

        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public async Task CopyToClipboardAsync()
        {
            try
            {
                await _platformService.CopyImageAsync(_generatedPng);
                StatusMessage = "Copied to clipboard";
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during copying qr code to clipboard.", ex);
                _dialogService.ShowErrorMessageBox("Could not copy the QR code. Another app may be using the clipboard, please try again.");
            }
        }

        /// <summary>
        /// Sends the preview (PNG) to another app, such as a messenger. Only where <see cref="CanShare"/>.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public async Task ShareQrCodeAsync()
        {
            try
            {
                await _platformService.ShareImageAsync(_generatedPng);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during sharing qr code.", ex);
                _dialogService.ShowErrorMessageBox("Could not share the QR code.");
            }
        }

        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public async Task SaveQrCodeAsync()
        {
            var (content, options, style, format, paths, folder) = (_generatedContent, _generatedOptions, _generatedStyle, SelectedImageFormat, _settings?.PathsSettings, OutputFolder);

            IsBusy = true;

            try
            {
                // The generator reuses the drawing it just made, so this is mostly rendering and writing the file.
                var path = await Task.Run(() => _platformService.SaveToOutputFolder(_qrCodeGeneratorService.Generate(content, options, style, format), format, paths));

                StatusMessage = $"Saved {QrCodeImage.PixelWidth} x {QrCodeImage.PixelHeight} px to {path}";
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during saving qr code process", ex);
                StatusMessage = null;
                _dialogService.ShowErrorMessageBox($"Could not save the QR code to {folder}.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public void ResetQrCode()
        {
            QrCodeImage = null;
            StatusMessage = null;
            _generatedPng = null;
            _generatedContent = null;
            _generatedOptions = null;
            _generatedStyle = null;
        }

        [RelayCommand]
        public async Task OpenOutputFolderAsync()
        {
            try
            {
                await _platformService.OpenOutputFolderAsync(_settings?.PathsSettings);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during opening output folder.", ex);
                _dialogService.ShowErrorMessageBox($"Could not open {OutputFolder}.");
            }
        }
    }
}