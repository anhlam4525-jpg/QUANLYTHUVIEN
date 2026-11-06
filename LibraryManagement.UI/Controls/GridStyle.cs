using System.Drawing.Drawing2D;

namespace LibraryManagement.UI.Controls
{
    /// <summary>Định dạng DataGridView theo phong cách bảng dữ liệu trên web (header nhạt, hàng cao, hover, badge).</summary>
    public static class GridStyle
    {
        public static DataGridView Create()
        {
            var dgv = new DataGridView();
            Apply(dgv);
            return dgv;
        }

        public static void Apply(DataGridView dgv)
        {
            Theme.EnableDoubleBuffer(dgv);

            dgv.AutoGenerateColumns = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Theme.Background;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = Theme.S(44);
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToOrderColumns = false;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgv.RowTemplate.Height = Theme.S(46);
            dgv.ScrollBars = ScrollBars.Both;
            dgv.StandardTab = true;

            var header = dgv.ColumnHeadersDefaultCellStyle;
            header.BackColor = Theme.SurfaceAlt;
            header.ForeColor = Theme.TextMuted;
            header.SelectionBackColor = Theme.SurfaceAlt;
            header.SelectionForeColor = Theme.TextMuted;
            header.Font = Theme.SmallBold;
            header.Padding = new Padding(Theme.S(12), 0, Theme.S(6), 0);
            header.Alignment = DataGridViewContentAlignment.MiddleLeft;
            header.WrapMode = DataGridViewTriState.False;

            var cell = dgv.DefaultCellStyle;
            cell.BackColor = Color.White;
            cell.ForeColor = Theme.Text;
            cell.Font = Theme.Body;
            cell.SelectionBackColor = Theme.PrimarySoft;
            cell.SelectionForeColor = Theme.Text;
            cell.Padding = new Padding(Theme.S(12), 0, Theme.S(6), 0);
            cell.WrapMode = DataGridViewTriState.False;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.White;

            // Hiệu ứng hover theo hàng
            int hoverRow = -1;
            dgv.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex == hoverRow) return;
                int old = hoverRow;
                hoverRow = e.RowIndex;
                if (old >= 0 && old < dgv.RowCount) dgv.InvalidateRow(old);
                if (hoverRow >= 0 && hoverRow < dgv.RowCount) dgv.InvalidateRow(hoverRow);
            };
            dgv.MouseLeave += (s, e) =>
            {
                int old = hoverRow;
                hoverRow = -1;
                if (old >= 0 && old < dgv.RowCount) dgv.InvalidateRow(old);
            };
            dgv.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex == hoverRow && !dgv.Rows[e.RowIndex].Selected)
                {
                    using var brush = new SolidBrush(Theme.SurfaceAlt);
                    e.Graphics.FillRectangle(brush, e.RowBounds);
                    e.PaintParts &= ~DataGridViewPaintParts.Background;
                }
            };
            // Đường kẻ dưới header
            dgv.CellPainting += (s, e) =>
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0 && e.Graphics != null)
                {
                    e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                    using var pen = new Pen(Theme.Border);
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    e.Handled = true;
                }
            };
        }

        public static DataGridViewTextBoxColumn AddColumn(DataGridView dgv, string property, string header,
            int width = 100, bool fill = false, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft,
            string? format = null)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = header,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                ReadOnly = true
            };
            if (fill)
            {
                // Cột chính (Tên sách, Họ tên...) giãn ra lấp chỗ trống, nhưng không hẹp hơn MinimumWidth
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                col.MinimumWidth = Theme.S(width);
                col.FillWeight = 100;
            }
            else
            {
                // Các cột còn lại giữ độ rộng cố định → khi bảng hẹp sẽ có thanh cuộn ngang
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Width = Theme.S(width);
            }
            col.DefaultCellStyle.Alignment = align;
            if (format != null) col.DefaultCellStyle.Format = format;
            if (align == DataGridViewContentAlignment.MiddleCenter)
            {
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            dgv.Columns.Add(col);
            return col;
        }

        /// <summary>Vẽ ô dạng "badge" bo tròn (vd. trạng thái Đang mượn / Quá hạn / Đã trả).</summary>
        public static void PaintBadge(DataGridViewCellPaintingEventArgs e, string text, Color back, Color fore,
                                      bool center = false)
        {
            if (e.Graphics == null) return;
            e.PaintBackground(e.CellBounds, true);
            var g = e.Graphics;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Size ts = TextRenderer.MeasureText(text, Theme.SmallBold);
            int w = ts.Width + Theme.S(16);
            int h = Theme.S(24);
            int x = center ? e.CellBounds.X + (e.CellBounds.Width - w) / 2 : e.CellBounds.X + Theme.S(10);
            var r = new RectangleF(x, e.CellBounds.Y + (e.CellBounds.Height - h) / 2f, w, h);
            Theme.FillRound(g, back, r, h / 2f);
            TextRenderer.DrawText(g, text, Theme.SmallBold, Rectangle.Round(r), fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            g.SmoothingMode = oldMode;
            e.Handled = true;
        }

        /// <summary>Vẽ ô có avatar tròn chữ cái đầu + tên (cột Họ tên).</summary>
        public static void PaintAvatarCell(DataGridViewCellPaintingEventArgs e, string name, string? sub = null)
        {
            if (e.Graphics == null) return;
            e.PaintBackground(e.CellBounds, true);
            var g = e.Graphics;
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int size = Theme.S(30);
            var av = new Rectangle(e.CellBounds.X + Theme.S(10), e.CellBounds.Y + (e.CellBounds.Height - size) / 2, size, size);
            using (var b = new SolidBrush(Theme.ColorFor(name))) g.FillEllipse(b, av);
            TextRenderer.DrawText(g, Theme.Initials(name), Theme.Tiny, av, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            int tx = av.Right + Theme.S(10);
            int tw = e.CellBounds.Right - tx - Theme.S(4);
            if (string.IsNullOrEmpty(sub))
            {
                Theme.DrawText(g, name, Theme.BodyBold, new Rectangle(tx, e.CellBounds.Y, tw, e.CellBounds.Height), Theme.Text);
            }
            else
            {
                int mid = e.CellBounds.Y + e.CellBounds.Height / 2;
                Theme.DrawText(g, name, Theme.BodyBold, new Rectangle(tx, mid - Theme.S(19), tw, Theme.S(20)), Theme.Text);
                Theme.DrawText(g, sub, Theme.Small, new Rectangle(tx, mid + Theme.S(1), tw, Theme.S(17)), Theme.TextMuted);
            }

            g.SmoothingMode = oldMode;
            e.Handled = true;
        }

        public static T? Selected<T>(DataGridView dgv) where T : class
        {
            return dgv.CurrentRow?.DataBoundItem as T;
        }

        /// <summary>Chọn lại dòng theo điều kiện sau khi nạp lại dữ liệu.</summary>
        public static void SelectWhere<T>(DataGridView dgv, Func<T, bool> predicate) where T : class
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.DataBoundItem is T item && predicate(item))
                {
                    row.Selected = true;
                    dgv.CurrentCell = row.Cells.Cast<DataGridViewCell>().FirstOrDefault(c => c.Visible);
                    if (row.Index >= 0 && !row.Displayed)
                    {
                        dgv.FirstDisplayedScrollingRowIndex = row.Index;
                    }
                    return;
                }
            }
        }
    }
}