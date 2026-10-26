using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WP_Inventario
{
    public enum ToastType
    {
        Success,
        Error,
        Warning,
        Info
    }

    public class ToastNotification : Form
    {
        private Timer _timerFadeIn;
        private Timer _timerDisplay;
        private Timer _timerFadeOut;
        private Label _lblIcon;
        private Label _lblMessage;
        private int _targetY;
        private int _startY;
        private double _slideProgress;
        private int _toastWidth = 380;
        private int _toastHeight = 60;
        private int _displayTime = 2500;
        private bool _isCenter;
        private const int MARGIN_BOTTOM = 30;
        private const int MARGIN_RIGHT = 30;
        private const int ANIMATION_INTERVAL = 12;

        private static int _activeToastCount = 0;

        private Action _onClick;

        public ToastNotification(string message, ToastType type, bool isCenter = false, Action onClick = null)
        {
            _onClick = onClick;
            _isCenter = isCenter;
            if (isCenter)
            {
                _toastWidth = 500;
                _toastHeight = 180;
                _displayTime = 4000; // Leave it on screen a bit longer
            }

            SetupForm();
            SetupControls(message, type);
            SetupAnimations();
        }

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(_toastWidth, _toastHeight);
            this.Opacity = 0;
            this.BackColor = Color.FromArgb(20, 20, 20); // will be drawn over
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);

            var screen = Screen.PrimaryScreen.WorkingArea;
            
            if (_isCenter)
            {
                _targetY = (screen.Height / 2) - (_toastHeight / 2);
                _startY = _targetY + 40;
                this.Location = new Point((screen.Width / 2) - (_toastWidth / 2), _startY);
            }
            else
            {
                // Position: bottom-right of primary screen
                int offsetY = _activeToastCount * (_toastHeight + 10);
                _targetY = screen.Bottom - _toastHeight - MARGIN_BOTTOM - offsetY;
                _startY = _targetY + 40; // start 40px below target (slide up)
                this.Location = new Point(screen.Right - _toastWidth - MARGIN_RIGHT, _startY);
            }
        }

        private void SetupControls(string message, ToastType type)
        {
            Color accentColor;
            string icon;

            switch (type)
            {
                case ToastType.Success:
                    accentColor = Color.FromArgb(39, 174, 96);
                    icon = "✓";
                    break;
                case ToastType.Error:
                    accentColor = Color.FromArgb(192, 57, 43);
                    icon = "✕";
                    break;
                case ToastType.Warning:
                    accentColor = Color.FromArgb(243, 156, 18);
                    icon = "⚠";
                    break;
                default:
                    accentColor = Color.FromArgb(52, 152, 219);
                    icon = "ℹ";
                    break;
            }

            // Icon label (left side colored circle)
            _lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", _isCenter ? 24f : 16f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size = new Size(_isCenter ? 80 : 50, _toastHeight),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Message label
            _lblMessage = new Label
            {
                Text = message,
                Font = new Font("Bahnschrift Light", _isCenter ? 12f : 11f),
                ForeColor = Color.FromArgb(230, 230, 230),
                BackColor = Color.Transparent,
                Size = new Size(_toastWidth - (_isCenter ? 90 : 60), _toastHeight),
                Location = new Point(_isCenter ? 80 : 50, 0),
                TextAlign = _isCenter ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                AutoEllipsis = !_isCenter
            };

            this.Controls.Add(_lblIcon);
            this.Controls.Add(_lblMessage);

            // Clic on components triggers the callback
            this.Cursor = _onClick != null ? Cursors.Hand : Cursors.Default;
            _lblIcon.Cursor = this.Cursor;
            _lblMessage.Cursor = this.Cursor;

            EventHandler clickAction = (s, e) =>
            {
                if (_onClick != null)
                {
                    _onClick.Invoke();
                    this.Close();
                }
            };

            this.Click += clickAction;
            _lblIcon.Click += clickAction;
            _lblMessage.Click += clickAction;

            // Store accent color for painting
            this.Tag = accentColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background with rounded corners
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var path = CreateRoundedRect(rect, 10))
            {
                using (var bgBrush = new SolidBrush(Color.FromArgb(30, 35, 32)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Accent bar on the left
                var accentColor = (Color)(this.Tag ?? Color.Gray);
                var accentRect = new Rectangle(0, 0, 5, this.Height);
                using (var accentPath = CreateRoundedRect(accentRect, 3))
                using (var accentBrush = new SolidBrush(accentColor))
                {
                    g.FillPath(accentBrush, accentPath);
                }

                // Subtle border
                using (var borderPen = new Pen(Color.FromArgb(50, 60, 55), 1f))
                {
                    g.DrawPath(borderPen, path);
                }
            }
        }

        private GraphicsPath CreateRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // WS_EX_TOOLWINDOW: hide from Alt+Tab
                cp.ExStyle |= 0x00000080;
                // NOTE: WS_EX_TRANSPARENT was intentionally removed.
                // That flag prevented Windows from painting this window on its own,
                // which combined with Opacity=0 at startup caused the toast to never appear.
                return cp;
            }
        }

        private void SetupAnimations()
        {
            _slideProgress = 0;

            // Slide + Fade-in
            _timerFadeIn = new Timer { Interval = ANIMATION_INTERVAL };
            _timerFadeIn.Tick += (s, e) =>
            {
                _slideProgress += 0.06;
                if (_slideProgress >= 1.0)
                {
                    _slideProgress = 1.0;
                    this.Opacity = 1.0;
                    int finalY = _targetY;
                    this.Location = new Point(this.Location.X, finalY);
                    _timerFadeIn.Stop();
                    _timerDisplay.Start();
                    return;
                }

                // Ease-out cubic for smooth deceleration
                double eased = 1 - Math.Pow(1 - _slideProgress, 3);
                this.Opacity = Math.Min(1.0, _slideProgress * 1.5);
                int currentY = _startY + (int)((_targetY - _startY) * eased);
                this.Location = new Point(this.Location.X, currentY);
            };

            // Display timer
            _timerDisplay = new Timer { Interval = _displayTime };
            _timerDisplay.Tick += (s, e) =>
            {
                _timerDisplay.Stop();
                _timerFadeOut.Start();
            };

            // Fade-out
            _timerFadeOut = new Timer { Interval = ANIMATION_INTERVAL };
            _timerFadeOut.Tick += (s, e) =>
            {
                if (this.Opacity > 0.05)
                {
                    this.Opacity -= 0.06;
                }
                else
                {
                    _timerFadeOut.Stop();
                    _activeToastCount--;
                    this.Close();
                }
            };
        }

        public void ShowToast(IWin32Window owner = null)
        {
            _activeToastCount++;
            if (owner != null)
                this.Show(owner);
            else
                this.Show();
            _timerFadeIn.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timerFadeIn?.Dispose();
            _timerDisplay?.Dispose();
            _timerFadeOut?.Dispose();
            base.OnFormClosed(e);
        }

        // ======= STATIC HELPERS (call these from anywhere) =======

        /// <summary>
        /// Shows a success toast. Use instead of MessageBox for confirmations.
        /// </summary>
        public static void ShowSuccess(string message, Action onClick = null)
        {
            var owner = GetActiveOwner();
            var toast = new ToastNotification(message, ToastType.Success, false, onClick);
            toast.ShowToast(owner);
        }

        /// <summary>
        /// Shows an error toast. Use instead of MessageBox for errors.
        /// </summary>
        public static void ShowError(string message, Action onClick = null)
        {
            var owner = GetActiveOwner();
            var toast = new ToastNotification(message, ToastType.Error, false, onClick);
            toast.ShowToast(owner);
        }

        /// <summary>
        /// Shows a warning toast.
        /// </summary>
        public static void ShowWarning(string message, Action onClick = null)
        {
            var owner = GetActiveOwner();
            var toast = new ToastNotification(message, ToastType.Warning, false, onClick);
            toast.ShowToast(owner);
        }

        /// <summary>
        /// Shows an info toast.
        /// </summary>
        public static void ShowInfo(string message, Action onClick = null)
        {
            var owner = GetActiveOwner();
            var toast = new ToastNotification(message, ToastType.Info, false, onClick);
            toast.ShowToast(owner);
        }

        public static void ShowCenterToast(string title, string details, ToastType type, Action onClick = null)
        {
            var owner = GetActiveOwner();
            // Formatear el texto para que se muestre en varias líneas
            string message = $"{title}\n\n{details}";
            var toast = new ToastNotification(message, type, true, onClick);
            toast.ShowToast(owner);
        }

        private static IWin32Window GetActiveOwner()
        {
            // Prefer the currently active form; fall back to the first open form
            Form active = Form.ActiveForm;
            if (active != null) return active;
            if (Application.OpenForms.Count > 0) return Application.OpenForms[0];
            return null;
        }
    }
}
