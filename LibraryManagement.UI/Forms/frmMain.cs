using System.ComponentModel;
using System.Globalization;
using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Common;
using LibraryManagement.DAL;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Logging;
using LibraryManagement.UI.Pages;

namespace LibraryManagement.UI.Forms
{
    /// <summary>
    /// Khung chính kiểu web-dashboard: Sidebar (menu) bên trái – Header (tiêu đề, đồng hồ, trạng thái CSDL) – Nội dung.
    /// Mỗi chức năng là một UserControl (Pages/*) được hoán đổi trong vùng nội dung.
    /// </summary>
    [DesignerCategory("Code")]
    public class frmMain : Form
    {
        private readonly AppServices _services;
        private readonly ILogger _logger;
        private readonly Dictionary<string, PageBase> _pages = new();
        private readonly Dictionary<string, SidebarButton> _nav = new();
        private readonly Panel _content;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private readonly Label _lblClock;
        private readonly StatusPill _dbPill;
        private readonly System.Windows.Forms.Timer _clock;
        private string _currentKey = string.Empty;

        public bool LogoutRequested { get; private set; }

        public frmMain(AppServices services)
        {
            _services = services;
            _logger = AppLogging.CreateLogger("LibraryManagement.UI.frmMain");

            SuspendLayout();
            AutoScaleMode = AutoScaleMode.None;
            Text = "Smart Library – Hệ thống quản lý thư viện";
            Font = Theme.Body;
            BackColor = Theme.Background;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(Theme.S(1180), Theme.S(720));
            Size = new Size(Theme.S(1400), Theme.S(860));
            DoubleBuffered = true;

            // ================= SIDEBAR =================
            var sidebar = BuildSidebar();

            // ================= KHU VỰC BÊN PHẢI =================
            var rightArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };

            // ----- Header -----
            var header = new Panel { Dock = DockStyle.Top, Height = Theme.S(76), BackColor = Color.White };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            _lblTitle = new Label
            {
                AutoSize = true,
                Font = Theme.H2,
                ForeColor = Theme.Text,
                Location = new Point(Theme.S(28), Theme.S(12)),
                BackColor = Color.White
            };
            _lblSubtitle = new Label
            {
                AutoSize = true,
                Font = Theme.Small,
                ForeColor = Theme.TextMuted,
                Location = new Point(Theme.S(30), Theme.S(44)),
                BackColor = Color.White
            };

