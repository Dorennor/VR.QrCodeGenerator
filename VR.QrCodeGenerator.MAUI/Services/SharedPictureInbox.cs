namespace VR.QrCodeGenerator.MAUI.Services
{
    /// <summary>
    /// A picture shared from another app, from the activity to the Picture tab. It waits here when it arrives before
    /// the app's window is ready (the share started the app).
    /// </summary>
    public static class SharedPictureInbox
    {
        private static readonly Lock Gate = new();
        private static string _pending;

        /// <summary>
        /// A picture arrived; take it with <see cref="TakePending"/>.
        /// </summary>
        public static event Action Received;

        public static void Deliver(string path)
        {
            lock (Gate)
            {
                _pending = path;
            }

            Received?.Invoke();
        }

        /// <summary>
        /// The waiting picture's path, once; null when there is none.
        /// </summary>
        public static string TakePending()
        {
            lock (Gate)
            {
                var path = _pending;
                _pending = null;
                return path;
            }
        }
    }
}