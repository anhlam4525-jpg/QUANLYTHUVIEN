using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.Tests.Fakes;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử chức năng Quản lý sách: Thêm / Sửa / Xóa / Tìm kiếm.</summary>
    [TestClass]
    public class BookServiceTests
    {
        private FakeBookRepository _repo = null!;
        private ListLogger _log = null!;
        private BookService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repo = new FakeBookRepository();
            _log = new ListLogger();
            _service = new BookService(_repo, _log);
        }

        private static BookDTO NewBook(string title = "Lập trình C# cơ bản") => new()
        {
            Title = title,
            Author = "Nguyễn Văn Hưng",
            Category = "CNTT",
            PublishYear = 2023,
            TotalQuantity = 10
        };

        [TestMethod]
        public void AddBook_ValidData_ReturnsSuccessAndSaves()
        {
            var result = _service.AddBook(NewBook());

            Assert.IsTrue(result.Success);
            Assert.HasCount(1, _repo.Books);
            Assert.IsGreaterThan(0, result.Id);
            Assert.IsTrue(_log.Has(LogLevel.Information, 1001), "Phải ghi log Information BookInserted (1001).");
        }

        [TestMethod]
        public void AddBook_EmptyTitle_ReturnsFailAndDoesNotCallDatabase()
        {
            var result = _service.AddBook(NewBook(""));

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _repo.InsertCalls);
            Assert.IsTrue(_log.Has(LogLevel.Warning, 1900), "Dữ liệu sai phải ghi log Warning.");
        }

        [TestMethod]
        public void AddBook_FuturePublishYear_ReturnsFail()
        {
            var book = NewBook();
            book.PublishYear = DateTime.Now.Year + 1;
            Assert.IsFalse(_service.AddBook(book).Success);
        }

        [TestMethod]
        public void AddBook_NegativeQuantity_ReturnsFail()
        {
            var book = NewBook();
            book.TotalQuantity = -1;
            Assert.IsFalse(_service.AddBook(book).Success);
        }

        [TestMethod]
        public void AddBook_DatabaseError_ReturnsFailAndLogsErrorWithException()
        {
            _repo.ThrowOnWrite = true;

            var result = _service.AddBook(NewBook());

            Assert.IsFalse(result.Success);
            var error = _log.Entries.FirstOrDefault(e => e.Level == LogLevel.Error);
            Assert.IsNotNull(error.Exception, "Log Error phải kèm đối tượng Exception (có Stack Trace).");
        }

        [TestMethod]
        public void UpdateBook_ValidData_UpdatesRepository()
        {
            var existing = _repo.Seed("Sách cũ", total: 5);
            var updated = NewBook("Sách mới");
            updated.BookId = existing.BookId;

            var result = _service.UpdateBook(updated);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Sách mới", _repo.GetById(existing.BookId)!.Title);
        }

        [TestMethod]
        public void UpdateBook_QuantityBelowBorrowed_ReturnsFail()
        {
            var existing = _repo.Seed("Sách đang mượn", total: 5, borrowed: 3);
            var updated = NewBook("Sách đang mượn");
            updated.BookId = existing.BookId;
            updated.TotalQuantity = 2;

            var result = _service.UpdateBook(updated);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(5, _repo.GetById(existing.BookId)!.TotalQuantity, "Không được ghi xuống CSDL.");
        }

        [TestMethod]
        public void UpdateBook_NotExisting_ReturnsFail()
        {
            var book = NewBook();
            book.BookId = 999;
            Assert.IsFalse(_service.UpdateBook(book).Success);
        }

        [TestMethod]
        public void UpdateBook_NoIdSelected_ReturnsFail()
            => Assert.IsFalse(_service.UpdateBook(NewBook()).Success);

        [TestMethod]
        public void DeleteBook_NoActiveBorrows_RemovesBook()
        {
            var b = _repo.Seed("Sách cần xóa", total: 3);

            var result = _service.DeleteBook(b.BookId);

            Assert.IsTrue(result.Success);
            Assert.IsEmpty(_repo.Books);
        }

        [TestMethod]
        public void DeleteBook_HasActiveBorrows_ReturnsFailAndKeepsBook()
        {
            var b = _repo.Seed("Sách đang được mượn", total: 3, borrowed: 1);
            _repo.ActiveBorrowCounter = id => id == b.BookId ? 1 : 0;

            var result = _service.DeleteBook(b.BookId);

            Assert.IsFalse(result.Success);
            Assert.HasCount(1, _repo.Books);
        }

        [TestMethod]
        public void DeleteBook_InvalidId_ReturnsFail()
            => Assert.IsFalse(_service.DeleteBook(0).Success);

        [TestMethod]
        public void SearchBooks_ByTitle_ReturnsMatching()
        {
            _repo.Seed("Lập trình C# cơ bản", 5);
            _repo.Seed("Cơ sở dữ liệu SQL Server", 5);

            var result = _service.SearchBooks("C#", "Tất cả");

            Assert.HasCount(1, result);
            Assert.AreEqual("Lập trình C# cơ bản", result[0].Title);
        }

        [TestMethod]
        public void SearchBooks_ByCategoryKeyword_ReturnsMatching()
        {
            _repo.Seed("Sách AI", 5, category: "AI");
            _repo.Seed("Sách Mạng", 5, category: "Mạng");

            var result = _service.SearchBooks("mạng", null);

            Assert.HasCount(1, result);
        }

        [TestMethod]
        public void SearchBooks_EmptyKeyword_ReturnsAllAndTrimsInput()
        {
            _repo.Seed("A1", 1);
            _repo.Seed("A2", 1);

            var result = _service.SearchBooks("   ", "");

            Assert.HasCount(2, result);
            Assert.AreEqual(string.Empty, _repo.LastKeyword);
            Assert.AreEqual("Tất cả", _repo.LastCategory);
        }

        [TestMethod]
        public void SearchBooks_FilterByCategory_ReturnsOnlyThatCategory()
        {
            _repo.Seed("Sách 1", 1, category: "CNTT");
            _repo.Seed("Sách 2", 1, category: "Kỹ năng");

            var result = _service.SearchBooks(string.Empty, "Kỹ năng");

            Assert.HasCount(1, result);
            Assert.AreEqual("Kỹ năng", result[0].Category);
        }

        [TestMethod]
        public void GetCategories_ReturnsDistinctList()
        {
            _repo.Seed("S1", 1, category: "CNTT");
            _repo.Seed("S2", 1, category: "CNTT");
            _repo.Seed("S3", 1, category: "AI");

            Assert.HasCount(2, _service.GetCategories());
        }
    }
}
