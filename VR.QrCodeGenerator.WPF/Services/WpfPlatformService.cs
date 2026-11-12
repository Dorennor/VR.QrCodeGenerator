using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using VR.QrCodeGenerator.Service.Services;
using VR.QrCodeGenerator.WPF.Converters;

namespace VR.QrCodeGenerator.WPF.Services
{
    /// <summary>
    /// Files and folders as on any Windows app, plus copying the code to the Windows clipboard.
    /// </summary>
    public class WpfPlatformService : FileSystemPlatformService
    {
        public override Task CopyImageAsync(byte[] pngData)
        {
            var convertedBitmap = new FormatConvertedBitmap(PreviewImageConverter.LoadBitmap(pngData), PixelFormats.Bgra32, null, 0);

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    Clipboard.SetImage(convertedBitmap);
                    return Task.CompletedTask;
                }
                catch (COMException)
                {
                    if (i == 2)
                        throw;

                    Thread.Sleep(50);
                }
            }

            return Task.CompletedTask;
        }
    }
}