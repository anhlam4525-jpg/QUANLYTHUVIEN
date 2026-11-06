using System.ComponentModel;
using Microsoft.Extensions.Logging;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Logging;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Lớp cơ sở cho các trang nội dung hiển thị trong khung chính.</summary>
    [DesignerCategory("Code")]
    public class PageBase : UserControl
    {
        protected readonly AppServices Services;
        protected readonly ILogger Logger;

        public string Title { get; protected set; } = string.Empty;
        public string Subtitle { get; protected set; } = string.Empty;

        /// <summary>Yêu cầu khung chính chuyển trang (key, tham số).</summary>
        public Action<string, object?>? Navigate { get; set; }

        /// <summary>Báo cho khung chính biết dữ liệu đã thay đổi (để cập nhật huy hiệu menu).</summary>
        public event EventHandler? DataChanged;

        // Constructor không tham số chỉ để Visual Studio Designer không báo lỗi khi mở file.
        public PageBase() : this(null!, "LibraryManagement.UI.Page") { }

        protected PageBase(AppServices services, string loggerCategory)
        {
            Services = services;
            Logger = AppLogging.CreateLogger(loggerCategory);
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.Background;
            Font = Theme.Body;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
        }

        /// <summary>Được gọi mỗi lần trang được hiển thị.</summary>
        public virtual void OnNavigatedTo(object? parameter) { }

        protected void RaiseDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);

        protected void ShowToast(string message, ToastKind kind = ToastKind.Success)
        {
            Toast.Show(FindForm(), message, kind);
        }

        protected void HandleError(Exception ex, string action)
        {
            Logger.LogError(UiLogEvents.UiError, ex, "Lỗi giao diện khi {Action}", action);
            ShowToast($"Không thể {action}. Kiểm tra kết nối CSDL. ({ex.Message})", ToastKind.Error);
        }

        protected void LogAction(string action, object? detail = null)
        {
            Logger.LogInformation(UiLogEvents.UserAction, "Người dùng thao tác {Action} {Detail}", action, detail ?? string.Empty);
        }

        /// <summary>Tạo TableLayoutPanel nền trong suốt theo màu trang.</summary>
        protected static TableLayoutPanel Grid(int columns, int rows)
        {
            var t = new TableLayoutPanel
            {
                ColumnCount = columns,
                RowCount = rows,
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            // Bảng chỉ có 1 cột / 1 hàng: cho ô đó chiếm 100% để control con Dock=Fill giãn hết
            if (columns == 1) t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            if (rows == 1) t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return t;
        }

        /// <summary>Nhãn "không có dữ liệu" hiển thị đè lên vùng dữ liệu của DataGridView.</summary>
        protected static Label AttachEmptyState(DataGridView grid, string text)
        {
            var lbl = new Label
            {
                Text = text,
                Font = Theme.Body,
                ForeColor = Theme.TextSubtle,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            grid.Controls.Add(lbl);
            void Place() => lbl.SetBounds(0, grid.ColumnHeadersHeight + 1, grid.Width,
                Math.Max(0, grid.Height - grid.ColumnHeadersHeight - 1));
            grid.Resize += (s, e) => Place();
            Place();
            return lbl;
        }

        /// <summary>Hàng toolbar: bên trái (ô tìm kiếm, bộ lọc), bên phải (các nút) – dùng FlowLayoutPanel.</summary>
        protected static (Panel Bar, FlowLayoutPanel Left, FlowLayoutPanel Right) ToolbarRow(Color back)
        {
            var bar = new Panel { BackColor = back, Dock = DockStyle.Fill, Margin = new Padding(0) };
            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                BackColor = back,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = back,
                Margin = new Padding(0),
                Padding = new Padding(0, Theme.S(1), 0, 0)
            };
            bar.Controls.Add(left);
            bar.Controls.Add(right);
            left.BringToFront();
            return (bar, left, right);
        }

        /// <summary>Bộ đếm trễ cho ô tìm kiếm (gõ xong 300ms mới tìm).</summary>
        protected static System.Windows.Forms.Timer Debounce(Action action, int ms = 300)
        {
            var timer = new System.Windows.Forms.Timer { Interval = ms };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                action();
            };
            return timer;
        }
    }
}
