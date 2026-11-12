using VR.QrCodeGenerator.MAUI.Controls;
using VR.QrCodeGenerator.MAUI.Services;
using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.MAUI.Views
{
    /// <summary>
    /// Draw around the code with a finger and get a QR code shaped like the drawing. The sheet (DrawingSurface) passes
    /// its drawing to the view model after every change, as the Windows canvas does.
    /// </summary>
    public partial class DrawingPage : ContentPage
    {
        private const string ToolsHelp =
            "• Draw: draw with your finger. Lines become solid black; shapes you close are filled with a QR-like pattern.\n\n" +
            "• Erase: touch or swipe over a line to remove the whole line.\n\n" +
            "• Move code: move and resize the QR code (the blue square) instead of drawing. Drag the square to move it, " +
            "drag its white corner to make it bigger or smaller, or touch anywhere to put the code there.\n\n" +
            "Pinching with two fingers resizes the code with every tool.";

        public DrawingPage(DrawingCodesViewModel viewModel)
        {
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();

            ShowTool(DrawingTool.Draw);
        }

        private void Tool_OnClicked(object sender, EventArgs eventArgs) =>
            ShowTool(sender == EraseToolButton ? DrawingTool.Erase : sender == MoveToolButton ? DrawingTool.MoveCode : DrawingTool.Draw);

        /// <summary>
        /// Selects the tool and highlights its button, like the Windows radio buttons.
        /// </summary>
        private void ShowTool(DrawingTool tool)
        {
            Surface.Tool = tool;

            var selected = (Style)Application.Current!.Resources["PrimaryButtonStyle"];

            // Null falls back to the implicit button style.
            DrawToolButton.Style = tool == DrawingTool.Draw ? selected : null;
            EraseToolButton.Style = tool == DrawingTool.Erase ? selected : null;
            MoveToolButton.Style = tool == DrawingTool.MoveCode ? selected : null;
        }

        private async void ToolsHelp_OnTapped(object sender, TappedEventArgs eventArgs) => await MauiDialogService.ShowAsync("Drawing tools", ToolsHelp);

        /// <summary>
        /// Even sizes from 4 to 24, like the Windows slider's ticks; only new lines change.
        /// </summary>
        private void PenSize_OnValueChanged(object sender, ValueChangedEventArgs eventArgs)
        {
            var size = Math.Round(eventArgs.NewValue / 2) * 2;

            if (Surface is not null)
                Surface.PenSize = size;

            if (PenSizeLabel is not null)
                PenSizeLabel.Text = size.ToString("0");
        }

        private void Undo_OnClicked(object sender, EventArgs eventArgs) => Surface.Undo();

        private void Clear_OnClicked(object sender, EventArgs eventArgs) => Surface.Clear();
    }
}