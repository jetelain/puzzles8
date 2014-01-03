using PuzzleCollection.CommonDX;
using PuzzleCollection.Games;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// Pour en savoir plus sur le modèle d'élément Contrôle basé sur un modèle, consultez la page http://go.microsoft.com/fwlink/?LinkId=234235

namespace PuzzleCollection
{
    [TemplatePart(Name="rectangle", Type=typeof(Rectangle))]
    public sealed class GameSurfaceControl : Control
    {
        private readonly ImageBrush d2dBrush;
        
        private Rectangle rectangle;
        private IGameController controller;
        private Drawing drawing;
        private SurfaceImageSourceTarget d2dTarget;
        private DeviceManager deviceManager;
        private volatile bool isDirectXReady;
        private volatile bool isSizeKnown;

        private Buttons release = Buttons.NONE;
        private bool tapHandled = false; // Right click was handled by game, thus should intercept righttap
        private Buttons press = Buttons.NONE;
        private bool delayedPressed;
        private Windows.Foundation.Point lastCursorPosition;
        private bool isLeftAsRight;

        public GameSurfaceControl()
        {
            this.DefaultStyleKey = typeof(GameSurfaceControl);
            this.Unloaded += OnUnloaded;
            this.SizeChanged += OnSizeChanged;

            d2dBrush = new ImageBrush();
        }

        private void OnSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            isSizeKnown = true;
            if (controller != null)
            {
                InitializeDirectX();
            }
        }

