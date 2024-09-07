using Nefarius.ViGEm.Client.Targets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using Gma.System.MouseKeyHook;
using System.Windows.Forms;

namespace Virtual_Gamepad
{
    /// <summary>
    /// Interaction logic for GamepadWindow.xaml
    /// </summary>
    public partial class GamepadWindow : Window
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT rect);
        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr rect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private bool isDragging = false;
        private bool isRunning = true;
        private Keys pauseButton = Keys.F10;

        // ViGEm controller
        private IXbox360Controller xboxController;
        // MouseKeyHooker
        private IKeyboardMouseEvents? mouseKeyHooker;

        public GamepadWindow(IXbox360Controller xbox360Controller)
        {
            InitializeComponent();
            // Make window fullscreen
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight - 1;

            xboxController = xbox360Controller;

            Loaded += (sender, args) =>
            {
                mouseKeyHooker = Hook.GlobalEvents();
                mouseKeyHooker.MouseMove += HandleMouseMove;
                mouseKeyHooker.MouseDown += HandleMouseDown;
                mouseKeyHooker.MouseUp += HandleMouseUp;
                mouseKeyHooker.KeyDown += HandleKeyDown;
                mouseKeyHooker.KeyUp += HandleKeyUp;
            };

            Loaded += (sender, args) =>
            {
                [DllImport("user32.dll")]
                static extern int GetWindowLong(IntPtr hWnd, int nIndex);

                [DllImport("user32.dll")]
                static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

                const int GWL_EXSTYLE = -20;
                const int WS_EX_TOOLWINDOW = 0x00000080;

                var hwnd = new WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
            };
        }

        private void HandleKeyUp(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == pauseButton)
            {
                isRunning = !isRunning;
                Visibility = Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void HandleKeyDown(object? sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == pauseButton)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void HandleMouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!isRunning || !isDragging)
                return;

            Thumbstick stick;
            if (System.Windows.Forms.Control.MouseButtons == LeftStick.Button) { stick = LeftStick; }
            else if (System.Windows.Forms.Control.MouseButtons == RightStick.Button) { stick = RightStick; }
            else { return; }

            double joystickRadius = stick.Joystick.Height * 0.5;
            double knobRadius = stick.Knob.Height * 0.5;
            Point center = stick.Joystick.PointToScreen(new Point(joystickRadius, joystickRadius));

            // Make coords related to the center
            var pfs = stick.Joystick.PointFromScreen(new Point(e.X, e.Y));
            Vector vtJoystickPos = new Vector(pfs.X - joystickRadius, pfs.Y - joystickRadius);

            //Normalize coords
            vtJoystickPos /= joystickRadius;

            //Limit R [0; 1]
            if (vtJoystickPos.Length > 1.0)
                vtJoystickPos.Normalize();


            stick.m_vtJoystickPos = vtJoystickPos;
            stick.UpdateKnobPosition();

            Vector knobPos = stick.GetKnobPosition();
            if (stick.Stick == "Left")
            {
                xboxController.SetAxisValue(Xbox360Axis.LeftThumbX, (short)(knobPos.X * 32767));
                xboxController.SetAxisValue(Xbox360Axis.LeftThumbY, (short)(knobPos.Y * -32767));
            }
            else if (stick.Stick == "Right")
            {
                xboxController.SetAxisValue(Xbox360Axis.RightThumbX, (short)(knobPos.X * 32767));
                xboxController.SetAxisValue(Xbox360Axis.RightThumbY, (short)(knobPos.Y * -32767));
            }
            xboxController.SubmitReport();
        }

        private void HandleMouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            Thumbstick stick;
            if (e.Button == LeftStick.Button) { stick = LeftStick; }
            else if (e.Button == RightStick.Button) { stick = RightStick; }
            else { return; }

            if (!isRunning)
                return;

            isDragging = true;



            double joystickRadius = stick.Joystick.Width * 0.5;
            // Move Cursor to Center of Thumbstick
            Point joystickCenter = stick.Joystick.PointToScreen(new Point(joystickRadius, joystickRadius));
            SetCursorPos(Convert.ToInt32(joystickCenter.X), Convert.ToInt32(joystickCenter.Y));

            RECT r = new()
            {
                Left = (int)(joystickCenter.X - joystickRadius * 1.25),
                Top = (int)(joystickCenter.Y - joystickRadius * 1.25),
                Right = (int)(joystickCenter.X + joystickRadius * 1.25),
                Bottom = (int)(joystickCenter.Y + joystickRadius * 1.25)
            };

            // Trap Mouse
            ClipCursor(ref r);
        }

        private void HandleMouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            Thumbstick stick;
            if (e.Button == LeftStick.Button) { stick = LeftStick; }
            else if (e.Button == RightStick.Button) { stick = RightStick; }
            else { return; }

            if (isRunning && isDragging)
            {
                isDragging = false;
                // Move Knob to Center
                stick.ResetKnobPosition();
                // Untrap Mouse
                ClipCursor(IntPtr.Zero);

                if (stick.Stick == "Left")
                {
                    xboxController.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
                    xboxController.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
                }
                else if (stick.Stick == "Right")
                {
                    xboxController.SetAxisValue(Xbox360Axis.RightThumbX, 0);
                    xboxController.SetAxisValue(Xbox360Axis.RightThumbY, 0);
                }
                xboxController.SubmitReport();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mouseKeyHooker?.Dispose();
            xboxController?.Disconnect();

            base.OnClosed(e);
        }

        public new void Close()
        {
            mouseKeyHooker?.Dispose();
            xboxController?.Disconnect();

            base.Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            setWindowExTransparent(hwnd);
        }

        private static void setWindowExTransparent(IntPtr hwnd)
        {
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int GWL_EXSTYLE = (-20);

            [DllImport("user32.dll")]
            static extern int GetWindowLong(IntPtr hwnd, int index);

            [DllImport("user32.dll")]
            static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
    }
}
