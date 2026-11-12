namespace VR.QrCodeGenerator.ViewModel.ViewModels
{
    /// <summary>
    /// How finely a drawing or picture around the code is drawn: the cells per module side (see ShapeStyle). Offered on
    /// the Drawing and Picture pages.
    /// </summary>
    public sealed record DetailOption(int CellsPerModule, string Name, string Description)
    {
        public static IReadOnlyList<DetailOption> All { get; } =
        [
            new(1, "Normal", "Dots as big as the code's squares: a chunky, pixel-art look like Kurkod."),
            new(2, "Fine", "Dots half the size of the code's squares."),
            new(3, "Extra fine", "Dots a third of the size of the code's squares."),
            new(4, "Ultra", "Dots a quarter of the size of the code's squares: the most detail.")
        ];
    }
}
