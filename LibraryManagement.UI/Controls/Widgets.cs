using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    /// <summary>Lớp cơ sở cho các control tự vẽ hoàn toàn (double-buffer, tự vẽ lại khi đổi kích thước).</summary>
    [DesignerCategory("Code")]
    public abstract class PaintedControl : Control
    {
        protected PaintedControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.Body;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 2 || Height <= 2) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Draw(e.Graphics);
        }

        protected abstract void Draw(Graphics g);
    }

    // =====================================================================
    // NÚT MENU BÊN TRÁI
    // =====================================================================
    public class SidebarButton : PaintedControl
    {
        private bool _hover;
        private bool _active;
        private string? _badge;

        public string Glyph { get; set; } = Theme.Icons.Home;

        public bool Active
        {
            get => _active;
            set { _active = value; Invalidate(); }
        }

        /// <summary>Số hiển thị dạng huy hiệu đỏ bên phải (vd. số phiếu quá hạn). Null/rỗng = ẩn.</summary>
        public string? Badge
        {
            get => _badge;
            set { _badge = value; Invalidate(); }
        }

        public SidebarButton(string text, string glyph)
        {
            Text = text;
            Glyph = glyph;
            Height = Theme.S(44);
            Cursor = Cursors.Hand;
            BackColor = Theme.Sidebar;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void Draw(Graphics g)
        {
            g.Clear(Theme.Sidebar);
            var r = new RectangleF(Theme.S(12), Theme.S(2), Width - Theme.S(24), Height - Theme.S(4));
            Color text = Theme.SidebarText;
            Color icon = Theme.SidebarText;

            if (_active)
            {
                using var brush = new LinearGradientBrush(
                    new RectangleF(r.X, r.Y, r.Width + 1, r.Height + 1), Theme.Primary, Theme.Violet, 0f);
                using var path = Theme.RoundRect(r, Theme.Sf(10));
                g.FillPath(brush, path);
                text = Color.White;
                icon = Color.White;
            }
            else if (_hover)
            {
                Theme.FillRound(g, Theme.SidebarHover, r, Theme.Sf(10));
                text = Color.FromArgb(226, 232, 240);
                icon = Color.FromArgb(226, 232, 240);
            }

            Theme.DrawIcon(g, Glyph, 11f, icon, new Rectangle(Theme.S(26), 0, Theme.S(22), Height));
            Theme.DrawText(g, Text, _active ? Theme.BodyBold : Theme.Body,
                new Rectangle(Theme.S(60), 0, Width - Theme.S(100), Height), text);

            if (!string.IsNullOrEmpty(_badge))
            {
                int bw = Math.Max(Theme.S(22), TextRenderer.MeasureText(_badge, Theme.Tiny).Width + Theme.S(10));
                var br = new RectangleF(Width - Theme.S(24) - bw, (Height - Theme.S(20)) / 2f, bw, Theme.S(20));
                Theme.FillRound(g, _active ? Color.White : Theme.Danger, br, Theme.Sf(10));
                TextRenderer.DrawText(g, _badge, Theme.Tiny, Rectangle.Round(br), _active ? Theme.Primary : Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }
    }

    // =====================================================================
    // THẺ SỐ LIỆU (KPI)
    // =====================================================================
    public class StatCard : PaintedControl
    {
        private string _value = "0";
        private string _caption = string.Empty;

        public string Title { get; set; }
        public string Glyph { get; set; }
        public Color Accent { get; set; }
        public Color AccentSoft { get; set; }
        public Color CaptionColor { get; set; } = Theme.TextMuted;

        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public string Caption
        {
            get => _caption;
            set { _caption = value; Invalidate(); }
        }

        public StatCard(string title, string glyph, Color accent, Color accentSoft)
        {
            Title = title;
            Glyph = glyph;
            Accent = accent;
            AccentSoft = accentSoft;
            Height = Theme.S(124);
            Margin = new Padding(0, 0, Theme.S(16), 0);
        }

        protected override void Draw(Graphics g)
        {
            Theme.PaintCard(g, ClientRectangle, Theme.ParentColor(this), Color.White, Theme.Border, Theme.Sf(12));

            int pad = Theme.S(20);
            int iconSize = Theme.S(46);
            var iconRect = new Rectangle(Width - pad - iconSize, Theme.S(20), iconSize, iconSize);
            Theme.FillRound(g, AccentSoft, iconRect, Theme.Sf(12));
            Theme.DrawIcon(g, Glyph, 15f, Accent, iconRect);

            int textWidth = Width - pad * 2 - iconSize - Theme.S(8);
            Theme.DrawText(g, Title, Theme.Body, new Rectangle(pad, Theme.S(16), textWidth, Theme.S(24)), Theme.TextMuted);
            Theme.DrawText(g, _value, Theme.StatValue, new Rectangle(pad - Theme.S(2), Theme.S(40), textWidth + Theme.S(40), Theme.S(44)), Theme.Text);

            // Thanh nhấn màu nhỏ + chú thích
            Theme.FillRound(g, Accent, new RectangleF(pad, Height - Theme.S(30), Theme.S(6), Theme.S(6)), Theme.Sf(3));
            Theme.DrawText(g, _caption, Theme.Small,
                new Rectangle(pad + Theme.S(12), Height - Theme.S(38), Width - pad * 2 - Theme.S(12), Theme.S(22)), CaptionColor);
        }
    }

    // =====================================================================
    // AVATAR CHỮ CÁI ĐẦU
    // =====================================================================
    public class Avatar : PaintedControl
    {
        private string _name = string.Empty;

        public string FullName
        {
            get => _name;
            set { _name = value; Invalidate(); }
        }

        public Color? FillColor { get; set; }
        public float FontSize { get; set; } = 10f;

        public Avatar(int size)
        {
            Size = new Size(size, size);
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(Theme.ParentColor(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = FillColor ?? Theme.ColorFor(_name);
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       fill, Theme.Blend(fill, Color.Black, 0.18f), 45f))
            {
                g.FillEllipse(brush, 0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            }
            using var font = new Font(Theme.FontFamilySemibold, FontSize);
            TextRenderer.DrawText(g, Theme.Initials(_name), font, new Rectangle(0, 0, Width, Height), Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    // =====================================================================
    // NHÃN TRẠNG THÁI (chấm màu + chữ) – vd. trạng thái kết nối CSDL
    // =====================================================================
    public class StatusPill : PaintedControl
    {
        private string _text = string.Empty;
        private bool _ok = true;

        public string PillText
        {
            get => _text;
            set { _text = value; FitWidth(); Invalidate(); }
        }

        public bool Ok
        {
            get => _ok;
            set { _ok = value; Invalidate(); }
        }

        public StatusPill()
        {
            Height = Theme.S(30);
            Font = Theme.SmallBold;
        }

        private void FitWidth()
        {
            Width = TextRenderer.MeasureText(_text, Font).Width + Theme.S(38);
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(Theme.ParentColor(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Theme.FillRound(g, _ok ? Theme.SuccessSoft : Theme.DangerSoft, r, Height / 2f);
            Color dot = _ok ? Theme.Success : Theme.Danger;
            using (var b = new SolidBrush(dot))
            {
                g.FillEllipse(b, Theme.S(12), (Height - Theme.S(8)) / 2f, Theme.S(8), Theme.S(8));
            }
            Theme.DrawText(g, _text, Font, new Rectangle(Theme.S(26), 0, Width - Theme.S(30), Height),
                _ok ? Theme.SuccessText : Theme.DangerText);
        }
    }

    // =====================================================================
    // THANH CHỌN DẠNG TAB (segmented control) – lọc trạng thái phiếu mượn
    // =====================================================================
    public class SegmentedTabs : PaintedControl
    {
        private string[] _items = Array.Empty<string>();
        private int[]? _counts;
        private int _selected;
        private int _hover = -1;
        private Rectangle[] _rects = Array.Empty<Rectangle>();

        public event EventHandler? SelectedIndexChanged;

        public string[] Items
        {
            get => _items;
            set { _items = value; Recalculate(); Invalidate(); }
        }

        public int SelectedIndex
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string SelectedItem => _selected >= 0 && _selected < _items.Length ? _items[_selected] : string.Empty;

        public SegmentedTabs()
        {
            Height = Theme.S(36);
            Cursor = Cursors.Hand;
            Font = Theme.SmallBold;
        }

        public void SetCounts(int[]? counts)
        {
            _counts = counts;
            Recalculate();
            Invalidate();
        }

        private string ItemText(int i) =>
            _counts != null && i < _counts.Length ? $"{_items[i]}  {_counts[i]}" : _items[i];

        private void Recalculate()
        {
            _rects = new Rectangle[_items.Length];
            int x = Theme.S(4);
            for (int i = 0; i < _items.Length; i++)
            {
                int w = TextRenderer.MeasureText(ItemText(i), Font).Width + Theme.S(26);
                _rects[i] = new Rectangle(x, Theme.S(4), w, Height - Theme.S(8));
                x += w + Theme.S(2);
            }
            Width = x + Theme.S(2);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = Array.FindIndex(_rects, r => r.Contains(e.Location));
            if (idx != _hover) { _hover = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int idx = Array.FindIndex(_rects, r => r.Contains(e.Location));
            if (idx >= 0) SelectedIndex = idx;
            base.OnMouseClick(e);
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(Theme.ParentColor(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Theme.FillRound(g, Theme.Background, new RectangleF(0, 0, Width - 1, Height - 1), Theme.Sf(9));

            for (int i = 0; i < _items.Length && i < _rects.Length; i++)
            {
                var r = _rects[i];
                bool sel = i == _selected;
                if (sel)
                {
                    Theme.FillRound(g, Color.White, r, Theme.Sf(7));
                    Theme.DrawRound(g, Theme.Border, r, Theme.Sf(7));
                }
                else if (i == _hover)
                {
                    Theme.FillRound(g, Color.FromArgb(232, 237, 244), r, Theme.Sf(7));
                }
                TextRenderer.DrawText(g, ItemText(i), Font, r, sel ? Theme.Primary : Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            }
        }
    }

    // =====================================================================
    // BANNER CHÀO MỪNG (nền gradient)
    // =====================================================================
    [DesignerCategory("Code")]
    public class GradientPanel : Panel
    {
        public Color ColorFrom { get; set; } = Theme.Primary;
        public Color ColorTo { get; set; } = Theme.Violet;
        public float Radius { get; set; } = Theme.Sf(14);
        public bool Decorations { get; set; } = true;

        public GradientPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Primary;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.ParentColor(this));
            if (Width <= 2 || Height <= 2) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new RectangleF(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundRect(rect, Radius);
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height), ColorFrom, ColorTo, 20f))
            {
                g.FillPath(brush, path);
            }

            if (Decorations)
            {
                var oldClip = g.Clip;
                g.SetClip(path);
                using (var b1 = new SolidBrush(Color.FromArgb(28, Color.White)))
                using (var b2 = new SolidBrush(Color.FromArgb(18, Color.White)))
                {
                    float d1 = Height * 1.6f;
                    g.FillEllipse(b1, Width - d1 * 0.55f, -d1 * 0.45f, d1, d1);
                    float d2 = Height * 1.1f;
                    g.FillEllipse(b2, Width - d1 * 0.95f, Height * 0.35f, d2, d2);
                }
                g.Clip = oldClip;
            }
        }
    }
}
