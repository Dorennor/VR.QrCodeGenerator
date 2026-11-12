using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using VR.QrCodeGenerator.ViewModel.ViewModels;

namespace VR.QrCodeGenerator.WPF.Views
{
    /// <summary>
    /// A picture used as the template of the QR code. A picture can be chosen with the button or dropped onto the
    /// picture card, and the code can be dragged to where it should sit on the picture.
    /// </summary>
    public partial class PicturePage : Page
    {
        private readonly PictureCodesViewModel _viewModel;

        // While dragging: where in the code's square it was grabbed, so the square does not jump under the mouse.
        private Vector _grabOffset;

        // While dragging the corner handle: resizing instead of moving.
        private bool _resizing;

        /// <summary>
        /// One wheel notch changes the code by this share of the picture's shorter side.
        /// </summary>
        private const double WheelStep = 0.02;

        public PicturePage(PictureCodesViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            InitializeComponent();
        }

        private void Picture_OnDragOver(object sender, DragEventArgs eventArgs)
        {
            eventArgs.Effects = eventArgs.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            eventArgs.Handled = true;
        }

        private void Picture_OnDrop(object sender, DragEventArgs eventArgs)
        {
            if (eventArgs.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
                _viewModel.LoadPicture(files[0]);
        }

        /// <summary>
        /// Pressed on the code: start dragging it. Pressed elsewhere: put the code's centre there and keep dragging.
        /// Positions are in template pixels, since the canvas is inside the same Viewbox as the template.
        /// </summary>
        private void CodeArea_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
        {
            var area = (IInputElement)sender;
            var position = eventArgs.GetPosition(area);

            // The corner handle resizes; it is checked first, since it overlaps the square's corner.
            _resizing = position.X >= _viewModel.CodeHandleLeft && position.X < _viewModel.CodeHandleLeft + _viewModel.CodeHandleSize
                        && position.Y >= _viewModel.CodeHandleTop && position.Y < _viewModel.CodeHandleTop + _viewModel.CodeHandleSize;

            if (_resizing)
            {
                area.CaptureMouse();
                eventArgs.Handled = true;
                return;
            }

            var centre = new Point(_viewModel.CodeLeft + _viewModel.CodeSide / 2, _viewModel.CodeTop + _viewModel.CodeSide / 2);
            var onCode = position.X >= _viewModel.CodeLeft && position.X < _viewModel.CodeLeft + _viewModel.CodeSide
                         && position.Y >= _viewModel.CodeTop && position.Y < _viewModel.CodeTop + _viewModel.CodeSide;

            _grabOffset = onCode ? position - centre : new Vector();

            if (!onCode)
                _viewModel.MoveCodeTo(position.X, position.Y);

            area.CaptureMouse();
            eventArgs.Handled = true;
        }

        private void CodeArea_OnMouseMove(object sender, MouseEventArgs eventArgs)
        {
            var area = (IInputElement)sender;

            if (Mouse.Captured != area || eventArgs.LeftButton != MouseButtonState.Pressed)
                return;

            if (_resizing)
            {
                var corner = eventArgs.GetPosition(area);
                _viewModel.ResizeCodeTo(corner.X, corner.Y);
                return;
            }

            var position = eventArgs.GetPosition(area) - _grabOffset;
            _viewModel.MoveCodeTo(position.X, position.Y);
        }

        private void CodeArea_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs eventArgs)
        {
            _resizing = false;
            ((IInputElement)sender).ReleaseMouseCapture();
        }

        /// <summary>
        /// The mouse wheel over the picture resizes the code; the page does not scroll meanwhile.
        /// </summary>
        private void CodeArea_OnMouseWheel(object sender, MouseWheelEventArgs eventArgs)
        {
            _viewModel.ResizeCodeBy(Math.Sign(eventArgs.Delta) * WheelStep);
            eventArgs.Handled = true;
        }
    }
}
