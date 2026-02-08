using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using QRCoder;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.ViewModel.Abstractions;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    public partial class MainViewModel : BasicViewModel
    {
        private string _qrCodePath;

        [ObservableProperty]
        private string _qrCodeContent;

        [ObservableProperty]
        private EnumContentType _selectedContentType;

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

        private readonly IDialogService _dialogService;
        private readonly IQrCodeGeneratorService _qrCodeGeneratorService;
        private readonly ILoggingService _loggingService;

        public byte[] QrCode;

        public event Action AfterQrCodeGenerated;

        public MainViewModel(IDialogService dialogService, IQrCodeGeneratorService qrCodeGeneratorService, ILoggingService loggingService)
        {
            _dialogService = dialogService;
            _qrCodeGeneratorService = qrCodeGeneratorService;
            _loggingService = loggingService;

            Initialize();
        }

        private void Initialize()
        {
            SelectedPixelsPerModule = 20;
            QrCodeContent = string.Empty;
            SelectedEccTag = "Q";
            SelectedForegroundColor = new SolidColorBrush(Colors.Black).Color;
            SelectedBackgroundColor = new SolidColorBrush(Colors.White).Color;
        }

        [RelayCommand]
        public void GenerateQrCode()
        {
            if (SelectedContentType is EnumContentType.Uri)
            {
                if (!UriHelper.IsValidUrl(QrCodeContent))
                {
                    _dialogService.ShowWarnMessageBox("Uri is broken");
                    return;
                }
            }

            try
            {
                var options = new QrCodeOptions
                {
                    PixelsPerModule = SelectedPixelsPerModule,
                    EccLevel = Enum.Parse<QRCodeGenerator.ECCLevel>(SelectedEccTag),
                    ForegroundColor = [SelectedForegroundColor.R, SelectedForegroundColor.G, SelectedForegroundColor.B],
                    BackgroundColor = [SelectedBackgroundColor.R, SelectedBackgroundColor.G, SelectedBackgroundColor.B]
                };

                QrCode = _qrCodeGeneratorService.GenerateAndSaveQr(QrCodeContent, options);
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorMessageBox("Error during qr code generation process.");
                return;
            }

            try
            {
                _qrCodePath = OutputHelper.SaveToFile(QrCode, imageFormat: SelectedImageFormat);
            }
            catch (Exception ex)
            {
                _dialogService.ShowErrorMessageBox("Error during saving qr code process");
            }

            AfterQrCodeGenerated?.Invoke();
        }

        [RelayCommand]
        public void CopyToClipboard()
        {
            OutputHelper.CopyQrToClipboard(_qrCodePath);
        }
    }
}