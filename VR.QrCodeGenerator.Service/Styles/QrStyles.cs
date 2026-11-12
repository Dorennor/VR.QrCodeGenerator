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
        private static readonly IReadOnlyDictionary<EnumQrStyle, IQrStyle> All = new Dictionary<EnumQrStyle, IQrStyle>
        {
            [EnumQrStyle.Classic] = new ClassicStyle(),
            [EnumQrStyle.Kurkod] = new ShapeStyle(new ChickenShape()),
            [EnumQrStyle.Cat] = new ShapeStyle(new CatShape()),
            [EnumQrStyle.Dog] = new ShapeStyle(new DogShape())
        };

        public static IQrStyle Get(EnumQrStyle style)
        {
            return All.TryGetValue(style, out var result)
                ? result
                : throw new ArgumentOutOfRangeException(nameof(style), style, "This QR style is not registered in QrStyles.");
        }
    }
}
