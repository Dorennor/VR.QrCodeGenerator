using VR.QrCodeGenerator.Model.Enums;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// Every style the generator can draw. Register a new <see cref="IQrStyle"/> here.
    /// </summary>
    public static class QrStyles
    {
        private static readonly IReadOnlyDictionary<EnumQrStyle, IQrStyle> All = new IQrStyle[]
        {
            new ClassicStyle(),
            new KurkodStyle()
        }.ToDictionary(style => style.Style);

        public static IQrStyle Get(EnumQrStyle style)
        {
            return All.TryGetValue(style, out var result)
                ? result
                : throw new ArgumentOutOfRangeException(nameof(style), style, "This QR style is not registered in QrStyles.");
        }
    }
}
