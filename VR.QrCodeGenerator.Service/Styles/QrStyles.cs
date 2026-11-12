using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.Service.Styles.Shapes;

namespace VR.QrCodeGenerator.Service.Styles
{
    /// <summary>
    /// The built-in styles, one per <see cref="EnumQrStyle"/>. A style made at runtime (for example from a drawing) is
    /// passed to IQrCodeGeneratorService.Generate directly instead.
    /// </summary>
    public static class QrStyles
    {
        private static readonly IReadOnlyDictionary<EnumQrStyle, IQrStyle> All = new IQrStyle[]
        {
            new ClassicStyle(),
            new ShapeStyle(EnumQrStyle.Kurkod, new ChickenShape()),
            new ShapeStyle(EnumQrStyle.Cat, new CatShape()),
            new ShapeStyle(EnumQrStyle.Dog, new DogShape())
        }.ToDictionary(style => style.Style);

        public static IQrStyle Get(EnumQrStyle style)
        {
            return All.TryGetValue(style, out var result)
                ? result
                : throw new ArgumentOutOfRangeException(nameof(style), style, "This QR style is not registered in QrStyles.");
        }
    }
}
