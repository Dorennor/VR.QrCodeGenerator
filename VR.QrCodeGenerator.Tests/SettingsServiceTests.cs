using System.IO;

using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Services;

namespace VR.QrCodeGenerator.Tests
{
    [TestFixture]
    public class SettingsServiceTests
    {
        private string _folder;
        private string _settingsFile;
        private string _legacyFile;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "VR.QrCodeGenerator.Tests", Guid.NewGuid().ToString());
            _settingsFile = Path.Combine(_folder, "AppData", "settings.json");
            _legacyFile = Path.Combine(_folder, "appsettings.json");
            Directory.CreateDirectory(_folder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        [Test]
        public void Load_WithoutAnyFile_ReturnsDefaults()
        {
            var settings = new SettingsService(_settingsFile, _legacyFile).Load();

            Assert.Multiple(() =>
            {
                Assert.That(settings.LoggingSettings.File, Is.True);
                Assert.That(settings.LoggingSettings.Debug, Is.False);
                Assert.That(settings.PathsSettings.UseCustomPath, Is.False);
            });
        }

        [Test]
        public void Save_ThenLoad_ReturnsSavedValues()
        {
            var service = new SettingsService(_settingsFile, _legacyFile);

            service.Save(new Settings
            {
                LoggingSettings = new LoggingSettings { File = false, Debug = true, Console = true },
                PathsSettings = new PathsSettings { UseCustomPath = true, OutputPath = @"D:\QR" }
            });

            var loaded = service.Load();

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(_settingsFile), Is.True);
                Assert.That(loaded.LoggingSettings.File, Is.False);
                Assert.That(loaded.LoggingSettings.Debug, Is.True);
                Assert.That(loaded.LoggingSettings.Console, Is.True);
                Assert.That(loaded.PathsSettings.UseCustomPath, Is.True);
                Assert.That(loaded.PathsSettings.OutputPath, Is.EqualTo(@"D:\QR"));
            });
        }

        [Test]
        public void Load_FileFromOlderVersionWithKurkodSetting_StillLoads()
        {
            // The Kurkod switch lived in Settings before it moved to the Custom codes page.
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
            File.WriteAllText(_settingsFile, """{ "PathsSettings": { "UseCustomPath": true, "OutputPath": "D:\\QR" }, "StyleSettings": { "ChickenShape": true } }""");

            var settings = new SettingsService(_settingsFile, _legacyFile).Load();

            Assert.That(settings.PathsSettings.OutputPath, Is.EqualTo(@"D:\QR"));
        }

        [Test]
        public void Load_WithOnlyLegacyFile_ReadsIt()
        {
            File.WriteAllText(_legacyFile, """{ "PathsSettings": { "UseCustomPath": true, "OutputPath": "D:\\Old" } }""");

            var settings = new SettingsService(_settingsFile, _legacyFile).Load();

            Assert.Multiple(() =>
            {
                Assert.That(settings.PathsSettings.OutputPath, Is.EqualTo(@"D:\Old"));
                Assert.That(settings.LoggingSettings, Is.Not.Null);
            });
        }

        [Test]
        public void Load_WithDamagedFile_ReturnsDefaults()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
            File.WriteAllText(_settingsFile, "{ not json");

            var settings = new SettingsService(_settingsFile, _legacyFile).Load();

            Assert.That(settings.LoggingSettings.File, Is.True);
        }
    }
}
