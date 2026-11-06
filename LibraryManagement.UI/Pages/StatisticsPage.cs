using LibraryManagement.BLL.Services;
using LibraryManagement.UI.Controls;

namespace LibraryManagement.UI.Pages
{
    /// <summary>Thống kê: tổng quan kho, phiếu mượn, sách theo thể loại, top sách được mượn nhiều.</summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class StatisticsPage : PageBase
    {
        private readonly StatCard _cardCopies;
        private readonly StatCard _cardUtil;
        private readonly StatCard _cardReturned;
        private readonly StatCard _cardOverdue;
        private readonly BarChart _categoryChart;
        private readonly DonutChart _ticketDonut;
        private readonly HBarList _topBooks;
        private readonly HBarList _categoryTitles;

        public StatisticsPage(AppServices services) : base(services, "LibraryManagement.UI.StatisticsPage")
        {
            Title = "Thống kê";
            Subtitle = "Số liệu kho sách và hoạt động mượn trả";
            AutoScroll = true;
            AutoScrollMinSize = new Size(Theme.S(900), Theme.S(700));

            var root = Grid(1, 3);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(140)));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));

            // ----- KPI -----
            var kpi = Grid(4, 1);
            for (int i = 0; i < 4; i++) kpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _cardCopies = new StatCard("Tổng số cuốn sách", Theme.Icons.Library, Theme.Primary, Theme.PrimarySoft);
            _cardUtil = new StatCard("Tỉ lệ đang lưu hành", Theme.Icons.Chart, Theme.Info, Theme.InfoSoft);
            _cardReturned = new StatCard("Lượt đã trả", Theme.Icons.Return, Theme.Success, Theme.SuccessSoft);
            _cardOverdue = new StatCard("Phiếu quá hạn", Theme.Icons.Warning, Theme.Danger, Theme.DangerSoft);
            var cards = new[] { _cardCopies, _cardUtil, _cardReturned, _cardOverdue };
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Dock = DockStyle.Fill;
                cards[i].Margin = new Padding(0, 0, i < cards.Length - 1 ? Theme.S(16) : 0, Theme.S(16));
                kpi.Controls.Add(cards[i], i, 0);
            }
            root.Controls.Add(kpi, 0, 0);

            // ----- Hàng 2: thể loại + donut phiếu -----
            var row2 = Grid(2, 1);
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            row2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            row2.Margin = new Padding(0, 0, 0, Theme.S(16));

            var catCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(16), 0) };
            catCard.SetHeader("Số cuốn sách theo thể loại", "Tổng số cuốn hiện có của từng thể loại");
            _categoryChart = catCard.AddFill(new BarChart { UseItemColors = true });

            var ticketCard = new Card { Dock = DockStyle.Fill };
            ticketCard.SetHeader("Tình trạng phiếu mượn", "Toàn bộ phiếu đã lập");
            _ticketDonut = ticketCard.AddFill(new DonutChart());

            row2.Controls.Add(catCard, 0, 0);
            row2.Controls.Add(ticketCard, 1, 0);
            root.Controls.Add(row2, 0, 1);

            // ----- Hàng 3: top sách + đầu sách theo thể loại -----
            var row3 = Grid(2, 1);
            row3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            row3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var topCard = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Theme.S(16), 0) };
            topCard.SetHeader("Top 5 sách được mượn nhiều nhất", "Tính trên toàn bộ lịch sử mượn");
            _topBooks = topCard.AddFill(new HBarList { Unit = " lượt", EmptyText = "Chưa có lượt mượn nào" });

            var titleCard = new Card { Dock = DockStyle.Fill };
            titleCard.SetHeader("Số đầu sách theo thể loại", "Mức độ đa dạng của từng thể loại");
            _categoryTitles = titleCard.AddFill(new HBarList { Unit = " đầu sách" });

            row3.Controls.Add(topCard, 0, 0);
            row3.Controls.Add(titleCard, 1, 0);
            root.Controls.Add(row3, 0, 2);

            Controls.Add(root);
        }

        public override void OnNavigatedTo(object? parameter)
        {
            try
            {
                var stats = Services.Statistics.GetDashboardStats();
                double util = StatisticsService.CalculateUtilizationPercent(stats);

                _cardCopies.Value = stats.TotalCopies.ToString("N0");
                _cardCopies.Caption = $"{stats.TotalBookTitles} đầu sách · còn {stats.AvailableCopies} cuốn";
                _cardUtil.Value = $"{util:0.#}%";
                _cardUtil.Caption = $"{stats.BorrowedCopies}/{stats.TotalCopies} cuốn đang được mượn";
                _cardReturned.Value = stats.ReturnedTickets.ToString("N0");
                _cardReturned.Caption = $"{stats.ActiveTickets} phiếu chưa trả";
                _cardOverdue.Value = stats.OverdueTickets.ToString("N0");
                _cardOverdue.Caption = stats.OverdueTickets > 0 ? "Cần liên hệ độc giả" : "Không có phiếu quá hạn";
                _cardOverdue.CaptionColor = stats.OverdueTickets > 0 ? Theme.DangerText : Theme.SuccessText;

                var categories = Services.Statistics.GetCategoryStats();
                _categoryChart.SetData(categories.Select((c, i) =>
                    new ChartItem(c.Category, c.TotalCopies, Theme.ChartPalette[i % Theme.ChartPalette.Length])));
                _categoryTitles.SetData(categories.OrderByDescending(c => c.TitleCount).Select((c, i) =>
                    new ChartItem(c.Category, c.TitleCount, Theme.ChartPalette[i % Theme.ChartPalette.Length])));

                int onTime = Math.Max(0, stats.ActiveTickets - stats.OverdueTickets);
                int total = stats.ActiveTickets + stats.ReturnedTickets;
                _ticketDonut.SetData(new[]
                {
                    new ChartItem("Đang mượn (trong hạn)", onTime, Theme.Primary),
                    new ChartItem("Quá hạn", stats.OverdueTickets, Theme.Danger),
                    new ChartItem("Đã trả", stats.ReturnedTickets, Theme.Success)
                }, total.ToString("N0"), "tổng số phiếu");

                _topBooks.SetData(Services.Statistics.GetTopBooks(5).Select(t =>
                    new ChartItem(t.Title, t.BorrowCount, Theme.Primary)));
            }
            catch (Exception ex)
            {
                HandleError(ex, "tải số liệu thống kê");
            }
        }
    }
}
