using VR.QrCodeGenerator.MAUI.Services;

namespace VR.QrCodeGenerator.MAUI.Controls
{
    /// <summary>
    /// An option row, like SettingsCard on Windows: a header, a description and a control. Stacked (the default), the
    /// control goes below the text, which suits most controls on a phone; otherwise it sits at the right.
    /// <para>
    /// <see cref="HelpText"/> adds an ⓘ button that explains the option, the touch version of the Windows tooltips.
    /// <see cref="IsActive"/> disables the control but keeps the help, as Windows shows tooltips on disabled cards.
    /// </para>
    /// </summary>
    [ContentProperty(nameof(Control))]
    public class SettingsCard : ContentView
    {
        public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(string), typeof(SettingsCard));

        public static readonly BindableProperty DescriptionProperty =
            BindableProperty.Create(nameof(Description), typeof(string), typeof(SettingsCard), propertyChanged: (card, _, _) => ((SettingsCard)card).UpdateDescription());

        public static readonly BindableProperty HelpTextProperty =
            BindableProperty.Create(nameof(HelpText), typeof(string), typeof(SettingsCard), propertyChanged: (card, _, _) => ((SettingsCard)card).UpdateHelp());

        public static readonly BindableProperty ControlProperty =
            BindableProperty.Create(nameof(Control), typeof(View), typeof(SettingsCard), propertyChanged: (card, _, _) => ((SettingsCard)card).UpdateLayout());

        public static readonly BindableProperty IsStackedProperty =
            BindableProperty.Create(nameof(IsStacked), typeof(bool), typeof(SettingsCard), true, propertyChanged: (card, _, _) => ((SettingsCard)card).UpdateLayout());

        public static readonly BindableProperty IsActiveProperty =
            BindableProperty.Create(nameof(IsActive), typeof(bool), typeof(SettingsCard), true, propertyChanged: (card, _, _) => ((SettingsCard)card).UpdateActive());

        private readonly Grid _grid;
        private readonly Label _header;
        private readonly Label _description;
        private readonly Label _help;
        private readonly ContentView _controlHost = new();

        public SettingsCard()
        {
            _header = new Label { FontSize = 15, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap };
            _header.SetBinding(Label.TextProperty, static (SettingsCard card) => card.Header, source: this);

            _description = new Label { Style = (Style)Application.Current!.Resources["CaptionLabelStyle"], Margin = new Thickness(0, 2, 0, 0) };
            _description.SetBinding(Label.TextProperty, static (SettingsCard card) => card.Description, source: this);

            // A big touch target around a small mark.
            _help = new Label
            {
                Text = "ⓘ",
                FontSize = 18,
                Padding = new Thickness(10, 0, 4, 0),
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            _help.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#0078D4"), Color.FromArgb("#4CC2FF"));
            _help.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await MauiDialogService.ShowAsync(Header, HelpText)) });
            SemanticProperties.SetDescription(_help, "Help");

            _grid = new Grid { ColumnSpacing = 8 };

            Content = new Border { Style = (Style)Application.Current.Resources["CardBorderStyle"], Content = _grid };

            UpdateDescription();
            UpdateLayout();
        }

        public string Header { get => (string)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

        public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        public string HelpText { get => (string)GetValue(HelpTextProperty); set => SetValue(HelpTextProperty, value); }

        public View Control { get => (View)GetValue(ControlProperty); set => SetValue(ControlProperty, value); }

        public bool IsStacked { get => (bool)GetValue(IsStackedProperty); set => SetValue(IsStackedProperty, value); }

        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

        private void UpdateDescription() => _description.IsVisible = !string.IsNullOrEmpty(Description);

        private void UpdateHelp() => _help.IsVisible = !string.IsNullOrEmpty(HelpText);

        private void UpdateActive()
        {
            _controlHost.IsEnabled = IsActive;
            _controlHost.Opacity = IsActive ? 1 : 0.4;
            _header.Opacity = _description.Opacity = IsActive ? 1 : 0.6;
        }

        /// <summary>
        /// Stacked: header and ⓘ, description, then the control. Side by side: the text left, the control right.
        /// </summary>
        private void UpdateLayout()
        {
            _grid.Clear();
            _grid.RowDefinitions.Clear();
            _grid.ColumnDefinitions.Clear();

            _controlHost.Content = Control;

            var headerRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            headerRow.Add(_header, 0, 0);
            headerRow.Add(_help, 1, 0);
            var text = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { headerRow, _description } };

            if (IsStacked)
            {
                _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _controlHost.Margin = Control is null ? new Thickness(0) : new Thickness(0, 8, 0, 0);

                _grid.Add(text, 0, 0);
                _grid.Add(_controlHost, 0, 1);
            }
            else
            {
                _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                _controlHost.Margin = new Thickness(0);

                _grid.Add(text, 0, 0);
                _grid.Add(_controlHost, 1, 0);
            }

            UpdateHelp();
            UpdateActive();
        }
    }
}