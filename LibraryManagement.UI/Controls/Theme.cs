using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    /// <summary>
    /// Bảng màu, font chữ, icon và các hàm vẽ dùng chung cho toàn bộ giao diện.
    /// 3 màu chủ đạo: Xanh than (Sidebar) – Chàm (Primary) – Trắng/Xám nhạt (nền).
    /// </summary>
    public static class Theme
    {
        // ---------- DPI ----------
        /// <summary>Hệ số phóng to theo DPI của màn hình (100% = 1.0, 125% = 1.25 ...).</summary>
        public static float Scale { get; private set; } = 1f;

        public static void InitDpi()
        {
            using var g = Graphics.FromHwnd(IntPtr.Zero);
            Scale = Math.Max(1f, g.DpiX / 96f);
        }

        /// <summary>Đổi kích thước thiết kế (px ở 96 DPI) sang px thực tế.</summary>
        public static int S(int px) => (int)Math.Round(px * Scale);
        public static float Sf(float px) => px * Scale;

        // ---------- MÀU ----------
        public static readonly Color Sidebar = Color.FromArgb(15, 23, 42);
        public static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);
        public static readonly Color SidebarText = Color.FromArgb(148, 163, 184);
        public static readonly Color SidebarMuted = Color.FromArgb(71, 85, 105);

        public static readonly Color Primary = Color.FromArgb(79, 70, 229);
        public static readonly Color PrimaryHover = Color.FromArgb(67, 56, 202);
        public static readonly Color PrimaryPressed = Color.FromArgb(55, 48, 163);
        public static readonly Color PrimarySoft = Color.FromArgb(238, 242, 255);
        public static readonly Color PrimarySoftText = Color.FromArgb(67, 56, 202);
        public static readonly Color PrimaryLight = Color.FromArgb(199, 210, 254);
        public static readonly Color Violet = Color.FromArgb(124, 58, 237);

        public static readonly Color Background = Color.FromArgb(241, 245, 249);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(248, 250, 252);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color BorderStrong = Color.FromArgb(203, 213, 225);

        public static readonly Color Text = Color.FromArgb(15, 23, 42);
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        public static readonly Color TextSubtle = Color.FromArgb(148, 163, 184);

        public static readonly Color Success = Color.FromArgb(16, 185, 129);
        public static readonly Color SuccessSoft = Color.FromArgb(209, 250, 229);
        public static readonly Color SuccessText = Color.FromArgb(4, 120, 87);

        public static readonly Color Warning = Color.FromArgb(245, 158, 11);
        public static readonly Color WarningSoft = Color.FromArgb(254, 243, 199);
        public static readonly Color WarningText = Color.FromArgb(180, 83, 9);

        public static readonly Color Danger = Color.FromArgb(239, 68, 68);
        public static readonly Color DangerSoft = Color.FromArgb(254, 226, 226);
        public static readonly Color DangerText = Color.FromArgb(185, 28, 28);

        public static readonly Color Info = Color.FromArgb(14, 165, 233);
        public static readonly Color InfoSoft = Color.FromArgb(224, 242, 254);
        public static readonly Color InfoText = Color.FromArgb(3, 105, 161);

        /// <summary>Bảng màu cho biểu đồ nhiều nhóm.</summary>
        public static readonly Color[] ChartPalette =
        {
            Color.FromArgb(79, 70, 229), Color.FromArgb(14, 165, 233), Color.FromArgb(16, 185, 129),
            Color.FromArgb(245, 158, 11), Color.FromArgb(239, 68, 68), Color.FromArgb(139, 92, 246),
            Color.FromArgb(236, 72, 153), Color.FromArgb(100, 116, 139)
        };

        // ---------- FONT ----------
        public const string FontFamily = "Segoe UI";
        public const string FontFamilySemibold = "Segoe UI Semibold";
        public const string IconFontFamily = "Segoe MDL2 Assets";

        public static readonly Font Body = new(FontFamily, 9.75f);
        public static readonly Font BodyBold = new(FontFamilySemibold, 9.75f);
        public static readonly Font Small = new(FontFamily, 8.5f);
        public static readonly Font SmallBold = new(FontFamilySemibold, 8.5f);
        public static readonly Font Tiny = new(FontFamilySemibold, 7.5f);
        public static readonly Font H1 = new(FontFamilySemibold, 19f);
        public static readonly Font H2 = new(FontFamilySemibold, 14f);
        public static readonly Font H3 = new(FontFamilySemibold, 11f);
        public static readonly Font StatValue = new(FontFamilySemibold, 21f);

        private static readonly Dictionary<float, Font> IconFonts = new();

        public static Font Icon(float size)
        {
            if (!IconFonts.TryGetValue(size, out var font))
            {
                font = new Font(IconFontFamily, size);
                IconFonts[size] = font;
            }
            return font;
        }

        // ---------- ICON (Segoe MDL2 Assets) ----------
        public static class Icons
        {
            public const string Home = "";
            public const string Library = "";
            public const string Book = "";
            public const string People = "";
            public const string Contact = "";
            public const string Switch = "";
            public const string Chart = "";
            public const string Add = "";
            public const string Edit = "";
            public const string Delete = "";
            public const string Refresh = "";
            public const string Search = "";
            public const string Check = "";
            public const string Close = "";
            public const string Warning = "";
            public const string Info = "";
            public const string Error = "";
            public const string Power = "";
            public const string Calendar = "";
            public const string Clock = "";
            public const string Lock = "";
            public const string Tag = "";
            public const string Forward = "";
            public const string Back = "";
            public const string History = "";
            public const string List = "";
            public const string Star = "";
            public const string Mail = "";
            public const string Phone = "";
            public const string Accept = "";
            public const string Return = "";
        }

        // ---------- HÀM VẼ ----------
        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2f, Math.Min(r.Width, r.Height));
            if (d <= 1f)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRound(Graphics g, Color color, RectangleF r, float radius)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using var path = RoundRect(r, radius);
            using var brush = new SolidBrush(color);
            g.FillPath(brush, path);
        }

        public static void DrawRound(Graphics g, Color color, RectangleF r, float radius, float width = 1f)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using var path = RoundRect(r, radius);
            using var pen = new Pen(color, width);
            g.DrawPath(pen, path);
        }

        /// <summary>Vẽ nền thẻ trắng bo góc có viền mảnh (dùng cho Card, StatCard, ...).</summary>
        public static void PaintCard(Graphics g, Rectangle bounds, Color parentBack, Color fill, Color border, float radius)
        {
            g.Clear(parentBack.A == 255 ? parentBack : Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, bounds.Width - 1.5f, bounds.Height - 1.5f);
            FillRound(g, fill, r, radius);
            DrawRound(g, border, r, radius);
        }

        public static void DrawIcon(Graphics g, string glyph, float size, Color color, Rectangle bounds)
        {
            TextRenderer.DrawText(g, glyph, Icon(size), bounds, color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        public static void DrawText(Graphics g, string? text, Font font, Rectangle bounds, Color color,
                                    TextFormatFlags extra = TextFormatFlags.Left)
        {
            if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0) return;
            TextRenderer.DrawText(g, text, font, bounds, color,
                extra | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        /// <summary>Lấy chữ cái đầu của 2 từ cuối trong họ tên: "Nguyễn Văn An" → "VA".</summary>
        public static string Initials(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "?";
            var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[^2].Substring(0, 1) + parts[^1].Substring(0, 1)).ToUpperInvariant();
        }

        /// <summary>Chọn màu ổn định theo chuỗi (dùng cho avatar, bìa sách theo thể loại).</summary>
        public static Color ColorFor(string? key)
        {
            if (string.IsNullOrEmpty(key)) return ChartPalette[0];
            int hash = 0;
            foreach (char c in key) hash = (hash * 31 + c) & 0x7FFFFFFF;
            return ChartPalette[hash % (ChartPalette.Length - 1)];
        }

        public static Color Blend(Color color, Color with, float amount)
        {
            amount = Math.Clamp(amount, 0f, 1f);
            return Color.FromArgb(
                (int)(color.R + (with.R - color.R) * amount),
                (int)(color.G + (with.G - color.G) * amount),
                (int)(color.B + (with.B - color.B) * amount));
        }

        /// <summary>Màu nền thực của control cha (bỏ qua các cha có nền trong suốt).</summary>
        public static Color ParentColor(Control control)
        {
            Control? p = control.Parent;
            while (p != null)
            {
                if (p.BackColor.A == 255) return p.BackColor;
                p = p.Parent;
            }
            return Background;
        }

        /// <summary>Bật double-buffer cho control không public thuộc tính DoubleBuffered (vd. DataGridView).</summary>
        public static void EnableDoubleBuffer(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }
    }
}
