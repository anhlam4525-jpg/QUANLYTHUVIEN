using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    public readonly record struct ChartItem(string Label, double Value, Color Color);

    // =====================================================================
    // BIỂU ĐỒ CỘT ĐỨNG
    // =====================================================================
    public class BarChart : PaintedControl
    {
        private List<ChartItem> _items = new();

        public Color BarColor { get; set; } = Theme.Primary;
        public Color BarColorSoft { get; set; } = Theme.PrimaryLight;
        /// <summary>Tô đậm cột cuối (vd. "hôm nay"), các cột khác màu nhạt.</summary>
        public bool HighlightLast { get; set; }
        /// <summary>Dùng màu riêng của từng ChartItem.</summary>
        public bool UseItemColors { get; set; }
        public string EmptyText { get; set; } = "Chưa có dữ liệu";

        public BarChart()
        {
            BackColor = Color.White;
            Font = Theme.Small;
        }

        public void SetData(IEnumerable<ChartItem> items)
        {
            _items = items.ToList();
            Invalidate();
        }

        private static double NiceStep(double raw)
        {
            if (raw <= 0) return 1;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            double norm = raw / mag;
            double step = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
            return Math.Max(1, step * mag);
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(BackColor);
            if (_items.Count == 0 || _items.All(i => i.Value <= 0))
            {
                DrawEmpty(g);
                return;
            }

            int left = Theme.S(34), right = Theme.S(6), top = Theme.S(18), bottom = Theme.S(28);
            var plot = new Rectangle(left, top, Width - left - right, Height - top - bottom);
            if (plot.Width < 20 || plot.Height < 20) return;

            double max = Math.Max(1, _items.Max(i => i.Value));
            double step = NiceStep(max / 4);
            double niceMax = Math.Ceiling(max / step) * step;
            int ticks = (int)Math.Round(niceMax / step);

            // Lưới ngang + nhãn trục tung
            using (var gridPen = new Pen(Theme.Background, 1f) { DashStyle = DashStyle.Dash })
            {
                for (int t = 0; t <= ticks; t++)
                {
                    float y = plot.Bottom - (float)(t * step / niceMax) * plot.Height;
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                    TextRenderer.DrawText(g, (t * step).ToString("0"), Font,
                        new Rectangle(0, (int)y - Theme.S(8), left - Theme.S(8), Theme.S(16)), Theme.TextSubtle,
                        TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            float slot = plot.Width / (float)_items.Count;
            float barW = Math.Min(Theme.Sf(34), slot * 0.58f);
            int labelEvery = 1;
            int maxLabelW = _items.Max(i => TextRenderer.MeasureText(i.Label, Font).Width) + Theme.S(6);
            while (slot * labelEvery < maxLabelW && labelEvery < _items.Count) labelEvery++;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                float h = (float)(item.Value / niceMax) * plot.Height;
                float x = plot.Left + slot * i + (slot - barW) / 2f;
                var bar = new RectangleF(x, plot.Bottom - h, barW, h);

                Color color = UseItemColors ? item.Color
                    : HighlightLast ? (i == _items.Count - 1 ? BarColor : BarColorSoft)
                    : BarColor;

                if (h >= 1)
                {
                    using var path = TopRoundedBar(bar, Math.Min(Theme.Sf(6), barW / 2f));
                    using var brush = new SolidBrush(color);
                    g.FillPath(brush, path);
                }

                if (item.Value > 0)
                {
                    TextRenderer.DrawText(g, item.Value.ToString("0"), Theme.SmallBold,
                        new Rectangle((int)(plot.Left + slot * i), (int)(bar.Top - Theme.S(18)), (int)slot, Theme.S(16)),
                        Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding);
                }

                if (i % labelEvery == 0 || i == _items.Count - 1)
                {
                    TextRenderer.DrawText(g, item.Label, Font,
                        new Rectangle((int)(plot.Left + slot * i - slot), plot.Bottom + Theme.S(6), (int)(slot * 3), Theme.S(18)),
                        Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                }
            }
        }

        private void DrawEmpty(Graphics g)
        {
            TextRenderer.DrawText(g, EmptyText, Theme.Body, ClientRectangle, Theme.TextSubtle,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static GraphicsPath TopRoundedBar(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d < 1)
            {
                path.AddRectangle(r);
                return path;
            }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddLine(r.Right, r.Y + d / 2, r.Right, r.Bottom);
            path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            path.CloseFigure();
            return path;
        }
    }

    // =====================================================================
    // BIỂU ĐỒ VÀNH KHUYÊN (DONUT) + CHÚ THÍCH
    // =====================================================================
    public class DonutChart : PaintedControl
    {
        private List<ChartItem> _items = new();

        public string CenterValue { get; set; } = string.Empty;
        public string CenterCaption { get; set; } = string.Empty;

        public DonutChart()
        {
            BackColor = Color.White;
        }

        public void SetData(IEnumerable<ChartItem> items, string centerValue, string centerCaption)
        {
            _items = items.ToList();
            CenterValue = centerValue;
            CenterCaption = centerCaption;
            Invalidate();
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool horizontal = Width > Height * 1.45f;
            int legendRowH = Theme.S(26);
            int legendH = _items.Count * legendRowH;

            int diameter = horizontal
                ? Math.Min(Height - Theme.S(8), (int)(Width * 0.48f))
                : Math.Min(Width - Theme.S(16), Height - legendH - Theme.S(16));
            diameter = Math.Max(Theme.S(60), diameter);

            var donut = horizontal
                ? new Rectangle(Theme.S(4), (Height - diameter) / 2, diameter, diameter)
                : new Rectangle((Width - diameter) / 2, Theme.S(4), diameter, diameter);

            float thickness = Math.Max(Theme.Sf(12), diameter * 0.16f);
            var arcRect = new RectangleF(donut.X + thickness / 2, donut.Y + thickness / 2,
                                         donut.Width - thickness, donut.Height - thickness);

            using (var track = new Pen(Theme.Background, thickness))
            {
                g.DrawEllipse(track, arcRect);
            }

            double total = _items.Sum(i => Math.Max(0, i.Value));
            if (total > 0)
            {
                float start = -90f;
                foreach (var item in _items)
                {
                    if (item.Value <= 0) continue;
                    float sweep = (float)(item.Value / total * 360.0);
                    using var pen = new Pen(item.Color, thickness) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
                    g.DrawArc(pen, arcRect, start, Math.Max(0.5f, sweep - (_items.Count > 1 ? 1.2f : 0f)));
                    start += sweep;
                }
            }

            // Chữ ở giữa
            var center = new Rectangle(donut.X, donut.Y + donut.Height / 2 - Theme.S(22), donut.Width, Theme.S(30));
            TextRenderer.DrawText(g, CenterValue, Theme.H2, center, Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            var cap = new Rectangle(donut.X, center.Bottom - Theme.S(2), donut.Width, Theme.S(18));
            TextRenderer.DrawText(g, CenterCaption, Theme.Small, cap, Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // Chú thích
            int lx = horizontal ? donut.Right + Theme.S(20) : Theme.S(8);
            int ly = horizontal ? (Height - legendH) / 2 : donut.Bottom + Theme.S(12);
            int lw = Width - lx - Theme.S(4);
            foreach (var item in _items)
            {
                using (var b = new SolidBrush(item.Color))
                {
                    g.FillEllipse(b, lx, ly + (legendRowH - Theme.S(10)) / 2f, Theme.S(10), Theme.S(10));
                }
                string pct = total > 0 ? $"{item.Value / total * 100:0}%" : "0%";
                string value = $"{item.Value:0} · {pct}";
                int vw = TextRenderer.MeasureText(value, Theme.SmallBold).Width;
                Theme.DrawText(g, item.Label, Theme.Small,
                    new Rectangle(lx + Theme.S(18), ly, lw - Theme.S(18) - vw - Theme.S(6), legendRowH), Theme.TextMuted);
                Theme.DrawText(g, value, Theme.SmallBold,
                    new Rectangle(lx + lw - vw, ly, vw, legendRowH), Theme.Text, TextFormatFlags.Right);
                ly += legendRowH;
            }
        }
    }

    // =====================================================================
    // DANH SÁCH THANH NGANG (Top sách, thể loại...)
    // =====================================================================
    public class HBarList : PaintedControl
    {
        private List<ChartItem> _items = new();
        public string EmptyText { get; set; } = "Chưa có dữ liệu";
        public string Unit { get; set; } = string.Empty;

        public HBarList()
        {
            BackColor = Color.White;
        }

        public void SetData(IEnumerable<ChartItem> items)
        {
            _items = items.ToList();
            Invalidate();
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(BackColor);
            if (_items.Count == 0)
            {
                TextRenderer.DrawText(g, EmptyText, Theme.Body, ClientRectangle, Theme.TextSubtle,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            double max = Math.Max(1, _items.Max(i => i.Value));
            int rowH = Math.Min(Theme.S(48), Math.Max(Theme.S(34), Height / Math.Max(1, _items.Count)));
            int y = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (y + rowH > Height + 2) break;
                var item = _items[i];

                // Số thứ tự
                var badge = new Rectangle(0, y + (rowH - Theme.S(24)) / 2, Theme.S(24), Theme.S(24));
                Theme.FillRound(g, i == 0 ? Theme.WarningSoft : Theme.Background, badge, Theme.Sf(7));
                TextRenderer.DrawText(g, (i + 1).ToString(), Theme.SmallBold, badge,
                    i == 0 ? Theme.WarningText : Theme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                string valueText = $"{item.Value:0}{Unit}";
                int vw = TextRenderer.MeasureText(valueText, Theme.SmallBold).Width;
                int textX = Theme.S(36);
                int textW = Width - textX - vw - Theme.S(4);
                Theme.DrawText(g, item.Label, Theme.Body, new Rectangle(textX, y + Theme.S(2), textW, rowH / 2), Theme.Text);
                Theme.DrawText(g, valueText, Theme.SmallBold, new Rectangle(Width - vw - Theme.S(2), y + Theme.S(2), vw, rowH / 2),
                    Theme.Text, TextFormatFlags.Right);

                var track = new RectangleF(textX, y + rowH / 2f + Theme.S(4), Width - textX - Theme.S(4), Theme.S(7));
                Theme.FillRound(g, Theme.Background, track, Theme.Sf(4));
                float fw = (float)(item.Value / max) * track.Width;
                if (fw > 1)
                {
                    Theme.FillRound(g, item.Color, new RectangleF(track.X, track.Y, fw, track.Height), Theme.Sf(4));
                }
                y += rowH;
            }
        }
    }

    // =====================================================================
    // KHUNG CHI TIẾT (bìa/ảnh đại diện + các dòng thông tin + thanh tiến độ)
    // =====================================================================
    public class InfoPanel : PaintedControl
    {
        private readonly List<(string Label, string Value, Color? Color)> _rows = new();

        public string HeaderTitle { get; set; } = string.Empty;
        public string HeaderSubtitle { get; set; } = string.Empty;
        public string Glyph { get; set; } = Theme.Icons.Book;
        /// <summary>true: hiển thị avatar chữ cái đầu thay vì icon.</summary>
        public bool UseInitials { get; set; }
        public Color HeaderColor { get; set; } = Theme.Primary;
        public string EmptyText { get; set; } = "Chọn một dòng để xem chi tiết";
        public bool IsEmpty { get; private set; } = true;

        public double? Progress { get; set; }
        public string ProgressCaption { get; set; } = string.Empty;
        /// <summary>Màu thanh tiến độ cố định (null = tự đổi màu xanh/vàng/đỏ theo tỉ lệ).</summary>
        public Color? ProgressColor { get; set; }

        public InfoPanel()
        {
            BackColor = Color.White;
        }

        public void Clear()
        {
            _rows.Clear();
            IsEmpty = true;
            Progress = null;
            Invalidate();
        }

        public void SetRows(IEnumerable<(string Label, string Value, Color? Color)> rows)
        {
            _rows.Clear();
            _rows.AddRange(rows);
            IsEmpty = false;
            Invalidate();
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(BackColor);
            if (IsEmpty)
            {
                var iconRect = new Rectangle((Width - Theme.S(64)) / 2, Height / 2 - Theme.S(64), Theme.S(64), Theme.S(64));
                Theme.FillRound(g, Theme.Background, iconRect, Theme.Sf(32));
                Theme.DrawIcon(g, UseInitials ? Theme.Icons.Contact : Glyph, 20f, Theme.TextSubtle, iconRect);
                TextRenderer.DrawText(g, EmptyText, Theme.Body,
                    new Rectangle(Theme.S(10), iconRect.Bottom + Theme.S(12), Width - Theme.S(20), Theme.S(40)),
                    Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                return;
            }

            // Bìa / avatar
            var cover = new RectangleF(0, 0, Width - 1, Theme.S(112));
            using (var path = Theme.RoundRect(cover, Theme.Sf(12)))
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Theme.S(112) + 1),
                       HeaderColor, Theme.Blend(HeaderColor, Theme.Sidebar, 0.35f), 35f))
            {
                g.FillPath(brush, path);
            }
            using (var deco = new SolidBrush(Color.FromArgb(30, Color.White)))
            {
                g.FillEllipse(deco, Width - Theme.S(120), -Theme.S(50), Theme.S(170), Theme.S(170));
            }

            var badge = new Rectangle(Theme.S(16), Theme.S(18), Theme.S(52), Theme.S(52));
            using (var white = new SolidBrush(Color.FromArgb(50, Color.White)))
            {
                if (UseInitials) g.FillEllipse(white, badge);
                else
                {
                    using var bp = Theme.RoundRect(badge, Theme.Sf(12));
                    g.FillPath(white, bp);
                }
            }
            if (UseInitials)
            {
                using var f = new Font(Theme.FontFamilySemibold, 15f);
                TextRenderer.DrawText(g, Theme.Initials(HeaderTitle), f, badge, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else
            {
                Theme.DrawIcon(g, Glyph, 18f, Color.White, badge);
            }

            Theme.DrawText(g, HeaderSubtitle, Theme.SmallBold,
                new Rectangle(Theme.S(16), Theme.S(78), Width - Theme.S(32), Theme.S(22)), Color.FromArgb(224, 231, 255));

            // Tiêu đề (cho phép 2 dòng)
            int y = Theme.S(124);
            var titleRect = new Rectangle(0, y, Width, Theme.S(50));
            TextRenderer.DrawText(g, HeaderTitle, Theme.H3, titleRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            int titleH = TextRenderer.MeasureText(g, HeaderTitle, Theme.H3, new Size(Width, Theme.S(50)),
                TextFormatFlags.WordBreak).Height;
            y += Math.Min(Theme.S(50), titleH) + Theme.S(10);

            // Các dòng thông tin
            int rowH = Theme.S(32);
            using (var sep = new Pen(Theme.Background))
            {
                foreach (var row in _rows)
                {
                    if (y + rowH > Height - (Progress.HasValue ? Theme.S(56) : 0)) break;
                    Theme.DrawText(g, row.Label, Theme.Body, new Rectangle(0, y, Width / 2, rowH), Theme.TextMuted);
                    Theme.DrawText(g, row.Value, Theme.BodyBold, new Rectangle(Width / 3, y, Width - Width / 3, rowH),
                        row.Color ?? Theme.Text, TextFormatFlags.Right);
                    y += rowH;
                    g.DrawLine(sep, 0, y - 1, Width, y - 1);
                }
            }

            if (Progress.HasValue)
            {
                y += Theme.S(12);
                Theme.DrawText(g, ProgressCaption, Theme.Small, new Rectangle(0, y, Width, Theme.S(18)), Theme.TextMuted);
                y += Theme.S(22);
                var track = new RectangleF(0, y, Width - 1, Theme.S(8));
                Theme.FillRound(g, Theme.Background, track, Theme.Sf(4));
                double p = Math.Clamp(Progress.Value, 0, 1);
                Color fill = ProgressColor ?? (p < 0.3 ? Theme.Warning : Theme.Success);
                if (p > 0)
                {
                    Theme.FillRound(g, fill, new RectangleF(0, y, (float)(track.Width * p), track.Height), Theme.Sf(4));
                }
            }
        }
    }

    // =====================================================================
    // DANH SÁCH "CẦN CHÚ Ý" (phiếu quá hạn / sắp đến hạn) trên Dashboard
    // =====================================================================
    public class AttentionList : PaintedControl
    {
        public record Entry(string Name, string Detail, string BadgeText, bool Danger);

        private List<Entry> _entries = new();

        public AttentionList()
        {
            BackColor = Color.White;
        }

        public void SetData(IEnumerable<Entry> entries)
        {
            _entries = entries.ToList();
            Invalidate();
        }

        protected override void Draw(Graphics g)
        {
            g.Clear(BackColor);
            if (_entries.Count == 0)
            {
                var icon = new Rectangle((Width - Theme.S(56)) / 2, Height / 2 - Theme.S(48), Theme.S(56), Theme.S(56));
                Theme.FillRound(g, Theme.SuccessSoft, icon, Theme.Sf(28));
                Theme.DrawIcon(g, Theme.Icons.Check, 18f, Theme.Success, icon);
                TextRenderer.DrawText(g, "Không có phiếu quá hạn hay sắp đến hạn", Theme.Body,
                    new Rectangle(Theme.S(8), icon.Bottom + Theme.S(10), Width - Theme.S(16), Theme.S(40)),
                    Theme.TextMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak);
                return;
            }

            int rowH = Theme.S(54);
            int y = 0;
            using var sep = new Pen(Theme.Background);
            foreach (var e in _entries)
            {
                if (y + rowH > Height + 2) break;

                var av = new Rectangle(0, y + (rowH - Theme.S(36)) / 2, Theme.S(36), Theme.S(36));
                Color c = Theme.ColorFor(e.Name);
                using (var b = new SolidBrush(c)) g.FillEllipse(b, av);
                TextRenderer.DrawText(g, Theme.Initials(e.Name), Theme.SmallBold, av, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int bw = TextRenderer.MeasureText(e.BadgeText, Theme.Tiny).Width + Theme.S(14);
                var badge = new RectangleF(Width - bw - 1, y + (rowH - Theme.S(22)) / 2f, bw, Theme.S(22));
                Theme.FillRound(g, e.Danger ? Theme.DangerSoft : Theme.WarningSoft, badge, Theme.Sf(11));
                TextRenderer.DrawText(g, e.BadgeText, Theme.Tiny, Rectangle.Round(badge),
                    e.Danger ? Theme.DangerText : Theme.WarningText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int tx = Theme.S(48);
                int tw = Width - tx - bw - Theme.S(8);
                Theme.DrawText(g, e.Name, Theme.BodyBold, new Rectangle(tx, y + Theme.S(8), tw, Theme.S(20)), Theme.Text);
                Theme.DrawText(g, e.Detail, Theme.Small, new Rectangle(tx, y + Theme.S(28), tw, Theme.S(18)), Theme.TextMuted);

                y += rowH;
                g.DrawLine(sep, tx, y - 1, Width, y - 1);
            }
        }
    }
}
