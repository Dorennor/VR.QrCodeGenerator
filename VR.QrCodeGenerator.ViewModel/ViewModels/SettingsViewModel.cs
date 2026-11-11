using System.Diagnostics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.ViewModel.Abstractions;

namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// Edits the saved settings (settings.json in %LOCALAPPDATA%). Changes apply to the running app and the file only on Save.
    /// </summary>
    public partial class SettingsViewModel : BasicViewModel
    {
        private readonly Settings _settings;
        private readonly ISettingsService _settingsService;
        private readonly ILoggingService _loggingService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private bool _useCustomPath;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EffectiveOutputFolder))]
        private string _outputPath;

        [ObservableProperty]
        private bool _logToFile;

        [ObservableProperty]
        private bool _debugLogging;

        [ObservableProperty]
        private bool _logToConsole;

        /// <summary>
        /// "Kurkod": QR codes are drawn as chickens.
        /// </summary>
        [ObservableProperty]
        private bool _chickenShape;

        [ObservableProperty]
        private string _statusMessage;

        public SettingsViewModel(Settings settings, ISettingsService settingsService, ILoggingService loggingService, IDialogService dialogService)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Load();
        }

        public string SettingsFilePath => _settingsService.SettingsFilePath;

        /// <summary>
        /// Where QR codes go with the values currently on the page (saved or not).
        /// </summary>
        public string EffectiveOutputFolder => OutputHelper.GetOutputFolder(new PathsSettings { UseCustomPath = UseCustomPath, OutputPath = OutputPath?.Trim() });

        partial void OnUseCustomPathChanged(bool value) => OnPropertyChanged(nameof(EffectiveOutputFolder));

        /// <summary>
        /// Shows the settings the app is running with, dropping unsaved edits.
        /// </summary>
        [RelayCommand]
        public void Load()
        {
            UseCustomPath = _settings.PathsSettings?.UseCustomPath ?? false;
            OutputPath = _settings.PathsSettings?.OutputPath ?? string.Empty;
            LogToFile = _settings.LoggingSettings?.File ?? true;
            DebugLogging = _settings.LoggingSettings?.Debug ?? false;
            LogToConsole = _settings.LoggingSettings?.Console ?? false;
            ChickenShape = _settings.StyleSettings?.ChickenShape ?? false;
            StatusMessage = null;
        }

        [RelayCommand]
        public void Save()
        {
            var outputPath = OutputPath?.Trim() ?? string.Empty;

            if (UseCustomPath && string.IsNullOrWhiteSpace(outputPath))
            {
                _dialogService.ShowWarnMessageBox("Choose an output folder, or turn off the custom output folder.");
                return;
            }

            var updated = new Settings
            {
                PathsSettings = new PathsSettings { UseCustomPath = UseCustomPath, OutputPath = outputPath },
                LoggingSettings = new LoggingSettings { File = LogToFile, Debug = DebugLogging, Console = LogToConsole },
                StyleSettings = new StyleSettings { ChickenShape = ChickenShape }
            };

            try
            {
                _settingsService.Save(updated);
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during saving settings.", ex);
                _dialogService.ShowErrorMessageBox($"Could not save the settings to {SettingsFilePath}. {ex.Message}");
                return;
            }

            // The rest of the app holds this same instance, so the new values apply without a restart.
            _settings.PathsSettings = updated.PathsSettings;
            _settings.LoggingSettings = updated.LoggingSettings;
            _settings.StyleSettings = updated.StyleSettings;
            _loggingService.ApplySettings(updated.LoggingSettings);

            OutputPath = outputPath;
            StatusMessage = $"Settings saved to {SettingsFilePath}";
            _loggingService.Info("Settings saved.");
        }

        [RelayCommand]
        public void BrowseOutputPath()
        {
            var selectedPath = _dialogService.OpenFolderDialog(OutputPath);

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                OutputPath = selectedPath;
                UseCustomPath = true;
            }
        }

        [RelayCommand]
        public void OpenLogsFolder()
        {
            var logsFolder = AppPaths.LogsDirectory;

            try
            {
                System.IO.Directory.CreateDirectory(logsFolder);

                // The quotes matter: without them a path with spaces opens the wrong folder.
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logsFolder}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _loggingService.Error("Error during opening logs folder.", ex);
                _dialogService.ShowErrorMessageBox($"Could not open {logsFolder}.");
            }
        }
    }
}
