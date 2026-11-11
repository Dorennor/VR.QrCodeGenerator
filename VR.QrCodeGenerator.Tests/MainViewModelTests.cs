using System.IO;
using System.Windows;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Interfaces;
using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainViewModelTests
    {
        private string _folder;
        private FakeDialogService _dialogs;
        private MainViewModel _viewModel;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());
            _dialogs = new FakeDialogService();

            var settings = new Settings { PathsSettings = new PathsSettings { UseCustomPath = true, OutputPath = _folder } };

            _viewModel = new MainViewModel(_dialogs, new QrCodeGeneratorService(), new FakeLoggingService(), settings);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        private string[] SavedFiles() => Directory.Exists(_folder) ? Directory.GetFiles(_folder) : [];

        [Test]
        public void BeforeGenerating_OnlyOpenFolderIsAvailable()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.GenerateQrCodeCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.SaveQrCodeCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.CopyToClipboardCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.ResetQrCodeCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.OpenOutputFolderCommand.CanExecute(null), Is.True);
            });
        }

        [Test]
        public void RejectedUrl_WarnsAndKeepsSaveAndCopyDisabled()
        {
            _viewModel.QrCodeContent = "not a link";

            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_dialogs.Warnings, Has.Count.EqualTo(1));
                Assert.That(_viewModel.QrCodeImage, Is.Null);
                Assert.That(_viewModel.SaveQrCodeCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.CopyToClipboardCommand.CanExecute(null), Is.False);
                Assert.That(SavedFiles(), Is.Empty);
            });
        }

        [Test]
        public void UrlWithSurroundingSpaces_IsTrimmed()
        {
            _viewModel.QrCodeContent = "  https://example.com  ";

            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.That(QrCodeGeneratorServiceTests.Decode(File.ReadAllBytes(SavedFiles().Single())), Is.EqualTo("https://example.com"));
        }

        [TestCase(EnumImageFormat.Png, ".png")]
        [TestCase(EnumImageFormat.Jpg, ".jpg")]
        [TestCase(EnumImageFormat.Svg, ".svg")]
        public void Generate_SavesSelectedFormatAndEnablesActions(EnumImageFormat format, string extension)
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.SelectedImageFormat = format;

            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(Path.GetExtension(SavedFiles().Single()), Is.EqualTo(extension));
                Assert.That(_viewModel.QrCodeImage, Is.Not.Null);
                Assert.That(_viewModel.StatusMessage, Does.Contain(_folder));
                Assert.That(_viewModel.SaveQrCodeCommand.CanExecute(null), Is.True);
                Assert.That(_viewModel.CopyToClipboardCommand.CanExecute(null), Is.True);
            });
        }

        [Test]
        public void Copy_AfterSavingSvg_PutsImageOnClipboard()
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.SelectedImageFormat = EnumImageFormat.Svg;
            _viewModel.GenerateQrCodeCommand.Execute(null);

            _viewModel.CopyToClipboardCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_dialogs.Errors, Is.Empty);
                Assert.That(Clipboard.ContainsImage(), Is.True);
            });
        }

        [Test]
        public void Save_UsesWhatThePreviewShows_NotLaterChanges()
        {
            _viewModel.QrCodeContent = "https://example.com/first";
            _viewModel.GenerateQrCodeCommand.Execute(null);
            File.Delete(SavedFiles().Single());

            _viewModel.QrCodeContent = "https://example.com/second";
            _viewModel.SaveQrCodeCommand.Execute(null);

            Assert.That(QrCodeGeneratorServiceTests.Decode(File.ReadAllBytes(SavedFiles().Single())), Is.EqualTo("https://example.com/first"));
        }

        [Test]
        public void Reset_ClearsEverythingSoSaveCannotRun()
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.GenerateQrCodeCommand.Execute(null);

            _viewModel.ResetQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.QrCodeImage, Is.Null);
                Assert.That(_viewModel.StatusMessage, Is.Null);
                Assert.That(_viewModel.SaveQrCodeCommand.CanExecute(null), Is.False);
                Assert.That(_viewModel.CopyToClipboardCommand.CanExecute(null), Is.False);
            });
        }

        [Test]
        public void TooLongContent_WarnsWithoutErrorAndKeepsPreviousCode()
        {
            _viewModel.SelectedContentType = EnumContentType.Text;
            _viewModel.QrCodeContent = "short";
            _viewModel.GenerateQrCodeCommand.Execute(null);
            var previous = _viewModel.QrCodeImage;

            _viewModel.QrCodeContent = new string('x', 5000);
            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_dialogs.Warnings, Has.Count.EqualTo(1));
                Assert.That(_dialogs.Errors, Is.Empty);
                Assert.That(_viewModel.QrCodeImage, Is.SameAs(previous));
            });
        }

        [Test]
        public void ExactPixelSize_ReplacesHeightAndWidth()
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.QrCodeWidth = 500;
            _viewModel.QrCodeHeight = 300;

            _viewModel.UseExactPixelSize = true;
            _viewModel.SelectedPixelsPerModule = 5;
            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.UseImageSize, Is.False);
                Assert.That(_viewModel.QrCodeImage.PixelWidth, Is.EqualTo(_viewModel.QrCodeImage.PixelHeight));
                Assert.That(_viewModel.QrCodeImage.PixelWidth % 5, Is.Zero);
                Assert.That(_viewModel.StatusMessage, Does.Contain($"{_viewModel.QrCodeImage.PixelWidth} x {_viewModel.QrCodeImage.PixelHeight} px"));
            });
        }

        [Test]
        public void ByDefault_HeightAndWidthDecideTheSize_WhateverTheSliderSays()
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.QrCodeWidth = 500;
            _viewModel.QrCodeHeight = 300;
            _viewModel.SelectedPixelsPerModule = 5;

            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_viewModel.UseImageSize, Is.True);
                Assert.That(_viewModel.QrCodeImage.PixelWidth, Is.EqualTo(500));
                Assert.That(_viewModel.QrCodeImage.PixelHeight, Is.EqualTo(300));
            });
        }

        [Test]
        public void TooSmallSize_Warns()
        {
            _viewModel.QrCodeContent = "https://example.com";
            _viewModel.QrCodeWidth = 10;
            _viewModel.QrCodeHeight = 10;

            _viewModel.GenerateQrCodeCommand.Execute(null);

            Assert.Multiple(() =>
            {
                Assert.That(_dialogs.Warnings, Has.Count.EqualTo(1));
                Assert.That(_viewModel.QrCodeImage, Is.Null);
            });
        }

        [Test]
        public void UnwritableOutputFolder_ShowsErrorInsteadOfThrowing()
        {
            // A file where the folder should be makes creating the folder fail.
            Directory.CreateDirectory(Path.GetDirectoryName(_folder));
            File.WriteAllText(_folder, string.Empty);

            try
            {
                _viewModel.QrCodeContent = "https://example.com";

                Assert.DoesNotThrow(() => _viewModel.GenerateQrCodeCommand.Execute(null));
                Assert.That(_dialogs.Errors, Has.Count.EqualTo(1));
                Assert.That(_viewModel.QrCodeImage, Is.Not.Null, "the preview still works");
            }
            finally
            {
                File.Delete(_folder);
            }
        }

        private sealed class FakeDialogService : IDialogService
        {
            public List<string> Warnings { get; } = [];
            public List<string> Errors { get; } = [];

            public string OpenFolderDialog(string initialDirectory = null) => string.Empty;

            public string OpenFileDialog() => string.Empty;

            public string SaveFileDialog(string defaultFileName) => string.Empty;

            public void ShowWarnMessageBox(string message) => Warnings.Add(message);

            public void ShowErrorMessageBox(string message) => Errors.Add(message);

            public void ShowMessageBox(string message, string title = "Info", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.Information)
            {
            }
        }

        private sealed class FakeLoggingService : ILoggingService
        {
            public void ApplySettings(LoggingSettings settings)
            {
            }

            public void Info(string message, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }

            public void Warn(string message, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }

            public void Error(string message, Exception exception = null, string callerMemberName = "", int callerLineNumber = -1, string callerFilePath = "")
            {
            }
        }
    }
}