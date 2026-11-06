using LibraryManagement.BLL.Common;
using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Trang tổng quan: banner chào, 4 thẻ KPI, biểu đồ lượt mượn, tình trạng kho, danh sách cần chú ý.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class DashboardPage : PageBase
    {
        private readonly Label _lblGreeting;
        private readonly Label _lblSummary;
        private readonly StatCard _cardTitles;
        private readonly StatCard _cardAvailable;
        private readonly StatCard _cardActive;
        private readonly StatCard _cardReaders;
        private readonly BarChart _trend;
        private readonly DonutChart _donut;
        private readonly AttentionList _attention;

        public DashboardPage(AppServices services) : base(services, "LibraryManagement.UI.DashboardPage")
        {
            Title = "Tổng quan";
            Subtitle = "Bức tranh toàn cảnh hoạt động của thư viện hôm nay";
            AutoScroll = true;
            AutoScrollMinSize = new Size(Theme.S(900), Theme.S(640));

            var root = Grid(1, 3);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(132)));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(140)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ================= BANNER =================
            var banner = new GradientPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Theme.S(16)) };
            _lblGreeting = new Label
            {
                AutoSize = true,
                Font = Theme.H2,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(Theme.S(30), Theme.S(26))
            };
            _lblSummary = new Label
            {
                AutoSize = true,
                Font = Theme.Body,
                ForeColor = Color.FromArgb(224, 231, 255),
                BackColor = Color.Transparent,
                Location = new Point(Theme.S(32), Theme.S(64))
            };
            var btnBorrow = new ModernButton("Tạo phiếu mượn", ButtonVariant.White, Theme.Icons.Switch)
            { Anchor = AnchorStyles.Top | AnchorStyles.Right, Height = Theme.S(42) };
            var btnAddBook = new ModernButton("Thêm sách mới", ButtonVariant.White, Theme.Icons.Add)
            { Anchor = AnchorStyles.Top | AnchorStyles.Right, Height = Theme.S(42) };
            btnBorrow.AutoFit();
            btnAddBook.AutoFit();
            banner.Controls.AddRange(new Control[] { _lblGreeting, _lblSummary, btnBorrow, btnAddBook });
            banner.Resize += (s, e) =>
            {
                int y = (banner.Height - Theme.S(42)) / 2;
                btnBorrow.Location = new Point(banner.Width - btnBorrow.Width - Theme.S(28), y);
                btnAddBook.Location = new Point(btnBorrow.Left - btnAddBook.Width - Theme.S(12), y);
            };
            btnBorrow.Click += (s, e) => Navigate?.Invoke("borrow", null);
            btnAddBook.Click += (s, e) => Navigate?.Invoke("books", "add");
            root.Controls.Add(banner, 0, 0);

            // ================= THẺ KPI =================
            var stats = Grid(4, 1);
            for (int i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _cardTitles = new StatCard("Tổng đầu sách", Theme.Icons.Library, Theme.Primary, Theme.PrimarySoft);
            _cardAvailable = new StatCard("Số cuốn còn có thể mượn", Theme.Icons.Book, Theme.Success, Theme.SuccessSoft);
            _cardActive = new StatCard("Phiếu đang mượn", Theme.Icons.Switch, Theme.Warning, Theme.WarningSoft);
            _cardReaders = new StatCard("Độc giả", Theme.Icons.People, Theme.Info, Theme.InfoSoft);
            var cards = new[] { _cardTitles, _cardAvailable, _cardActive, _cardReaders };
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Dock = DockStyle.Fill;
                cards[i].Margin = new Padding(0, 0, i < cards.Length - 1 ? Theme.S(16) : 0, Theme.S(16));
                stats.Controls.Add(cards[i], i, 0);
            }
            _cardTitles.Click += (s, e) => Navigate?.Invoke("books", null);
            _cardActive.Click += (s, e) => Navigate?.Invoke("borrow", null);
            _cardReaders.Click += (s, e) => Navigate?.Invoke("readers", null);
            foreach (var c in cards) c.Cursor = Cursors.Hand;
            root.Controls.Add(stats, 0, 1);

            // ================= BIỂU ĐỒ + CẦN CHÚ Ý =================
            var bottom = Grid(3, 1);
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));

            var trendCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(16), 0) };
            trendCard.SetHeader("Lượt mượn 14 ngày qua", "Số phiếu mượn được tạo mỗi ngày");
            _trend = trendCard.AddFill(new BarChart { HighlightLast = true, EmptyText = "Chưa có phiếu mượn trong 14 ngày qua" });

            var donutCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(16), 0) };
            donutCard.SetHeader("Tình trạng kho sách", "Phân bổ số cuốn trong thư viện");
            _donut = donutCard.AddFill(new DonutChart());

            var attentionCard = new Card { Dock = DockStyle.Fill };
            attentionCard.SetHeader("Cần chú ý", "Phiếu quá hạn & sắp đến hạn (≤ 3 ngày)");
            _attention = attentionCard.AddFill(new AttentionList());

            bottom.Controls.Add(trendCard, 0, 0);
            bottom.Controls.Add(donutCard, 1, 0);
            bottom.Controls.Add(attentionCard, 2, 0);
            root.Controls.Add(bottom, 0, 2);

            Controls.Add(root);
        }

        public override void OnNavigatedTo(object? parameter)
        {
            try
            {
                var stats = Services.Statistics.GetDashboardStats();

                string name = AppSession.CurrentUser?.FullName ?? "bạn";
                int hour = DateTime.Now.Hour;
                string greet = hour < 11 ? "Chào buổi sáng" : hour < 14 ? "Chào buổi trưa" : hour < 18 ? "Chào buổi chiều" : "Chào buổi tối";
                _lblGreeting.Text = $"{greet}, {name}!";
                _lblSummary.Text = stats.OverdueTickets > 0
                    ? $"Hôm nay có {stats.ActiveTickets} phiếu đang mượn, trong đó {stats.OverdueTickets} phiếu đã quá hạn cần nhắc độc giả."
                    : $"Hôm nay có {stats.ActiveTickets} phiếu đang mượn và không có phiếu nào quá hạn. Tuyệt vời!";

                _cardTitles.Value = stats.TotalBookTitles.ToString("N0");
                _cardTitles.Caption = $"{stats.TotalCopies:N0} cuốn trong kho";
                _cardAvailable.Value = stats.AvailableCopies.ToString("N0");
                _cardAvailable.Caption = stats.TotalCopies > 0
                    ? $"{stats.AvailableCopies * 100.0 / stats.TotalCopies:0}% tổng số cuốn"
                    : "Kho đang trống";
                _cardActive.Value = stats.ActiveTickets.ToString("N0");
                _cardActive.Caption = stats.OverdueTickets > 0 ? $"{stats.OverdueTickets} phiếu quá hạn" : "Không có phiếu quá hạn";
                _cardActive.CaptionColor = stats.OverdueTickets > 0 ? Theme.DangerText : Theme.TextMuted;
                _cardReaders.Value = stats.TotalReaders.ToString("N0");
                _cardReaders.Caption = $"{stats.ReturnedTickets} lượt đã trả sách";

                // Biểu đồ lượt mượn
                var trend = Services.Statistics.GetBorrowTrend(14);
                _trend.SetData(trend.Select(t => new ChartItem(
                    t.Key.Date == DateTime.Today ? "Hôm nay" : t.Key.ToString("dd/MM"), t.Value, Theme.Primary)));

                // Donut tình trạng kho
                int overdue = stats.OverdueTickets;
                int borrowingOnTime = Math.Max(0, stats.BorrowedCopies - overdue);
                double util = StatisticsService.CalculateUtilizationPercent(stats);
                _donut.SetData(new[]
                {
                    new ChartItem("Sẵn sàng", stats.AvailableCopies, Theme.Success),
                    new ChartItem("Đang mượn", borrowingOnTime, Theme.Primary),
                    new ChartItem("Quá hạn", overdue, Theme.Danger)
                }, $"{util:0}%", "đang lưu hành");

                // Cần chú ý
                DateTime today = DateTime.Today;
                var attention = Services.Borrows.SearchRecords(string.Empty, BorrowRecordDTO.StatusBorrowing)
                    .Where(r => r.IsOverdue || (r.DueDate.Date - today).Days <= 3)
                    .OrderBy(r => r.DueDate)
                    .Take(10)
                    .Select(r =>
                    {
                        int days = (r.DueDate.Date - today).Days;
                        string badge = r.IsOverdue ? $"Quá hạn {r.OverdueDays} ngày"
                            : days == 0 ? "Hết hạn hôm nay"
                            : $"Còn {days} ngày";
                        return new AttentionList.Entry(r.ReaderName, r.BookTitle, badge, r.IsOverdue);
                    });
                _attention.SetData(attention);
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải số liệu tổng quan");
            }
        }
    }
}
