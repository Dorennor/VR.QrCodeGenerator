using System.Windows;
using System.Windows.Controls;

namespace VR.QrCodeGenerator.WPF.Controls
{
    /// <summary>
    /// A Windows 11 Settings style row: icon, header and description on the left, the content (the control) on the right.
    /// With <see cref="IsStacked"/> the content is placed below the text and stretches, for wide inputs.
    /// The template lives in Styles/AppStyles.xaml.
    /// </summary>
    public class SettingsCard : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(SettingsCard), new PropertyMetadata(null));

        public static readonly DependencyProperty IsStackedProperty =
            DependencyProperty.Register(nameof(IsStacked), typeof(bool), typeof(SettingsCard), new PropertyMetadata(false));

        /// <summary>
        /// A plain-language explanation shown when hovering the card, with an info mark next to the header.
        /// </summary>
        public static readonly DependencyProperty HelpTextProperty =
            DependencyProperty.Register(nameof(HelpText), typeof(string), typeof(SettingsCard), new PropertyMetadata(null, OnHelpTextChanged));

        public string HelpText
        {
            get => (string)GetValue(HelpTextProperty);
            set => SetValue(HelpTextProperty, value);
        }

        private static void OnHelpTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var card = (SettingsCard)sender;
            var text = eventArgs.NewValue as string;

            card.ToolTip = string.IsNullOrWhiteSpace(text)
                ? null
                : new ToolTip { Style = Application.Current?.TryFindResource("HelpToolTipStyle") as Style, Content = text };

            // Long enough to read, and also on disabled cards, where it explains what the option needs.
            ToolTipService.SetShowDuration(card, 60_000);
            ToolTipService.SetInitialShowDelay(card, 400);
            ToolTipService.SetShowOnDisabled(card, true);
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public bool IsStacked
        {
            get => (bool)GetValue(IsStackedProperty);
            set => SetValue(IsStackedProperty, value);
        }
    }
}