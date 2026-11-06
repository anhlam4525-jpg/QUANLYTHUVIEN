using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LibraryManagement.BLL.Common;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DAL;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class BorrowService
    {
        public static readonly string[] StatusFilters = { "Tất cả", "Đang mượn", "Quá hạn", "Đã trả" };

        private readonly IBorrowRepository _borrowRepo;
        private readonly IBookRepository _bookRepo;
        private readonly IReaderRepository _readerRepo;
        private readonly ILogger _logger;
        private readonly BorrowValidator _validator = new();
        private readonly BookValidator _bookValidator = new();

        /// <summary>Ngày "hôm nay" — cho phép Unit Test cố định thời gian.</summary>
        public Func<DateTime> Today { get; set; } = () => DateTime.Today;

        public BorrowService() : this(new BorrowDAL(), new BookDAL(), new ReaderDAL()) { }

        public BorrowService(IBorrowRepository borrowRepo, IBookRepository bookRepo,
                             IReaderRepository readerRepo, ILogger? logger = null)
        {
            _borrowRepo = borrowRepo;
            _bookRepo = bookRepo;
            _readerRepo = readerRepo;
            _logger = logger ?? NullLogger.Instance;
        }

        public List<BorrowRecordDTO> GetAllRecords() => _borrowRepo.Search(string.Empty, "Tất cả");

        public List<BorrowRecordDTO> SearchRecords(string? keyword, string? statusFilter)
        {
            string status = string.IsNullOrWhiteSpace(statusFilter) || !StatusFilters.Contains(statusFilter)
                ? "Tất cả" : statusFilter;
            return _borrowRepo.Search(keyword?.Trim() ?? string.Empty, status);
        }

        public DateTime SuggestDueDate() => Today().AddDays(BorrowValidator.DefaultLoanDays);

        public OperationResult BorrowBook(int readerId, int bookId, DateTime dueDate, string? notes = null)
        {
            if (readerId <= 0) return Reject(readerId, bookId, "Vui lòng chọn độc giả!");
            if (bookId <= 0) return Reject(readerId, bookId, "Vui lòng chọn sách cần mượn!");

            DateTime today = Today();
            if (dueDate.Date < today)
                return Reject(readerId, bookId, "Hạn trả không được trước ngày mượn (hôm nay)!");
            if (!_validator.IsValidDueDate(today, dueDate))
                return Reject(readerId, bookId, $"Thời hạn mượn tối đa là {BorrowValidator.MaxLoanDays} ngày!");

            try
            {
                var reader = _readerRepo.GetById(readerId);
                if (reader == null) return Reject(readerId, bookId, "Độc giả không tồn tại!");

                var book = _bookRepo.GetById(bookId);
                if (book == null) return Reject(readerId, bookId, "Sách không tồn tại!");

                if (!_bookValidator.CanBorrow(book.TotalQuantity, book.BorrowedQuantity))
                    return Reject(readerId, bookId, $"Sách \"{book.Title}\" đã hết, không thể cho mượn!");

                if (_borrowRepo.CountOverdueByReader(readerId) > 0)
                    return Reject(readerId, bookId,
                        $"Độc giả {reader.FullName} đang có sách quá hạn, cần trả trước khi mượn tiếp!");

                if (!_validator.IsWithinBorrowLimit(_borrowRepo.CountActiveByReader(readerId)))
                    return Reject(readerId, bookId,
                        $"Mỗi độc giả chỉ được mượn tối đa {BorrowValidator.MaxActiveBorrowsPerReader} cuốn cùng lúc!");

                if (_borrowRepo.HasActiveBorrow(readerId, bookId))
                    return Reject(readerId, bookId, "Độc giả đang mượn cuốn sách này và chưa trả!");

                bool ok = _borrowRepo.CreateBorrowTicket(readerId, bookId, dueDate.Date, notes,
                                                         out int recordId, out string err);
                if (!ok) return Reject(readerId, bookId, err);

                _logger.LogInformation(BllLogEvents.BorrowCreated,
                    "Tạo phiếu mượn RecordId={RecordId}: ReaderCode={ReaderCode} mượn BookId={BookId}, DueDate={DueDate:yyyy-MM-dd}",
                    recordId, reader.ReaderCode, bookId, dueDate);
                return OperationResult.Ok(
                    $"Đã tạo phiếu #{recordId}: {reader.FullName} mượn \"{book.Title}\", hạn trả {dueDate:dd/MM/yyyy}.",
                    recordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex,
                    "Tạo phiếu mượn thất bại ReaderId={ReaderId}, BookId={BookId}", readerId, bookId);
                return OperationResult.Fail("Không thể tạo phiếu mượn do lỗi cơ sở dữ liệu.");
            }
        }

        public OperationResult ReturnBook(int recordId)
        {
            if (recordId <= 0) return OperationResult.Fail("Vui lòng chọn phiếu mượn cần trả!");

            try
            {
                var record = _borrowRepo.GetById(recordId);
                if (record == null) return OperationResult.Fail("Không tìm thấy phiếu mượn!");
                if (record.Status == BorrowRecordDTO.StatusReturned)
                    return OperationResult.Fail($"Phiếu #{recordId} đã được trả trước đó.");

                bool ok = _borrowRepo.ReturnBook(recordId, out string err);
                if (!ok)
                {
                    _logger.LogWarning(BllLogEvents.ValidationFailed,
                        "Trả sách bị từ chối RecordId={RecordId}: {Reason}", recordId, err);
                    return OperationResult.Fail(err);
                }

                int lateDays = _validator.CalculateOverdueDays(record.DueDate, Today());
                _logger.LogInformation(BllLogEvents.BookReturned,
                    "Trả sách RecordId={RecordId}, BookId={BookId}, ReaderCode={ReaderCode}, LateDays={LateDays}",
                    recordId, record.BookId, record.ReaderCode, lateDays);

                string msg = $"Đã ghi nhận {record.ReaderName} trả \"{record.BookTitle}\".";
                if (lateDays > 0) msg += $" Trả trễ {lateDays} ngày.";
                return OperationResult.Ok(msg, recordId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Trả sách thất bại RecordId={RecordId}", recordId);
                return OperationResult.Fail("Không thể ghi nhận trả sách do lỗi cơ sở dữ liệu.");
            }
        }

        private OperationResult Reject(int readerId, int bookId, string reason)
        {
            _logger.LogWarning(BllLogEvents.BorrowRejected,
                "Từ chối mượn ReaderId={ReaderId}, BookId={BookId}: {Reason}", readerId, bookId, reason);
            return OperationResult.Fail(reason);
        }
    }
}
