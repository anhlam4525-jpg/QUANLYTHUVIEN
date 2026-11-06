using System.ComponentModel;
using System.Drawing.Drawing2D;
using LibraryManagement.DAL;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Forms
{
    /// <summary>Màn hình đăng nhập: nửa trái giới thiệu (gradient), nửa phải form đăng nhập.</summary>
    [DesignerCategory("Code")]
    public class frmLogin : Form
    {
        private readonly AppServices _services;
        private readonly TextBox _txtUser;
        private readonly TextBox _txtPass;
        private readonly Label _lblError;
        private readonly ModernButton _btnLogin;

        public frmLogin(AppServices services)
        {
            _services = services;

            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(Theme.S(940), Theme.S(580));
            BackColor = Color.White;
            Font = Theme.Body;
            Text = "Đăng nhập - Smart Library";
            DoubleBuffered = true;
            KeyPreview = true;

            // ---------- NỬA TRÁI: giới thiệu ----------
            var hero = new Panel { Dock = DockStyle.Left, Width = Theme.S(430), BackColor = Theme.Sidebar };
            hero.Paint += PaintHero;
            NativeMethods.MakeDraggable(hero, this);

            // ---------- NỬA PHẢI: form ----------
            var right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            NativeMethods.MakeDraggable(right, this);

            int x = Theme.S(70);
            int w = Theme.S(370);

            var btnClose = new ModernButton(string.Empty, ButtonVariant.Ghost, Theme.Icons.Close)
            {
                Size = new Size(Theme.S(36), Theme.S(36)),
                Location = new Point(Theme.S(510) - Theme.S(50), Theme.S(14)),
                TabStop = false
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var lblWelcome = MakeLabel("CHÀO MỪNG TRỞ LẠI", Theme.SmallBold, Theme.Primary, x, Theme.S(92));
            var lblTitle = MakeLabel("Đăng nhập hệ thống", Theme.H1, Theme.Text, x, Theme.S(114));
            var lblSub = MakeLabel("Sử dụng tài khoản thủ thư để quản lý thư viện", Theme.Body, Theme.TextMuted, x, Theme.S(160));

            var lblUser = MakeLabel("Tên đăng nhập", Theme.SmallBold, Theme.Text, x, Theme.S(214));
            var fUser = Fields.Text(out _txtUser, "Nhập tên đăng nhập", Theme.Icons.Contact);
            fUser.SetBounds(x, Theme.S(238), w, Theme.S(46));

            var lblPass = MakeLabel("Mật khẩu", Theme.SmallBold, Theme.Text, x, Theme.S(300));
            var fPass = Fields.Text(out _txtPass, "Nhập mật khẩu", Theme.Icons.Lock, password: true);
            fPass.SetBounds(x, Theme.S(324), w, Theme.S(46));

            var chkShow = new CheckBox
            {
                Text = "Hiện mật khẩu",
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Location = new Point(x, Theme.S(382)),
                Cursor = Cursors.Hand,
                BackColor = Color.White
            };
            chkShow.CheckedChanged += (s, e) => _txtPass.UseSystemPasswordChar = !chkShow.Checked;

            _lblError = new Label
            {
                Font = Theme.SmallBold,
                ForeColor = Theme.DangerText,
                AutoSize = false,
                Location = new Point(x, Theme.S(410)),
                Size = new Size(w, Theme.S(22)),
                BackColor = Color.White
            };

            _btnLogin = new ModernButton("Đăng nhập", ButtonVariant.Primary, Theme.Icons.Forward)
            {
                Location = new Point(x, Theme.S(436)),
                Size = new Size(w, Theme.S(48)),
                Radius = Theme.Sf(10)
            };
            _btnLogin.Click += (s, e) => DoLogin();
            AcceptButton = _btnLogin;

            var lblDemo = MakeLabel("Tài khoản demo:  admin / admin123   ·   thuthu / 123456",
                Theme.Small, Theme.TextMuted, x, Theme.S(500));
            var lblDb = MakeLabel("CSDL: " + DbHelper.DescribeConnection(), Theme.Small, Theme.TextSubtle, x, Theme.S(540));

            right.Controls.AddRange(new Control[]
            {
                btnClose, lblWelcome, lblTitle, lblSub, lblUser, fUser, lblPass, fPass, chkShow,
                _lblError, _btnLogin, lblDemo, lblDb
            });

            Controls.Add(right);
            Controls.Add(hero);
            right.BringToFront();

            _txtUser.TextChanged += (s, e) => _lblError.Text = string.Empty;
            _txtPass.TextChanged += (s, e) => _lblError.Text = string.Empty;
            Shown += (s, e) => _txtUser.Focus();
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            base.OnKeyDown(e);
        }

        private void DoLogin()
        {
            _btnLogin.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                var result = _services.Auth.Login(_txtUser.Text, _txtPass.Text);
                if (result.Success)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                _lblError.Text = result.Message;
                _txtPass.SelectAll();
                _txtPass.Focus();
                if (_services.Auth.IsLocked) _btnLogin.Enabled = false;
            }
            finally
            {
                Cursor = Cursors.Default;
                if (!_services.Auth.IsLocked && !IsDisposed) _btnLogin.Enabled = true;
            }
        }

        private static Label MakeLabel(string text, Font font, Color color, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                AutoSize = true,
                Location = new Point(x, y),
                BackColor = Color.White
            };
        }

        private void PaintHero(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p || p.Width <= 0 || p.Height <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bg = new LinearGradientBrush(p.ClientRectangle, Theme.Primary, Theme.Sidebar, 60f))
            {
                g.FillRectangle(bg, p.ClientRectangle);
            }
            using (var c1 = new SolidBrush(Color.FromArgb(24, Color.White)))
            using (var c2 = new SolidBrush(Color.FromArgb(14, Color.White)))
            {
                g.FillEllipse(c1, -Theme.S(120), -Theme.S(140), Theme.S(360), Theme.S(360));
                g.FillEllipse(c2, p.Width - Theme.S(180), p.Height - Theme.S(220), Theme.S(320), Theme.S(320));
                g.FillEllipse(c2, p.Width - Theme.S(120), Theme.S(150), Theme.S(90), Theme.S(90));
            }

            int x = Theme.S(48);
            // Logo
            var logo = new Rectangle(x, Theme.S(48), Theme.S(52), Theme.S(52));
            Theme.FillRound(g, Color.FromArgb(40, Color.White), logo, Theme.Sf(14));
            Theme.DrawIcon(g, Theme.Icons.Library, 20f, Color.White, logo);
            Theme.DrawText(g, "Smart Library", Theme.H3, new Rectangle(logo.Right + Theme.S(14), Theme.S(50), Theme.S(260), Theme.S(26)), Color.White);
            Theme.DrawText(g, "Library Management System", Theme.Small,
                new Rectangle(logo.Right + Theme.S(14), Theme.S(76), Theme.S(260), Theme.S(20)), Color.FromArgb(199, 210, 254));

            using (var big = new Font(Theme.FontFamilySemibold, 24f))
            {
                TextRenderer.DrawText(g, "Quản lý thư viện\nthông minh & hiện đại", big,
                    new Rectangle(x, Theme.S(170), p.Width - x * 2, Theme.S(110)), Color.White,
                    TextFormatFlags.Left | TextFormatFlags.WordBreak);
            }
            TextRenderer.DrawText(g,
                "Theo dõi kho sách, độc giả và phiếu mượn trả trên một màn hình duy nhất.",
                Theme.Body, new Rectangle(x, Theme.S(290), p.Width - x * 2, Theme.S(48)),
                Color.FromArgb(224, 231, 255), TextFormatFlags.Left | TextFormatFlags.WordBreak);

            string[] features =
            {
                "Quản lý sách & độc giả: thêm, sửa, xóa, tìm kiếm",
                "Mượn – trả sách, cảnh báo phiếu quá hạn",
                "Thống kê trực quan bằng biểu đồ"
            };
            int y = Theme.S(360);
            foreach (string f in features)
            {
                var dot = new Rectangle(x, y, Theme.S(24), Theme.S(24));
                Theme.FillRound(g, Color.FromArgb(46, Color.White), dot, Theme.Sf(12));
                Theme.DrawIcon(g, Theme.Icons.Check, 8f, Color.White, dot);
                Theme.DrawText(g, f, Theme.Body, new Rectangle(dot.Right + Theme.S(12), y, p.Width - dot.Right - Theme.S(40), Theme.S(24)), Color.White);
                y += Theme.S(38);
            }

            Theme.DrawText(g, "© 2026 · Đồ án Công cụ & Môi trường phát triển phần mềm", Theme.Small,
                new Rectangle(x, p.Height - Theme.S(44), p.Width - x, Theme.S(20)), Color.FromArgb(165, 180, 252));
        }
    }
}
