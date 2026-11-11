using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private BitmapSource _qrCodeImage;

        [ObservableProperty]
        private string _statusMessage;

        private readonly IDialogService _dialogService;
        private readonly IQrCodeGeneratorService _qrCodeGeneratorService;
        private readonly ILoggingService _loggingService;
        private readonly Settings _settings;

        // Read on every use so a folder changed on the Settings page applies right away.
        private string OutputFolder => OutputHelper.GetOutputFolder(_settings?.PathsSettings);

        // What the preview shows. Saving uses these, so settings changed since Generate do not affect the saved file.
        private string _generatedContent;

        private QrCodeOptions _generatedOptions;
        private byte[] _generatedPng;

        public MainViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService, Settings settings)
        {
            _dialogService = dialogService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
            _loggingService = loggingService;
            _settings = settings;

            Initialize();
        }

        private void Initialize()
        {
            SelectedPixelsPerModule = 20;
            QrCodeContent = string.Empty;
            SelectedEccTag = "Q";
            SelectedForegroundColor = new SolidColorBrush(Colors.Black).Color;
            SelectedBackgroundColor = new SolidColorBrush(Colors.White).Color;
            QrCodeHeight = 500;
            QrCodeWidth = 500;
        }

        /// <summary>
        /// Height and Width decide the image size unless an exact pixel size does.
        /// </summary>
        public bool UseImageSize => !UseExactPixelSize;

        private bool CanGenerate() => !string.IsNullOrWhiteSpace(QrCodeContent);

        private bool HasQrCode() => QrCodeImage != null;

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        public void GenerateQrCode()
        {
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

            try
            {
                var png = _qrCodeGeneratorService.Generate(content, options);

                QrCodeImage = ImageHelper.LoadBitmap(png);
                _generatedPng = png;
                _generatedContent = content;
                _generatedOptions = options;
            }
            catch (DataTooLongException ex)
            {
                _loggingService.Warn($"Content too long: {ex.Message}");
                _dialogService.ShowWarnMessageBox("This content is too long for a QR code. Shorten it or choose a lower error correction level.");
                return;
            }
            catch (QrCodeTooSmallException ex)
            {
                _dialogService.ShowWarnMessageBox(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during qr code generation process.", ex);
                _dialogService.ShowErrorMessageBox("Could not generate the QR code.");
                return;
            }

            SaveQrCode();
        }

        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public void CopyToClipboard()
        {
            try
            {
                OutputHelper.CopyQrToClipboard(_generatedPng);
                StatusMessage = "Copied to clipboard";
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during copying qr code to clipboard.", ex);
                _dialogService.ShowErrorMessageBox("Could not copy the QR code. Another app may be using the clipboard, please try again.");
            }
        }

        [RelayCommand(CanExecute = nameof(HasQrCode))]
        public void SaveQrCode()
        {
            try
            {
                var file = _qrCodeGeneratorService.Generate(_generatedContent, _generatedOptions, SelectedImageFormat);
                var path = OutputHelper.SaveToFile(file, OutputFolder, SelectedImageFormat);

                StatusMessage = $"Saved {QrCodeImage.PixelWidth} x {QrCodeImage.PixelHeight} px to {path}";
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during saving qr code process", ex);
                _dialogService.ShowErrorMessageBox($"Could not save the QR code to {OutputFolder}.");
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
        }

        [RelayCommand]
        public void OpenOutputFolder()
        {
            try
            {
                System.IO.Directory.CreateDirectory(OutputFolder);

                // The quotes matter: without them a path with spaces opens the wrong folder.
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{OutputFolder}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during opening output folder.", ex);
                _dialogService.ShowErrorMessageBox($"Could not open {OutputFolder}.");
            }
        }
    }
}