namespace LibraryManagement.UI.Controls
{
    public enum ToastKind
    {
        Success,
        Error,
        Warning,
        Info
    }

    /// <summary>Thông báo nổi góc phải dưới, tự ẩn sau vài giây (thay cho MessageBox ở các thao tác thành công).</summary>
    public class Toast : PaintedControl
    {
        private readonly ToastKind _kind;
        private readonly string _message;
        private readonly System.Windows.Forms.Timer _timer;

        private Toast(ToastKind kind, string message, int durationMs)
        {
            _kind = kind;
            _message = message;
            Cursor = Cursors.Hand;

            int width = Theme.S(380);
            Size textSize = TextRenderer.MeasureText(message, Theme.Body, new Size(width - Theme.S(84), 0),
                TextFormatFlags.WordBreak);
            Size = new Size(width, Math.Max(Theme.S(72), textSize.Height + Theme.S(48)));

            _timer = new System.Windows.Forms.Timer { Interval = durationMs };
            _timer.Tick += (s, e) => Close();
            _timer.Start();
            Click += (s, e) => Close();
        }

        public static void Show(Control? host, string message, ToastKind kind = ToastKind.Success, int durationMs = 3500)
        {
            Form? form = host as Form ?? host?.FindForm();
            if (form == null || form.IsDisposed) return;

            // Chỉ giữ 1 toast tại một thời điểm
            foreach (var old in form.Controls.OfType<Toast>().ToList())
            {
                old.Close();
            }

            var toast = new Toast(kind, message, durationMs);
            toast.Location = new Point(
                form.ClientSize.Width - toast.Width - Theme.S(24),
                form.ClientSize.Height - toast.Height - Theme.S(24));
            toast.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            form.Controls.Add(toast);
            toast.BringToFront();
        }

        private void Close()
        {
            _timer.Stop();
            _timer.Dispose();
            Parent?.Controls.Remove(this);
            Dispose();
        }

        private (Color Accent, Color Soft, string Glyph, string Title) Style() => _kind switch
        {
            ToastKind.Error => (Theme.Danger, Theme.DangerSoft, Theme.Icons.Error, "Có lỗi xảy ra"),
            ToastKind.Warning => (Theme.Warning, Theme.WarningSoft, Theme.Icons.Warning, "Lưu ý"),
            ToastKind.Info => (Theme.Info, Theme.InfoSoft, Theme.Icons.Info, "Thông báo"),
            _ => (Theme.Success, Theme.SuccessSoft, Theme.Icons.Check, "Thành công")
        };

        protected override void Draw(Graphics g)
        {
            var (accent, soft, glyph, title) = Style();
            Theme.PaintCard(g, ClientRectangle, Theme.ParentColor(this), Color.White, Theme.BorderStrong, Theme.Sf(12));

            // Thanh màu bên trái
            Theme.FillRound(g, accent, new RectangleF(Theme.S(6), Theme.S(12), Theme.S(4), Height - Theme.S(24)), Theme.Sf(2));

            var icon = new Rectangle(Theme.S(20), Theme.S(16), Theme.S(36), Theme.S(36));
            Theme.FillRound(g, soft, icon, Theme.Sf(18));
            Theme.DrawIcon(g, glyph, 12f, accent, icon);

            int tx = Theme.S(68);
            Theme.DrawText(g, title, Theme.BodyBold, new Rectangle(tx, Theme.S(12), Width - tx - Theme.S(16), Theme.S(22)), Theme.Text);
            TextRenderer.DrawText(g, _message, Theme.Body,
                new Rectangle(tx, Theme.S(34), Width - tx - Theme.S(16), Height - Theme.S(40)), Theme.TextMuted,
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
