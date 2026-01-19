using System;

namespace VR.QrCodeGenerator.WinUI.Helpers
{
    public static class UriHelper
    {
        public static bool IsValidUrl(string urlString)
        {
            if (Uri.TryCreate(urlString, UriKind.Absolute, out Uri uri))
                return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            return false;
        }
    }
}