        private void OnUnloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            DisposeDirectX();
        }

        internal void SetGameController(IGameController setController)
        {
            controller = setController;
            controller.Redraw += AskRender;
            if (isSizeKnown)
            {
                InitializeDirectX();
            }
        }

        private static int ToPhysicalPixels(double locicalPixel)
        {
            return (int)(locicalPixel * DisplayProperties.LogicalDpi / 96.0);
        }

        private static int ToGamePixels(double locicalPixel)
        {
            return (int)locicalPixel;
        }

        private void InitializeDirectX()
        {
            int pixelWidth = ToPhysicalPixels(ActualWidth);
            int pixelHeight = ToPhysicalPixels(ActualHeight);

            controller.DrawSurfaceSizeChanged(ToGamePixels(ActualWidth), ToGamePixels(ActualHeight));

            if (isDirectXReady)
            {
                DisposeDirectX();
            }

            Debug.WriteLine("GameSurfaceControl::InitializeDirectX Px={0}x{1} Actual={2}x{3}", pixelWidth, pixelHeight, ActualWidth, ActualHeight);

            deviceManager = new DeviceManager();

            d2dTarget = new SurfaceImageSourceTarget(pixelWidth, pixelHeight);
            d2dBrush.ImageSource = d2dTarget.ImageSource;
            d2dTarget.OnRender += d2dTarget_OnRender;

            deviceManager.OnInitialize += d2dTarget.Initialize;
            deviceManager.Initialize(DisplayProperties.LogicalDpi);

            drawing = new Drawing(deviceManager);
            controller.PrepareColors(drawing);

            isDirectXReady = true;

            d2dTarget.RenderAll();
        }

        private void d2dTarget_OnRender(TargetBase target)
        {
            var context2D = target.DeviceManager.ContextDirect2D;
            context2D.BeginDraw();
            context2D.Clear(SharpDX.Color.White);
            controller.Draw(drawing);
            context2D.EndDraw();
        }

        private void AskRender()
        {
            if (isDirectXReady)
            {
                d2dTarget.RenderAll();
            }
        }

        private void DisposeDirectX()
        {
            Debug.WriteLine("GameSurfaceControl::DisposeDirectX");
            isDirectXReady = false;
            d2dBrush.ImageSource = null; // Disconnect DirectX
            if (drawing != null)
            {
                drawing.Dispose();
                drawing = null;
            }
            if (d2dTarget != null)
            {
                d2dTarget.Dispose();
                d2dTarget = null;
            }
            if (deviceManager != null)
            {
                deviceManager.Dispose();
                deviceManager = null;
            }
        }

        private static bool IsTouchOrStylus(PointerPoint point)
        {
            return (point.PointerDevice.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse);
        }

        protected override void OnRightTapped(RightTappedRoutedEventArgs e)
        {
            base.OnRightTapped(e);
            e.Handled = tapHandled; // Disable Appbar shown on right click on the game surface
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(e);
            if (controller == null)
            {
                return;
            }
            if (release != Buttons.NONE)
            {
                var point = e.GetCurrentPoint(this);
                if (point.Position == lastCursorPosition)
                {
                    Debug.WriteLine("Ignore Move: Pointer has not moved since last mouse event");
                    return;
                }
                FireDelayedIfAny();

                var move = Buttons.NONE;
                if (point.Properties.IsLeftButtonPressed && release == Buttons.LEFT_RELEASE)
                {
                    move = Buttons.LEFT_DRAG;
                }
                else if ((point.Properties.IsRightButtonPressed || (point.Properties.IsLeftButtonPressed && isLeftAsRight)) && release == Buttons.RIGHT_RELEASE)
                {
                    move = Buttons.RIGHT_DRAG;
                }
                else if ((point.Properties.IsMiddleButtonPressed || IsShiftPressed) && release == Buttons.MIDDLE_RELEASE)
                {
                    move = Buttons.MIDDLE_DRAG;
                }
                lastCursorPosition = point.Position;
                if (move != Buttons.NONE)
                {
                    controller.MouseEvent(drawing, ToGamePixels(point.Position.X), ToGamePixels(point.Position.Y), move, IsTouchOrStylus(point));
                }
            }
        }

        protected override void OnHolding(HoldingRoutedEventArgs e)
        {
            base.OnHolding(e);
            if (delayedPressed)
            {
                press = Buttons.RIGHT_BUTTON;
                release = Buttons.RIGHT_RELEASE;
                isLeftAsRight = true;
                FireDelayedIfAny();
            }
        }

        private void FireDelayedIfAny()
        {
            if (delayedPressed)
            {
                delayedPressed = false;
                if (controller.MouseEvent(drawing,
                    ToGamePixels(lastCursorPosition.X), ToGamePixels(lastCursorPosition.Y),
                    press, true))
                {
                    tapHandled = true;
                }
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (controller == null)
            {
                return;
            }
            if (release != Buttons.NONE)
            {
                FireDelayedIfAny();

                var point = e.GetCurrentPoint(this);
                lastCursorPosition = point.Position;
                if (controller.MouseEvent(drawing,
                    ToGamePixels(point.Position.X), ToGamePixels(point.Position.Y),
                    release, IsTouchOrStylus(point)))
                {
                    tapHandled = true;
                }
                release = Buttons.NONE;
            }
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            Focus(Windows.UI.Xaml.FocusState.Pointer);
            base.OnPointerPressed(e);
            tapHandled = false;
            if (controller == null)
            {
                return;
            }
            if (release != Buttons.NONE)
            {
                return;
            }
            var point = e.GetCurrentPoint(this);

            press = PressButton(point);
            release = ReleaseButton(point);
            delayedPressed = false;
            isLeftAsRight = false;
            lastCursorPosition = point.Position;

            if (IsTouchOrStylus(point) && controller.HoldToRightMouse)
            {
                delayedPressed = true;
                return;
            }

            if (controller.MouseEvent(drawing,
                ToGamePixels(point.Position.X), ToGamePixels(point.Position.Y),
                press, IsTouchOrStylus(point)))
            {
                tapHandled = true;
            }
        }

        private Buttons PressButton(PointerPoint point)
        {
            if (IsShiftPressed || point.Properties.IsMiddleButtonPressed)
            {
                return Buttons.MIDDLE_BUTTON;
            }
            if (point.Properties.IsRightButtonPressed)
            {
                return Buttons.RIGHT_BUTTON;
            }
            return Buttons.LEFT_BUTTON;
        }

        private Buttons ReleaseButton(PointerPoint point)
        {
            if (IsShiftPressed || point.Properties.IsMiddleButtonPressed)
            {
                return Buttons.MIDDLE_RELEASE;
            }
            if (point.Properties.IsRightButtonPressed)
            {
                return Buttons.RIGHT_RELEASE;
            }
            return Buttons.LEFT_RELEASE;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            rectangle = (Rectangle)GetTemplateChild("rectangle");
            rectangle.Fill = d2dBrush;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);
            if (this.FocusState == FocusState.Keyboard)
            {
                controller.IsKeyboardCursorVisible = true;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            controller.IsKeyboardCursorVisible = false;
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);
            var buttons = KeyToButtons(e.Key);
            if (buttons != Buttons.NONE)
            {
                if (IsShiftPressed)
                {
                    buttons |= Buttons.MOD_SHFT;
                }
                if (IsCtrlPressed)
                {
                    buttons |= Buttons.MOD_CTRL;
                }
                controller.MouseEvent(drawing, 0, 0, buttons, false);
                e.Handled = true;
            }
        }

        private static Buttons KeyToButtons(VirtualKey key)
        {
            switch (key)
            {
                case VirtualKey.Enter: return Buttons.CURSOR_SELECT;
                case VirtualKey.Space: return Buttons.CURSOR_SELECT2;
                case VirtualKey.Left: return Buttons.CURSOR_LEFT;
                case VirtualKey.Right: return Buttons.CURSOR_RIGHT;
                case VirtualKey.Up: return Buttons.CURSOR_UP;
                case VirtualKey.Down: return Buttons.CURSOR_DOWN;
            }
            return Buttons.NONE;
        }

        private bool IsShiftPressed
        {
            get { return (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down; }
        }

        private bool IsCtrlPressed
        {
            get { return (Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down; }
        }
    }
}
