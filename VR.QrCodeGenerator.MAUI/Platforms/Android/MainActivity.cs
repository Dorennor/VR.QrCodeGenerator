using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;

using VR.QrCodeGenerator.MAUI.Services;

namespace VR.QrCodeGenerator.MAUI
{
    /// <summary>
    /// The one activity. It also receives pictures shared from other apps (the Android version of dropping a file on
    /// the Picture tab) and returns the folder chosen for the custom output folder.
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme",
              MainLauncher = true,
              // One instance: a picture shared from another app comes to the running app instead of a second copy.
              LaunchMode = LaunchMode.SingleTask,
              ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [IntentFilter([Intent.ActionSend], Categories = [Intent.CategoryDefault], DataMimeType = "image/*")]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int PickFolderRequest = 4711;

        private static TaskCompletionSource<Android.Net.Uri> _pickFolder;

        /// <summary>
        /// Shows the system folder picker; returns the chosen folder (a tree URI with lasting read and write access),
        /// or null when cancelled.
        /// </summary>
        public static Task<Android.Net.Uri> PickFolderAsync()
        {
            if (Platform.CurrentActivity is not MainActivity activity)
                throw new InvalidOperationException("The app is not in the foreground.");

            _pickFolder?.TrySetResult(null);
            _pickFolder = new TaskCompletionSource<Android.Net.Uri>();

            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

            activity.StartActivityForResult(intent, PickFolderRequest);

            return _pickFolder.Task;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Started by sharing a picture with the app.
            if (savedInstanceState is null)
                ReceiveSharedPicture(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            ReceiveSharedPicture(intent);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode != PickFolderRequest)
                return;

            var folder = resultCode == Result.Ok ? data?.Data : null;

            if (folder is not null)
            {
                // Keep access after a restart: the folder is saved in the settings.
                ContentResolver?.TakePersistableUriPermission(folder, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            }

            _pickFolder?.TrySetResult(folder);
            _pickFolder = null;
        }

        /// <summary>
        /// Copies a shared picture into the cache (the link only works for a while) and hands it to the Picture tab.
        /// </summary>
        private void ReceiveSharedPicture(Intent intent)
        {
            if (intent?.Action != Intent.ActionSend || intent.Type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                return;

            var uri = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? intent.GetParcelableExtra(Intent.ExtraStream, Java.Lang.Class.FromType(typeof(Android.Net.Uri))) as Android.Net.Uri
#pragma warning disable CA1422 // The typed overload above is used from Android 13.
                : intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
#pragma warning restore CA1422

            if (uri is null || ContentResolver is not { } resolver)
                return;

            try
            {
                var folder = Path.Combine(FileSystem.CacheDirectory, "shared");

                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);

                Directory.CreateDirectory(folder);

                var path = Path.Combine(folder, GetDisplayName(resolver, uri));

                using (var input = resolver.OpenInputStream(uri) ?? throw new IOException("The shared picture cannot be read."))
                using (var output = File.Create(path))
                {
                    input.CopyTo(output);
                }

                SharedPictureInbox.Deliver(path);
            }
            catch (Exception ex)
            {
                CrashLog.Write(ex);
            }
        }

        private static string GetDisplayName(ContentResolver resolver, Android.Net.Uri uri)
        {
            using var cursor = resolver.Query(uri, [IOpenableColumns.DisplayName], null, null, null);

            var name = cursor is not null && cursor.MoveToFirst() ? cursor.GetString(0) : null;

            // The name becomes a file name: keep only its last part and no characters a file name cannot have.
            name = string.IsNullOrWhiteSpace(name) ? "Shared picture" : Path.GetFileName(name);

            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');

            return name;
        }
    }
}