using System.ComponentModel;

namespace LibraryManagement.UI.Controls
{
    /// <summary>Thẻ nền trắng bo góc, viền mảnh – khối giao diện cơ bản giống card trên web.</summary>
    [DesignerCategory("Code")]
    public class Card : Panel
    {
        private Panel? _header;

        public float Radius { get; set; } = Theme.Sf(12);
        public Color BorderColor { get; set; } = Theme.Border;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
            Padding = new Padding(Theme.S(20), Theme.S(16), Theme.S(20), Theme.S(16));
            Margin = new Padding(0);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Theme.PaintCard(e.Graphics, ClientRectangle, Theme.ParentColor(this), BackColor, BorderColor, Radius);
        }

        /// <summary>Thêm control lấp đầy phần còn lại của thẻ (luôn đưa lên trước để Dock đúng).</summary>
        public T AddFill<T>(T control) where T : Control
        {
            control.Dock = DockStyle.Fill;
            Controls.Add(control);
            control.BringToFront();
            return control;
        }

        /// <summary>Tạo tiêu đề thẻ (tiêu đề + mô tả + control bên phải tùy chọn) ở trên cùng.</summary>
        public Panel SetHeader(string title, string? subtitle = null, Control? right = null)
        {
            _header?.Dispose();

            int height = subtitle == null ? Theme.S(40) : Theme.S(54);
            var header = new Panel { Dock = DockStyle.Top, Height = height, BackColor = BackColor };

            var lblTitle = new Label
            {
                Text = title,
                Font = Theme.H3,
                ForeColor = Theme.Text,
                AutoSize = true,
                Location = new Point(0, 0),
                BackColor = BackColor
            };
            header.Controls.Add(lblTitle);

            if (subtitle != null)
            {
                var lblSub = new Label
                {
                    Text = subtitle,
                    Font = Theme.Small,
                    ForeColor = Theme.TextMuted,
                    AutoSize = true,
                    Location = new Point(0, Theme.S(24)),
                    BackColor = BackColor,
                    Name = "lblSubtitle"
                };
                header.Controls.Add(lblSub);
            }

            if (right != null)
            {
                right.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                header.Controls.Add(right);
                header.Layout += (s, e) =>
                    right.Location = new Point(header.Width - right.Width, Math.Max(0, (Theme.S(36) - right.Height) / 2));
            }

            Controls.Add(header);
            header.SendToBack(); // nằm cuối danh sách → được Dock đầu tiên → luôn ở trên cùng
            _header = header;
            return header;
        }

        /// <summary>Cập nhật dòng mô tả của tiêu đề (vd. "12 đầu sách").</summary>
        public void SetSubtitle(string text)
        {
            if (_header?.Controls["lblSubtitle"] is Label lbl) lbl.Text = text;
        }
    }
}
