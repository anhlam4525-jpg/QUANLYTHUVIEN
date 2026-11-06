using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LibraryManagement.BLL.Common;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DAL;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class BookService
    {
        private readonly IBookRepository _repo;
        private readonly ILogger _logger;
        private readonly BookValidator _validator = new();

        /// <summary>Dùng trong ứng dụng thật (kết nối SQL Server).</summary>
        public BookService() : this(new BookDAL()) { }

        /// <summary>Dùng cho Unit Test (truyền repository giả) hoặc khi cần gắn Logger.</summary>
        public BookService(IBookRepository repo, ILogger? logger = null)
        {
            _repo = repo;
            _logger = logger ?? NullLogger.Instance;
        }

        public List<BookDTO> GetAllBooks() => _repo.Search(string.Empty, "Tất cả");

        public List<BookDTO> SearchBooks(string? keyword, string? category)
        {
            string kw = keyword?.Trim() ?? string.Empty;
            string cat = string.IsNullOrWhiteSpace(category) ? "Tất cả" : category.Trim();
            var result = _repo.Search(kw, cat);
            if (kw.Length > 0 || cat != "Tất cả")
            {
                _logger.LogInformation(BllLogEvents.BookSearched,
                    "Tìm sách Keyword={Keyword}, Category={Category} → {Count} kết quả", kw, cat, result.Count);
            }
            return result;
        }

        public BookDTO? GetById(int bookId) => bookId > 0 ? _repo.GetById(bookId) : null;

        public List<string> GetCategories() => _repo.GetCategories();

        public OperationResult AddBook(BookDTO book)
        {
            string? error = Validate(book);
            if (error != null) return Invalid("AddBook", error);

            try
            {
                int id = _repo.Insert(book);
                _logger.LogInformation(BllLogEvents.BookInserted,
                    "Thêm sách thành công BookId={BookId}, Title={Title}, Quantity={Quantity}",
                    id, book.Title, book.TotalQuantity);
                return OperationResult.Ok($"Đã thêm sách \"{book.Title.Trim()}\" (mã {id}).", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Thêm sách thất bại Title={Title}", book.Title);
                return OperationResult.Fail("Không thể thêm sách do lỗi cơ sở dữ liệu. Vui lòng thử lại.");
            }
        }

        public OperationResult UpdateBook(BookDTO book)
        {
            if (book.BookId <= 0) return Invalid("UpdateBook", "Vui lòng chọn sách cần cập nhật!");

            string? error = Validate(book);
            if (error != null) return Invalid("UpdateBook", error);

            try
            {
                var current = _repo.GetById(book.BookId);
                if (current == null) return Invalid("UpdateBook", "Không tìm thấy sách cần cập nhật!");

                if (!_validator.IsValidQuantityForUpdate(book.TotalQuantity, current.BorrowedQuantity))
                    return Invalid("UpdateBook",
                        $"Tổng số lượng không được nhỏ hơn số cuốn đang cho mượn ({current.BorrowedQuantity})!");

                bool ok = _repo.Update(book);
                if (!ok) return Invalid("UpdateBook", "Không tìm thấy sách cần cập nhật!");

                _logger.LogInformation(BllLogEvents.BookUpdated,
                    "Cập nhật sách thành công BookId={BookId}, Title={Title}", book.BookId, book.Title);
                return OperationResult.Ok("Đã cập nhật thông tin sách.", book.BookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Cập nhật sách thất bại BookId={BookId}", book.BookId);
                return OperationResult.Fail("Không thể cập nhật sách do lỗi cơ sở dữ liệu.");
            }
        }

        public OperationResult DeleteBook(int bookId)
        {
            if (bookId <= 0) return Invalid("DeleteBook", "Vui lòng chọn sách cần xóa!");

            try
            {
                int active = _repo.CountActiveBorrows(bookId);
                if (active > 0)
                    return Invalid("DeleteBook",
                        $"Không thể xóa: sách đang có {active} phiếu mượn chưa trả.");

                bool ok = _repo.Delete(bookId);
                if (!ok) return Invalid("DeleteBook", "Sách không tồn tại hoặc đã bị xóa.");

                _logger.LogInformation(BllLogEvents.BookDeleted, "Xóa sách thành công BookId={BookId}", bookId);
                return OperationResult.Ok("Đã xóa sách khỏi thư viện.", bookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Xóa sách thất bại BookId={BookId}", bookId);
                return OperationResult.Fail("Không thể xóa sách do lỗi cơ sở dữ liệu.");
            }
        }

        /// <summary>Trả về thông báo lỗi đầu tiên, hoặc null nếu dữ liệu hợp lệ.</summary>
        public string? Validate(BookDTO book)
        {
            if (!_validator.IsValidTitle(book.Title))
                return "Tên sách phải từ 2 đến 200 ký tự!";
            if (!_validator.IsValidAuthor(book.Author))
                return "Tên tác giả phải từ 2 đến 100 ký tự!";
            if (!_validator.IsValidCategory(book.Category))
                return "Vui lòng chọn hoặc nhập thể loại sách!";
            if (!_validator.IsValidPublishYear(book.PublishYear))
                return $"Năm xuất bản phải từ {BookValidator.MinPublishYear} đến {DateTime.Now.Year}!";
            if (!_validator.IsValidQuantity(book.TotalQuantity))
                return $"Số lượng phải từ 0 đến {BookValidator.MaxQuantity}!";
            return null;
        }

        private OperationResult Invalid(string action, string message)
        {
            _logger.LogWarning(BllLogEvents.ValidationFailed,
                "{Action} bị từ chối: {Reason}", action, message);
            return OperationResult.Fail(message);
        }
    }
}