            var headerRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0, Theme.S(20), Theme.S(24), 0)
            };

            _lblClock = new Label
            {
                AutoSize = true,
                Font = Theme.BodyBold,
                ForeColor = Theme.TextMuted,
                Margin = new Padding(0, Theme.S(7), Theme.S(16), 0),
                BackColor = Color.White
            };
            _dbPill = new StatusPill { Margin = new Padding(0, Theme.S(2), Theme.S(16), 0) };
            _dbPill.PillText = DbHelper.DescribeConnection();

            var avatar = new Avatar(Theme.S(36))
            {
                FullName = AppSession.CurrentUser?.FullName ?? "Admin",
                FillColor = Theme.Primary,
                Margin = new Padding(0, 0, 0, 0)
            };

            headerRight.Controls.AddRange(new Control[] { _lblClock, _dbPill, avatar });
            header.Controls.Add(_lblTitle);
            header.Controls.Add(_lblSubtitle);
            header.Controls.Add(headerRight);

            // ----- Content -----
            _content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Padding = new Padding(Theme.S(24), Theme.S(20), Theme.S(24), Theme.S(20))
            };

            rightArea.Controls.Add(_content);
            rightArea.Controls.Add(header);
            _content.BringToFront();

            Controls.Add(rightArea);
            Controls.Add(sidebar);
            rightArea.BringToFront();
            ResumeLayout(true);

            // ----- Đồng hồ -----
            _clock = new System.Windows.Forms.Timer { Interval = 1000 };
            _clock.Tick += (s, e) => UpdateClock();
            _clock.Start();
            UpdateClock();

            // ----- Trang -----
            RegisterPage("dashboard", new DashboardPage(services));
            RegisterPage("books", new BooksPage(services));
            RegisterPage("readers", new ReadersPage(services));
            RegisterPage("borrow", new BorrowPage(services));
            RegisterPage("stats", new StatisticsPage(services));

            Shown += (s, e) =>
            {
                NavigateTo("dashboard", null);
                RefreshBadges();
            };
            FormClosed += (s, e) => _clock.Stop();

            _logger.LogInformation(UiLogEvents.AppStarted, "Mở màn hình chính cho {Username}", AppSession.Username);
        }

        private Panel BuildSidebar()
        {
            var sidebar = new Panel { Dock = DockStyle.Left, Width = Theme.S(256), BackColor = Theme.Sidebar };
            sidebar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var logo = new Rectangle(Theme.S(22), Theme.S(22), Theme.S(42), Theme.S(42));
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(logo, Theme.Primary, Theme.Violet, 45f))
                using (var path = Theme.RoundRect(logo, Theme.Sf(12)))
                {
                    g.FillPath(brush, path);
                }
                Theme.DrawIcon(g, Theme.Icons.Library, 15f, Color.White, logo);
                Theme.DrawText(g, "Smart Library", Theme.H3,
                    new Rectangle(logo.Right + Theme.S(12), Theme.S(22), Theme.S(170), Theme.S(24)), Color.White);
                Theme.DrawText(g, "Quản lý thư viện", Theme.Small,
                    new Rectangle(logo.Right + Theme.S(12), Theme.S(46), Theme.S(170), Theme.S(18)), Theme.SidebarText);
                Theme.DrawText(g, "MENU CHÍNH", Theme.Tiny,
                    new Rectangle(Theme.S(26), Theme.S(96), Theme.S(200), Theme.S(18)), Theme.SidebarMuted);
            };

            var items = new (string Key, string Text, string Glyph)[]
            {
                ("dashboard", "Tổng quan", Theme.Icons.Home),
                ("books", "Quản lý sách", Theme.Icons.Library),
                ("readers", "Quản lý độc giả", Theme.Icons.People),
                ("borrow", "Mượn / Trả sách", Theme.Icons.Switch),
                ("stats", "Thống kê", Theme.Icons.Chart)
            };

            int y = Theme.S(120);
            foreach (var item in items)
            {
                var btn = new SidebarButton(item.Text, item.Glyph)
                {
                    Location = new Point(0, y),
                    Width = Theme.S(256),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                string key = item.Key;
                btn.Click += (s, e) => NavigateTo(key, null);
                sidebar.Controls.Add(btn);
                _nav[key] = btn;
                y += Theme.S(48);
            }

            // ----- Thẻ người dùng + Đăng xuất (dưới cùng) -----
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(132), BackColor = Theme.Sidebar };
            bottom.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.SidebarHover);
                e.Graphics.DrawLine(pen, Theme.S(20), 0, bottom.Width - Theme.S(20), 0);
            };

            var user = AppSession.CurrentUser;
            var av = new Avatar(Theme.S(40))
            {
                FullName = user?.FullName ?? "Admin",
                Location = new Point(Theme.S(22), Theme.S(18))
            };
            var lblName = new Label
            {
                Text = user?.FullName ?? "Quản trị viên",
                Font = Theme.BodyBold,
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(Theme.S(170), Theme.S(22)),
                Location = new Point(Theme.S(72), Theme.S(18)),
                BackColor = Theme.Sidebar,
                AutoEllipsis = true
            };
            var lblRole = new Label
            {
                Text = (user?.Role ?? "Quản trị") + " · @" + (user?.Username ?? "admin"),
                Font = Theme.Small,
                ForeColor = Theme.SidebarText,
                AutoSize = false,
                Size = new Size(Theme.S(170), Theme.S(18)),
                Location = new Point(Theme.S(72), Theme.S(40)),
                BackColor = Theme.Sidebar,
                AutoEllipsis = true
            };
            var btnLogout = new SidebarButton("Đăng xuất", Theme.Icons.Power)
            {
                Location = new Point(0, Theme.S(74)),
                Width = Theme.S(256),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            btnLogout.Click += (s, e) =>
            {
                if (frmConfirm.Ask(this, "Đăng xuất", "Bạn có muốn đăng xuất khỏi hệ thống?", "Đăng xuất"))
                {
                    LogoutRequested = true;
                    Close();
                }
            };
            bottom.Controls.AddRange(new Control[] { av, lblName, lblRole, btnLogout });
            sidebar.Controls.Add(bottom);

            return sidebar;
        }

        private void RegisterPage(string key, PageBase page)
        {
            page.Navigate = NavigateTo;
            page.DataChanged += (s, e) => RefreshBadges();
            page.Visible = false;
            _pages[key] = page;
            _content.Controls.Add(page);
        }

        private void NavigateTo(string key, object? parameter)
        {
            if (!_pages.TryGetValue(key, out var page)) return;

            if (_currentKey != key)
            {
                _logger.LogInformation(UiLogEvents.Navigate, "Chuyển trang {From} → {To}",
                    string.IsNullOrEmpty(_currentKey) ? "(start)" : _currentKey, key);
            }

            foreach (var kv in _nav) kv.Value.Active = kv.Key == key;

            SuspendLayout();
            foreach (var p in _pages.Values)
            {
                if (p != page) p.Visible = false;
            }
            page.Visible = true;
            page.BringToFront();
            ResumeLayout(true);

            _lblTitle.Text = page.Title;
            _lblSubtitle.Text = page.Subtitle;
            _currentKey = key;

            Cursor = Cursors.WaitCursor;
            try
            {
                page.OnNavigatedTo(parameter);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>Cập nhật huy hiệu số phiếu quá hạn trên menu Mượn/Trả + trạng thái CSDL.</summary>
        private void RefreshBadges()
        {
            try
            {
                var stats = _services.Statistics.GetDashboardStats();
                _nav["borrow"].Badge = stats.OverdueTickets > 0 ? stats.OverdueTickets.ToString() : null;
                _dbPill.Ok = true;
                _dbPill.PillText = DbHelper.DescribeConnection();
            }
            catch (Exception ex)
            {
                _logger.LogError(UiLogEvents.UiError, ex, "Không cập nhật được huy hiệu menu");
                _dbPill.Ok = false;
                _dbPill.PillText = "Mất kết nối CSDL";
            }
        }

        private void UpdateClock()
        {
            var vi = new CultureInfo("vi-VN");
            string day = vi.TextInfo.ToTitleCase(DateTime.Now.ToString("dddd", vi));
            _lblClock.Text = $"{day}, {DateTime.Now:dd/MM/yyyy  ·  HH:mm:ss}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _clock.Dispose();
            base.Dispose(disposing);
        }
    }
}
