using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Services;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DTO;
using LibraryManagement.Tests.Fakes;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử chức năng Mượn – Trả sách (tạo phiếu, hạn trả, ghi nhận trả, không cho mượn khi hết sách).</summary>
    [TestClass]
    public class BorrowServiceTests
    {
        private FakeBookRepository _books = null!;
        private FakeReaderRepository _readers = null!;
        private FakeBorrowRepository _borrows = null!;
        private ListLogger _log = null!;
        private BorrowService _service = null!;
        private ReaderDTO _reader = null!;
        private BookDTO _book = null!;

        [TestInitialize]
        public void Setup()
        {
            _books = new FakeBookRepository();
            _readers = new FakeReaderRepository();
            _borrows = new FakeBorrowRepository(_books, _readers);
            _log = new ListLogger();
            _service = new BorrowService(_borrows, _books, _readers, _log);

            _reader = _readers.Seed("DG001", "Nguyễn Văn An", "an@sv.edu.vn");
            _book = _books.Seed("Lập trình C# cơ bản", total: 2);
        }

        private DateTime Due(int days = 14) => DateTime.Today.AddDays(days);

        [TestMethod]
        public void BorrowBook_ValidRequest_CreatesTicketAndIncreasesBorrowed()
        {
            var result = _service.BorrowBook(_reader.ReaderId, _book.BookId, Due());

            Assert.IsTrue(result.Success, result.Message);
            Assert.HasCount(1, _borrows.Records);
            Assert.AreEqual(1, _book.BorrowedQuantity);
            Assert.AreEqual(Due(), _borrows.Records[0].DueDate, "Phải lưu đúng hạn trả.");
            Assert.IsTrue(_log.Has(LogLevel.Information, 1201));
        }

        [TestMethod]
        public void BorrowBook_OutOfStock_ReturnsFail()
        {
            var soldOut = _books.Seed("Sách đã hết", total: 1, borrowed: 1);

            var result = _service.BorrowBook(_reader.ReaderId, soldOut.BookId, Due());

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _borrows.CreateCalls, "Không được tạo phiếu khi hết sách.");
            Assert.IsTrue(_log.Has(LogLevel.Warning, 1203));
        }

        [TestMethod]
        public void BorrowBook_LastCopy_SucceedsThenNextFails()
        {
            var r2 = _readers.Seed("DG002", "Trần Thị Bình", "binh@sv.edu.vn");
            var r3 = _readers.Seed("DG003", "Lê Minh Châu", "chau@sv.edu.vn");

            Assert.IsTrue(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due()).Success);
            Assert.IsTrue(_service.BorrowBook(r2.ReaderId, _book.BookId, Due()).Success);
            Assert.IsFalse(_service.BorrowBook(r3.ReaderId, _book.BookId, Due()).Success);
            Assert.AreEqual(0, _book.AvailableQuantity);
        }

        [TestMethod]
        public void BorrowBook_DueDateInPast_ReturnsFail()
            => Assert.IsFalse(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due(-1)).Success);

        [TestMethod]
        public void BorrowBook_DueDateToday_AtBoundary_Succeeds()
            => Assert.IsTrue(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due(0)).Success);

        [TestMethod]
        public void BorrowBook_DueDateOverMaxLoanDays_ReturnsFail()
            => Assert.IsFalse(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due(BorrowValidator.MaxLoanDays + 1)).Success);

        [TestMethod]
        public void BorrowBook_ReaderNotSelected_ReturnsFail()
            => Assert.IsFalse(_service.BorrowBook(0, _book.BookId, Due()).Success);

        [TestMethod]
        public void BorrowBook_ReaderNotExisting_ReturnsFail()
            => Assert.IsFalse(_service.BorrowBook(9999, _book.BookId, Due()).Success);

        [TestMethod]
        public void BorrowBook_ReaderHasOverdue_ReturnsFail()
        {
            var other = _books.Seed("Sách khác", total: 3);
            _borrows.SeedRecord(_reader, other, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-5));

            var result = _service.BorrowBook(_reader.ReaderId, _book.BookId, Due());

            Assert.IsFalse(result.Success);
            Assert.Contains("quá hạn", result.Message);
        }

        [TestMethod]
        public void BorrowBook_ReaderReachedLimit_ReturnsFail()
        {
            for (int i = 0; i < BorrowValidator.MaxActiveBorrowsPerReader; i++)
            {
                var b = _books.Seed("Sách " + i, total: 2);
                _borrows.SeedRecord(_reader, b, DateTime.Today, Due());
            }

            Assert.IsFalse(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due()).Success);
        }

        [TestMethod]
        public void BorrowBook_SameBookAlreadyBorrowing_ReturnsFail()
        {
            _borrows.SeedRecord(_reader, _book, DateTime.Today, Due());
            Assert.IsFalse(_service.BorrowBook(_reader.ReaderId, _book.BookId, Due()).Success);
        }

        [TestMethod]
        public void ReturnBook_ActiveTicket_MarksReturnedAndRestoresStock()
        {
            var record = _borrows.SeedRecord(_reader, _book, DateTime.Today.AddDays(-3), Due(7));
            Assert.AreEqual(1, _book.BorrowedQuantity);

            var result = _service.ReturnBook(record.RecordId);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(BorrowRecordDTO.StatusReturned, record.Status);
            Assert.AreEqual(DateTime.Today, record.ReturnDate, "Phải ghi nhận ngày trả.");
            Assert.AreEqual(0, _book.BorrowedQuantity);
            Assert.IsTrue(_log.Has(LogLevel.Information, 1202));
        }

        [TestMethod]
        public void ReturnBook_Late_MessageContainsLateDays()
        {
            var record = _borrows.SeedRecord(_reader, _book, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-4));

            var result = _service.ReturnBook(record.RecordId);

            Assert.IsTrue(result.Success);
            Assert.Contains("trễ 4 ngày", result.Message, result.Message);
        }

        [TestMethod]
        public void ReturnBook_AlreadyReturned_ReturnsFail()
        {
            var record = _borrows.SeedRecord(_reader, _book, DateTime.Today.AddDays(-10), DateTime.Today, returned: true);
            Assert.IsFalse(_service.ReturnBook(record.RecordId).Success);
        }

        [TestMethod]
        public void ReturnBook_NotExisting_ReturnsFail()
            => Assert.IsFalse(_service.ReturnBook(12345).Success);

        [TestMethod]
        public void SearchRecords_OverdueFilter_ReturnsOnlyOverdue()
        {
            var other = _books.Seed("Sách khác", total: 3);
            _borrows.SeedRecord(_reader, _book, DateTime.Today.AddDays(-20), DateTime.Today.AddDays(-2));
            _borrows.SeedRecord(_reader, other, DateTime.Today, Due());

            var result = _service.SearchRecords(null, "Quá hạn");

            Assert.HasCount(1, result);
            Assert.IsTrue(result[0].IsOverdue);
            Assert.AreEqual("Quá hạn", result[0].DisplayStatus);
        }

        [TestMethod]
        public void SearchRecords_UnknownFilter_FallsBackToAll()
        {
            _borrows.SeedRecord(_reader, _book, DateTime.Today, Due());
            _borrows.SeedRecord(_reader, _books.Seed("X", 1), DateTime.Today.AddDays(-9), DateTime.Today, returned: true);

            Assert.HasCount(2, _service.SearchRecords("", "abc"));
        }

        [TestMethod]
        public void SuggestDueDate_Is14DaysFromToday()
        {
            _service.Today = () => new DateTime(2026, 11, 2);
            Assert.AreEqual(new DateTime(2026, 11, 16), _service.SuggestDueDate());
        }
    }
}
