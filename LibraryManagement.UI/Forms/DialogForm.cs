using System.ComponentModel;
using System.Runtime.InteropServices;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Forms
{
    /// <summary>Các hàm Win32: kéo cửa sổ không viền, bo góc Windows 11, mở Console.</summary>
    internal static class NativeMethods
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        public const int CS_DROPSHADOW = 0x20000;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        /// <summary>Cho phép kéo form không viền bằng cách giữ chuột trên một control.</summary>
        public static void MakeDraggable(Control handle, Form form)
        {
            handle.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(form.Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            };
        }

        /// <summary>Bo góc cửa sổ trên Windows 11 (Windows 10 bỏ qua, không lỗi).</summary>
        public static void RoundCorners(Form form)
        {
            try
            {
                int preference = 2; // DWMWCP_ROUND
                DwmSetWindowAttribute(form.Handle, 33, ref preference, sizeof(int));
            }
            catch
            {
                // Không hỗ trợ → giữ góc vuông
            }
        }
    }

    /// <summary>
    /// Form hộp thoại không viền kiểu web: Header (icon + tiêu đề + nút đóng) – Body – Footer (các nút).
    /// </summary>
    [DesignerCategory("Code")]
    public class DialogForm : Form
    {
        protected readonly Panel HeaderPanel;
        protected readonly Panel BodyPanel;
        protected readonly FlowLayoutPanel FooterButtons;
        private readonly Panel _footer;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private string _glyph = Theme.Icons.Info;
        private Color _accent = Theme.Primary;
        private Color _accentSoft = Theme.PrimarySoft;

        public DialogForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.White;
            Font = Theme.Body;
            KeyPreview = true;
            DoubleBuffered = true;
            Padding = new Padding(1);

            // ----- Header -----
            HeaderPanel = new Panel { Dock = DockStyle.Top, Height = Theme.S(76), BackColor = Color.White };
            HeaderPanel.Paint += PaintHeader;

            _lblTitle = new Label
            {
                AutoSize = true,
                Font = Theme.H3,
                ForeColor = Theme.Text,
                Location = new Point(Theme.S(80), Theme.S(18)),
                BackColor = Color.White
            };
            _lblSubtitle = new Label
            {
                AutoSize = true,
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                Location = new Point(Theme.S(80), Theme.S(42)),
                BackColor = Color.White
            };
            var btnClose = new ModernButton(string.Empty, ButtonVariant.Ghost, Theme.Icons.Close)
            {
                Size = new Size(Theme.S(36), Theme.S(36)),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            HeaderPanel.Controls.AddRange(new Control[] { _lblTitle, _lblSubtitle, btnClose });
            HeaderPanel.Resize += (s, e) => btnClose.Location = new Point(HeaderPanel.Width - btnClose.Width - Theme.S(14), Theme.S(18));
            NativeMethods.MakeDraggable(HeaderPanel, this);
            NativeMethods.MakeDraggable(_lblTitle, this);
            NativeMethods.MakeDraggable(_lblSubtitle, this);

            // ----- Body -----
            BodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(Theme.S(28), Theme.S(8), Theme.S(28), Theme.S(16))
            };

			// ----- Footer -----
			_footer = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(72), BackColor = Theme.SurfaceAlt };
			_footer.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, 0, _footer.Width, 0);
            };
            FooterButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                BackColor = Theme.SurfaceAlt,
                Padding = new Padding(0, Theme.S(17), Theme.S(20), 0)
            };
            _footer.Controls.Add(FooterButtons);

            Controls.Add(BodyPanel);
            Controls.Add(_footer);
            Controls.Add(HeaderPanel);
            BodyPanel.BringToFront();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= NativeMethods.CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.RoundCorners(this);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Theme.BorderStrong);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected void SetHeader(string title, string subtitle, string glyph, Color accent, Color accentSoft)
        {
            Text = title;
            _lblTitle.Text = title;
            _lblSubtitle.Text = subtitle;
            _glyph = glyph;
            _accent = accent;
            _accentSoft = accentSoft;
            HeaderPanel.Invalidate();
        }

        /// <summary>Thêm nút vào footer (thêm theo thứ tự từ phải sang trái).</summary>
        protected ModernButton AddFooterButton(string text, ButtonVariant variant, string? glyph = null)
        {
            var btn = new ModernButton(text, variant, glyph);
            btn.AutoFit(Theme.S(100));
            btn.Margin = new Padding(Theme.S(10), 0, 0, 0);
            FooterButtons.Controls.Add(btn);
            return btn;
        }

        private void PaintHeader(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var icon = new Rectangle(Theme.S(24), Theme.S(18), Theme.S(42), Theme.S(42));
            Theme.FillRound(g, _accentSoft, icon, Theme.Sf(12));
            Theme.DrawIcon(g, _glyph, 14f, _accent, icon);
        }
    }
}
