using System.Globalization;

using DrawingColor = System.Drawing.Color;

namespace VR.QrCodeGenerator.MAUI.Views
{
    /// <summary>
    /// Picks a color for the code or its background, in place of the Windows color canvas: common colors to tap, red,
    /// green and blue sliders, and a hex field. No transparency, as on Windows.
    /// </summary>
    public sealed class ColorPickerPage : ContentPage
    {
        private static readonly string[] Presets =
        [
            "#000000", "#FFFFFF", "#404040", "#808080",
            "#C00000", "#E81123", "#FF8C00", "#FFB900",
            "#107C10", "#00B294", "#0078D4", "#002050",
            "#5C2D91", "#B4009E", "#8B4513", "#F5F0E1"
        ];

        private readonly TaskCompletionSource<DrawingColor?> _result = new();
        private readonly BoxView _preview = new() { HeightRequest = 56, CornerRadius = 8 };
        private readonly Entry _hex = new() { Keyboard = Keyboard.Text, MaxLength = 7, Placeholder = "#RRGGBB", FontFamily = "monospace" };
        private readonly (Slider Slider, Label Value)[] _channels;

        private bool _updating;

        private ColorPickerPage(string title, DrawingColor initial)
        {
            Title = title;
            Padding = 16;

            _channels = [Channel(), Channel(), Channel()];

            _hex.Completed += (_, _) => ApplyHex();
            _hex.Unfocused += (_, _) => ApplyHex();

            var presets = new Grid { ColumnSpacing = 8, RowSpacing = 8 };

            for (var i = 0; i < 4; i++)
            {
                presets.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                presets.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            for (var i = 0; i < Presets.Length; i++)
            {
                var color = Color.FromArgb(Presets[i]);
                var swatch = new Border
                {
                    Style = (Style)Application.Current!.Resources["ColorSwatchBorderStyle"],
                    WidthRequest = -1,
                    HeightRequest = 40,
                    BackgroundColor = color
                };

                swatch.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => SetColor(ToDrawing(color))) });
                SemanticProperties.SetDescription(swatch, Presets[i]);
                presets.Add(swatch, i % 4, i / 4);
            }

            var cancel = new Button { Text = "Cancel" };
            cancel.Clicked += async (_, _) => await CloseAsync(null);

            var ok = new Button { Text = "OK", Style = (Style)Application.Current.Resources["PrimaryButtonStyle"] };
            ok.Clicked += async (_, _) =>
            {
                ApplyHex();
                await CloseAsync(CurrentColor);
            };

            var buttons = new Grid { ColumnSpacing = 8, Margin = new Thickness(0, 16, 0, 0), ColumnDefinitions = { new(GridLength.Star), new(GridLength.Star) } };
            buttons.Add(cancel, 0, 0);
            buttons.Add(ok, 1, 0);

            var layout = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = title, FontSize = 20, FontAttributes = FontAttributes.Bold },
                    _preview,
                    presets,
                    Row("Red", _channels[0]),
                    Row("Green", _channels[1]),
                    Row("Blue", _channels[2]),
                    Row("Hex", _hex),
                    buttons
                }
            };

            Content = new ScrollView { Content = layout };

            SetColor(initial);
        }

        private DrawingColor CurrentColor => DrawingColor.FromArgb((int)_channels[0].Slider.Value, (int)_channels[1].Slider.Value, (int)_channels[2].Slider.Value);

        /// <summary>
        /// Shows the picker; returns the chosen color, or null when cancelled (also with the Back button).
        /// </summary>
        public static async Task<DrawingColor?> PickAsync(INavigation navigation, string title, DrawingColor initial)
        {
            var page = new ColorPickerPage(title, initial);

            await navigation.PushModalAsync(page, true);

            return await page._result.Task;
        }

        protected override bool OnBackButtonPressed()
        {
            _ = CloseAsync(null);
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Closed some other way: same as Cancel.
            _result.TrySetResult(null);
        }

        private async Task CloseAsync(DrawingColor? color)
        {
            _result.TrySetResult(color);
            await Navigation.PopModalAsync(true);
        }

        private (Slider Slider, Label Value) Channel()
        {
            var slider = new Slider { Maximum = 255, Minimum = 0 };
            var value = new Label { Style = (Style)Application.Current!.Resources["ValueLabelStyle"] };

            slider.ValueChanged += (_, _) =>
            {
                // Whole values, like the Windows color canvas.
                var rounded = Math.Round(slider.Value);

                if (Math.Abs(slider.Value - rounded) > 0.001)
                {
                    slider.Value = rounded;
                    return;
                }

                value.Text = ((int)rounded).ToString(CultureInfo.InvariantCulture);

                if (!_updating)
                    ShowColor(CurrentColor, updateHex: true);
            };

            return (slider, value);
        }

        private static Grid Row(string name, (Slider Slider, Label Value) channel)
        {
            var row = Row(name, channel.Slider);
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.Add(channel.Value, 2, 0);
            return row;
        }

        private static Grid Row(string name, View control)
        {
            var row = new Grid { ColumnSpacing = 12, ColumnDefinitions = { new(new GridLength(56)), new(GridLength.Star) } };
            row.Add(new Label { Text = name, VerticalOptions = LayoutOptions.Center }, 0, 0);
            row.Add(control, 1, 0);
            return row;
        }

        private void SetColor(DrawingColor color)
        {
            _updating = true;

            _channels[0].Slider.Value = color.R;
            _channels[1].Slider.Value = color.G;
            _channels[2].Slider.Value = color.B;

            _updating = false;

            ShowColor(color, updateHex: true);
        }

        private void ShowColor(DrawingColor color, bool updateHex)
        {
            _preview.Color = Color.FromRgb(color.R, color.G, color.B);

            if (updateHex)
                _hex.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// A typed #RRGGBB (or RRGGBB) becomes the color; anything else puts the current color back in the field.
        /// </summary>
        private void ApplyHex()
        {
            var text = _hex.Text?.Trim().TrimStart('#') ?? string.Empty;

            if (text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                SetColor(DrawingColor.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF));
            else
                ShowColor(CurrentColor, updateHex: true);
        }

        private static DrawingColor ToDrawing(Color color)
        {
            color.ToRgb(out var red, out var green, out var blue);
            return DrawingColor.FromArgb(red, green, blue);
        }
    }
}