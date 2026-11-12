using Android.Content;
using Android.Provider;

using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Model.Models.Settings;
using VR.QrCodeGenerator.Service.Helpers;
using VR.QrCodeGenerator.Service.Interfaces;

using AndroidUri = Android.Net.Uri;
using FileProvider = AndroidX.Core.Content.FileProvider;

namespace VR.QrCodeGenerator.MAUI.Services
{
    /// <summary>
    /// Android's way of doing what the Windows app does with folders and the clipboard:
    /// <list type="bullet">
    /// <item>Codes are saved to Pictures/QR Code Generator, so they show up in the gallery (SVG, which galleries do not
    /// show, to Download/QR Code Generator). A custom folder is one chosen in the system folder picker.</item>
    /// <item>"Open output folder" opens the last saved code; logs are shared rather than opened.</item>
    /// <item>Copy puts the PNG on the clipboard, and Share sends it to another app.</item>
    /// </list>
    /// </summary>
    public sealed class AndroidPlatformService : IPlatformService
    {
        public const string FolderName = "QR Code Generator";

        // Matches the provider in AndroidManifest.xml and its paths in Resources/xml/qr_file_paths.xml.
        private const string FileProviderAuthority = "com.vr.qrcodegenerator.qrfiles";

        private static readonly string PicturesFolder = $"{Android.OS.Environment.DirectoryPictures}/{FolderName}";
        private static readonly string DownloadsFolder = $"{Android.OS.Environment.DirectoryDownloads}/{FolderName}";

        private readonly Lock _lastSavedLock = new();

        // The last saved code, for "Open output folder".
        private (AndroidUri Uri, string MimeType)? _lastSaved;

        public bool CanShare => true;

        private static Context Context => Platform.AppContext;

        public string GetOutputFolder(PathsSettings settings) =>
            CustomFolder(settings) is { } folder ? DescribeFolder(folder) : PicturesFolder;

        public string SaveToOutputFolder(byte[] content, EnumImageFormat format, PathsSettings settings)
        {
            var name = OutputHelper.CreateFileName(format);
            var mimeType = OutputHelper.GetMimeType(format);
            var resolver = Context.ContentResolver ?? throw new IOException("No content resolver.");

            AndroidUri file;
            string location;

            if (CustomFolder(settings) is { } folder)
            {
                // A folder from the system picker: create the file through the documents provider that owns it.
                var directory = DocumentsContract.BuildDocumentUriUsingTree(folder, DocumentsContract.GetTreeDocumentId(folder));

                file = DocumentsContract.CreateDocument(resolver, directory, mimeType, name) ?? throw new IOException($"Could not create {name}.");
                Write(resolver, file, content);
                location = $"{DescribeFolder(folder)}/{name}";
            }
            else
            {
                // The shared Pictures (or Download) folder through MediaStore: no permission needed, and the gallery
                // sees the file once it is complete (IS_PENDING).
                var isSvg = format == EnumImageFormat.Svg;
                var relativePath = isSvg ? DownloadsFolder : PicturesFolder;

                var values = new ContentValues();
                values.Put("_display_name", name);
                values.Put("mime_type", mimeType);
                values.Put("relative_path", relativePath);
                values.Put("is_pending", 1);

                var collection = isSvg ? MediaStore.Downloads.ExternalContentUri : MediaStore.Images.Media.ExternalContentUri;

                file = resolver.Insert(collection, values) ?? throw new IOException($"Could not create {name} in {relativePath}.");

                try
                {
                    Write(resolver, file, content);
                }
                catch
                {
                    resolver.Delete(file, null, null);
                    throw;
                }

                values.Clear();
                values.Put("is_pending", 0);
                resolver.Update(file, values, null, null);

                location = $"{relativePath}/{name}";
            }

            lock (_lastSavedLock)
            {
                _lastSaved = (file, mimeType);
            }

            return location;
        }

        public Task OpenOutputFolderAsync(PathsSettings settings) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                (AndroidUri Uri, string MimeType)? lastSaved;

                lock (_lastSavedLock)
                {
                    lastSaved = _lastSaved;
                }

                // Android has no common way to open a folder: show the last saved code in the viewer, where the others
                // are a swipe away; before anything is saved, the gallery.
                var intent = lastSaved is { } saved
                    ? new Intent(Intent.ActionView).SetDataAndType(saved.Uri, saved.MimeType)
                    : new Intent(Intent.ActionView).SetDataAndType(MediaStore.Images.Media.ExternalContentUri, "image/*");

                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                Context.StartActivity(intent);
            });

        public async Task OpenLogsAsync(string logsFolder)
        {
            // Nobody browses an app's private files on a phone, so the logs are shared (email, messenger, Drive…).
            var logs = Directory.Exists(logsFolder) ? Directory.GetFiles(logsFolder, "*.log").OrderByDescending(File.GetLastWriteTimeUtc).ToList() : [];

            if (logs.Count == 0)
                throw new FileNotFoundException("There are no log files yet.", logsFolder);

            await Share.Default.RequestAsync(new ShareMultipleFilesRequest("QR Code Generator logs", logs.Select(log => new ShareFile(log, "text/plain")).ToList()));
        }

        public Task CopyImageAsync(byte[] png) =>
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                // The clipboard holds a link to the image; the provider lets the app that pastes read it.
                var file = WriteToCache("clipboard", png);
                var uri = FileProvider.GetUriForFile(Context, FileProviderAuthority, new Java.IO.File(file));
                var clipboard = (ClipboardManager)Context.GetSystemService(Context.ClipboardService) ?? throw new InvalidOperationException("No clipboard.");

                clipboard.PrimaryClip = ClipData.NewUri(Context.ContentResolver, "QR code", uri);
            });

        public Task ShareImageAsync(byte[] png) =>
            Share.Default.RequestAsync(new ShareFileRequest("QR code", new ShareFile(WriteToCache("share", png), "image/png")));

        /// <summary>
        /// The custom folder from the settings, when one was chosen in the folder picker (a content:// tree URI).
        /// </summary>
        private static AndroidUri CustomFolder(PathsSettings settings)
        {
            if (settings is not { UseCustomPath: true } || string.IsNullOrWhiteSpace(settings.OutputPath))
                return null;

            return settings.OutputPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase) ? AndroidUri.Parse(settings.OutputPath) : null;
        }

        /// <summary>
        /// A picked folder as people know it, such as "Internal storage/Documents/QR" for primary:Documents/QR.
        /// </summary>
        private static string DescribeFolder(AndroidUri folder)
        {
            try
            {
                var parts = DocumentsContract.GetTreeDocumentId(folder).Split(':', 2);
                var volume = parts[0] == "primary" ? "Internal storage" : parts[0];

                return parts.Length > 1 && parts[1].Length > 0 ? $"{volume}/{parts[1]}" : volume;
            }
            catch (Exception)
            {
                return folder.ToString();
            }
        }

        private static void Write(ContentResolver resolver, AndroidUri file, byte[] content)
        {
            using var output = resolver.OpenOutputStream(file) ?? throw new IOException($"Could not write {file}.");

            output.Write(content, 0, content.Length);
        }

        /// <summary>
        /// A fresh "QR code.png" in its own cache folder; the previous one is removed.
        /// </summary>
        private static string WriteToCache(string folderName, byte[] png)
        {
            var folder = Path.Combine(FileSystem.CacheDirectory, folderName);

            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, "QR code.png");
            File.WriteAllBytes(path, png);

            return path;
        }
    }
}