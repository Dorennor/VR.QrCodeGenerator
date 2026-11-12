using System.ComponentModel;

using VR.QrCodeGenerator.MAUI.Converters;
using VR.QrCodeGenerator.MAUI.Views;
using VR.QrCodeGenerator.Model.Enums;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Controls
{
    /// <summary>
    /// The generator layout shared by the four generator tabs. The view model decides the description; <see cref="TopContent"/>
    /// adds a page's own section (style picker, drawing, picture) above Generate.
    /// </summary>
    public partial class QrGeneratorView : ContentView
    {
        public static readonly BindableProperty TopContentProperty =
            BindableProperty.Create(nameof(TopContent), typeof(View), typeof(QrGeneratorView), propertyChanged: (view, _, value) => ((QrGeneratorView)view).TopHost.Content = (View)value);

        // The image size, like the Windows number boxes.
        private const int MinimumSize = 10;
        private const int MaximumSize = 5000;

        private MainViewModel _viewModel;

        public QrGeneratorView()
        {
            InitializeComponent();

            // The selected values come from the view model through the SelectedItem bindings.
            ContentTypePicker.ItemsSource = Enum.GetValues<EnumContentType>();
            FormatPicker.ItemsSource = Enum.GetValues<EnumImageFormat>();
            EccPicker.ItemsSource = EccTagIndexConverter.Names.ToList();
        }

        /// <summary>
        /// Shown between the content and Generate. It inherits the page's view model as its BindingContext.
        /// </summary>
        public View TopContent
        {
            get => (View)GetValue(TopContentProperty);
            set => SetValue(TopContentProperty, value);
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            if (_viewModel is not null)
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;

            _viewModel = BindingContext as MainViewModel;

            if (_viewModel is not null)
                _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        }

        /// <summary>
        /// On a phone the preview is below the fold: bring it into view while generating and when the code is ready.
        /// </summary>
        private void ViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName is nameof(MainViewModel.IsBusy) && _viewModel.IsBusy
                || eventArgs.PropertyName is nameof(MainViewModel.QrCodeImage) && _viewModel.QrCodeImage is not null)
            {
                Dispatcher.Dispatch(async () => await Scroller.ScrollToAsync(PreviewCard, ScrollToPosition.MakeVisible, true));
            }
        }

        /// <summary>
        /// Puts an empty or out-of-range size back within 10 to 5000 pixels.
        /// </summary>
        private void SizeEntry_OnUnfocused(object sender, FocusEventArgs eventArgs)
        {
            if (_viewModel is null)
                return;

            var isHeight = sender == HeightEntry;
            var entry = (Entry)sender;
            var current = isHeight ? _viewModel.QrCodeHeight : _viewModel.QrCodeWidth;
            var value = int.TryParse(entry.Text, out var typed) ? Math.Clamp(typed, MinimumSize, MaximumSize) : current;

            if (isHeight)
                _viewModel.QrCodeHeight = value;
            else
                _viewModel.QrCodeWidth = value;

            entry.Text = value.ToString();
        }

        private async void ForegroundSwatch_OnTapped(object sender, TappedEventArgs eventArgs)
        {
            if (_viewModel is not null && await ColorPickerPage.PickAsync(Navigation, "QR code color", _viewModel.SelectedForegroundColor) is { } color)
                _viewModel.SelectedForegroundColor = color;
        }

        private async void BackgroundSwatch_OnTapped(object sender, TappedEventArgs eventArgs)
        {
            if (_viewModel is not null && await ColorPickerPage.PickAsync(Navigation, "Background color", _viewModel.SelectedBackgroundColor) is { } color)
                _viewModel.SelectedBackgroundColor = color;
        }
    }
}