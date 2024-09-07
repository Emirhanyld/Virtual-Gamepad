using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Point = System.Windows.Point;
using System.Windows.Forms;

namespace Virtual_Gamepad
{
    
    /// <summary>
    /// Interaction logic for Thumbstick.xaml
    /// </summary>
    public partial class Thumbstick : System.Windows.Controls.UserControl
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        private static extern bool ClipCursor(ref RECT rect);
        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr rect);

        private bool isDragging = false;
        private bool isMouseLocked = true;


        /// <summary>
        /// Current joystick position
        /// </summary>
        public Vector m_vtJoystickPos = new Vector();


        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        

        public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register(nameof(Button), typeof(string), typeof(Thumbstick));
        public static readonly DependencyProperty StickProperty = DependencyProperty.Register(nameof(Stick), typeof(string), typeof(Thumbstick));

        public MouseButtons Button
        {
            get
            {
                if ((string)GetValue(ButtonProperty) == "Left")
                {
                    return MouseButtons.Left;
                }
                else if ((string)GetValue(ButtonProperty) == "Right")
                {
                    return MouseButtons.Right;
                }
                else
                {
                    return MouseButtons.None;
                }
            }
            set { SetValue(ButtonProperty, value); }
        }
        public string Stick
        {
            get { return (string)GetValue(ButtonProperty); }
            set { SetValue(ButtonProperty, value); }
        }


        public Thumbstick()
        {
            InitializeComponent();
            ResetKnobPosition();

            //Loaded += (sender, args) =>
            //{
            //    mouseKeyHooker = Hook.GlobalEvents();
            //    mouseKeyHooker.MouseMove += HandleMouseMove;
            //    mouseKeyHooker.MouseDown += HandleMouseDown;
            //    mouseKeyHooker.MouseUp += HandleMouseUp;
            //};
        }

        private void HandleMouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            double joystickRadius = Joystick.Height * 0.5;
            double knobRadius = Knob.Height * 0.5;
            Point center = Joystick.PointToScreen(new Point(joystickRadius, joystickRadius));

            //Make coords related to the center
            var pfs = Joystick.PointFromScreen(new Point(e.X, e.Y));
            Vector vtJoystickPos = new Vector(pfs.X - joystickRadius, pfs.Y - joystickRadius);


            //Normalize coords
            vtJoystickPos /= joystickRadius;

            //Limit R [0; 1]
            if (vtJoystickPos.Length > 1.0)
                vtJoystickPos.Normalize();

            //Debug.WriteLine(e.Location);
            if (isDragging)
            {
                m_vtJoystickPos = vtJoystickPos;
                UpdateKnobPosition();

                double distance = Math.Sqrt(Math.Pow(e.X - center.X, 2) + Math.Pow(e.Y - center.Y, 2));
                if (distance > joystickRadius)
                {
                    SetCursorPos(Convert.ToInt32(10), Convert.ToInt32(10));
                }
            }

        }

        private void HandleMouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isMouseLocked && e.Button == Button)
            {
                isDragging = true;

                double joystickRadius = Joystick.Width * 0.5;
                // Move Cursor to Center of Thumbstick
                Point joystickCenter = Joystick.PointToScreen(new Point(joystickRadius, joystickRadius));
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
            else if (e.Button == MouseButtons.Middle)
            {
                isMouseLocked = !isMouseLocked;
                Joystick.Visibility = Joystick.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                Knob.Visibility = Knob.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void HandleMouseUp(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (isMouseLocked && isDragging && e.Button == Button)
            {
                isDragging = false;
                // Move Knob to Center
                ResetKnobPosition();
                // Untrap Mouse
                ClipCursor(IntPtr.Zero);
            }
        }

        /*
         * Updates Knob position
         */
        public void UpdateKnobPosition()
        {
            double fJoystickRadius = Joystick.Height * 0.5;
            double fKnobRadius = Knob.Width * 0.5;
            Canvas.SetLeft(Knob, Canvas.GetLeft(Joystick) + m_vtJoystickPos.X * fJoystickRadius + fJoystickRadius - fKnobRadius);
            Canvas.SetBottom(Knob, Canvas.GetBottom(Joystick) - m_vtJoystickPos.Y * fJoystickRadius + fJoystickRadius - fKnobRadius);
        }

        /*
         * Resets Knob position to center
         */
        public void ResetKnobPosition()
        {
            m_vtJoystickPos = new Vector(0, 0);
            UpdateKnobPosition();
        }

        /* Returns normalized Knob position as Vector
         * Top-Left is [-1, -1]
         * Middle is [0, 0]
         * Right-Bottom is [1, 1]
         */
        public Vector GetKnobPosition()
        {
            return new Vector(m_vtJoystickPos.X, m_vtJoystickPos.Y);
        }

        private void MoveCursor()
        {
            double fKnobRadius = Knob.Width * 0.5;
            int x = Convert.ToInt32(Knob.PointToScreen(new Point(0, 0)).X + fKnobRadius);
            int y = Convert.ToInt32(Knob.PointToScreen(new Point(0, 0)).Y + fKnobRadius);
            SetCursorPos(Convert.ToInt32(Knob.PointToScreen(new Point(0, 0)).X + fKnobRadius), Convert.ToInt32(Knob.PointToScreen(new Point(0, 0)).X + fKnobRadius));
        }
    }
}
