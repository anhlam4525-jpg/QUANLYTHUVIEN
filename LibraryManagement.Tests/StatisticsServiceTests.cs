using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.Tests.Fakes;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử chức năng Thống kê: tổng đầu sách, số cuốn còn mượn được, số phiếu đang mượn, biểu đồ.</summary>
    [TestClass]
    public class StatisticsServiceTests
    {
        private FakeBookRepository _books = null!;
        private FakeReaderRepository _readers = null!;
        private FakeBorrowRepository _borrows = null!;
        private StatisticsService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _books = new FakeBookRepository();
            _readers = new FakeReaderRepository();
            _borrows = new FakeBorrowRepository(_books, _readers);
            _service = new StatisticsService(_borrows, new ListLogger());
        }

        [TestMethod]
        public void GetDashboardStats_ReturnsTitlesAvailableAndActiveTickets()
        {
            var r = _readers.Seed("DG001", "A", "a@sv.edu.vn");
            var b1 = _books.Seed("S1", total: 10);
            var b2 = _books.Seed("S2", total: 5);
            _borrows.SeedRecord(r, b1, DateTime.Today, DateTime.Today.AddDays(7));
            _borrows.SeedRecord(r, b2, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-1));
            _borrows.SeedRecord(r, b2, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(-16), returned: true);

            var stats = _service.GetDashboardStats();

            Assert.AreEqual(2, stats.TotalBookTitles, "Tổng đầu sách");
            Assert.AreEqual(15, stats.TotalCopies);
            Assert.AreEqual(13, stats.AvailableCopies, "Số cuốn còn có thể mượn");
            Assert.AreEqual(2, stats.ActiveTickets, "Số phiếu đang mượn");
            Assert.AreEqual(1, stats.OverdueTickets);
            Assert.AreEqual(1, stats.ReturnedTickets);
        }

        [TestMethod]
        public void GetBorrowTrend_FillsMissingDaysWithZero()
        {
            var today = new DateTime(2026, 11, 2);
            _service.Today = () => today;
            _borrows.CountByDay[today] = 3;
            _borrows.CountByDay[today.AddDays(-2)] = 1;

            var trend = _service.GetBorrowTrend(7);

            Assert.HasCount(7, trend);
            Assert.AreEqual(today.AddDays(-6), trend[0].Key);
            Assert.AreEqual(today, trend[6].Key);
            Assert.AreEqual(3, trend[6].Value);
            Assert.AreEqual(1, trend[4].Value);
            Assert.AreEqual(4, trend.Sum(t => t.Value));
        }

        [TestMethod]
        public void GetBorrowTrend_InvalidDays_ClampedToAtLeastOne()
            => Assert.HasCount(1, _service.GetBorrowTrend(0));

        [TestMethod]
        public void CalculateUtilizationPercent_NormalCase()
        {
            var stats = new DashboardStatsDTO { TotalCopies = 40, BorrowedCopies = 10 };
            Assert.AreEqual(25.0, StatisticsService.CalculateUtilizationPercent(stats));
        }

        [TestMethod]
        public void CalculateUtilizationPercent_EmptyLibrary_ReturnsZero()
            => Assert.AreEqual(0.0, StatisticsService.CalculateUtilizationPercent(new DashboardStatsDTO()));

        [TestMethod]
        public void GetTopBooks_OrderedByBorrowCount()
        {
            var r = _readers.Seed("DG001", "A", "a@sv.edu.vn");
            var popular = _books.Seed("Phổ biến", total: 10);
            var normal = _books.Seed("Bình thường", total: 10);
            _borrows.SeedRecord(r, popular, DateTime.Today, DateTime.Today, returned: true);
            _borrows.SeedRecord(r, popular, DateTime.Today, DateTime.Today, returned: true);
            _borrows.SeedRecord(r, normal, DateTime.Today, DateTime.Today, returned: true);

            var top = _service.GetTopBooks(5);

            Assert.AreEqual("Phổ biến", top[0].Title);
            Assert.AreEqual(2, top[0].BorrowCount);
        }

        [TestMethod]
        public void GetCategoryStats_GroupsByCategory()
        {
            _books.Seed("S1", 3, category: "CNTT");
            _books.Seed("S2", 4, category: "CNTT");
            _books.Seed("S3", 5, category: "AI");

            var cats = _service.GetCategoryStats();

            Assert.HasCount(2, cats);
            Assert.AreEqual(7, cats.First(c => c.Category == "CNTT").TotalCopies);
        }
    }
}
