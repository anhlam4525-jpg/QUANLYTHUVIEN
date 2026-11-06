using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LibraryManagement.BLL.Common;
using LibraryManagement.DAL;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    /// <summary>Thống kê: tổng đầu sách, số cuốn còn mượn được, số phiếu đang mượn/quá hạn, biểu đồ.</summary>
    public class StatisticsService
    {
        private readonly IBorrowRepository _repo;
        private readonly ILogger _logger;

        public Func<DateTime> Today { get; set; } = () => DateTime.Today;

        public StatisticsService() : this(new BorrowDAL()) { }

        public StatisticsService(IBorrowRepository repo, ILogger? logger = null)
        {
            _repo = repo;
            _logger = logger ?? NullLogger.Instance;
        }

        public DashboardStatsDTO GetDashboardStats()
        {
            var stats = _repo.GetDashboardStats();
            _logger.LogInformation(BllLogEvents.StatisticsLoaded,
                "Thống kê: {Titles} đầu sách, {Available}/{Copies} cuốn sẵn sàng, {Active} phiếu đang mượn, {Overdue} quá hạn",
                stats.TotalBookTitles, stats.AvailableCopies, stats.TotalCopies, stats.ActiveTickets, stats.OverdueTickets);
            return stats;
        }

        public List<CategoryStatDTO> GetCategoryStats() => _repo.GetCategoryStats();

        public List<TopBookDTO> GetTopBooks(int top = 5) => _repo.GetTopBooks(Math.Clamp(top, 1, 50));

        /// <summary>
        /// Số phiếu mượn theo từng ngày trong <paramref name="days"/> ngày gần nhất, ngày không có phiếu = 0.
        /// Kết quả luôn có đúng <paramref name="days"/> phần tử, sắp xếp tăng dần theo ngày.
        /// </summary>
        public List<KeyValuePair<DateTime, int>> GetBorrowTrend(int days = 14)
        {
            days = Math.Clamp(days, 1, 365);
            var raw = _repo.GetBorrowCountByDay(days);
            DateTime start = Today().Date.AddDays(-days + 1);
            var result = new List<KeyValuePair<DateTime, int>>(days);
            for (int i = 0; i < days; i++)
            {
                DateTime d = start.AddDays(i);
                result.Add(new KeyValuePair<DateTime, int>(d, raw.TryGetValue(d, out int c) ? c : 0));
            }
            return result;
        }

        /// <summary>Tỉ lệ % số cuốn đang được mượn trên tổng số cuốn (0 nếu thư viện trống).</summary>
        public static double CalculateUtilizationPercent(DashboardStatsDTO stats)
        {
            if (stats.TotalCopies <= 0) return 0;
            return Math.Round(stats.BorrowedCopies * 100.0 / stats.TotalCopies, 1);
        }
    }
}
