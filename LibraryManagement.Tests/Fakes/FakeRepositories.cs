using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.Tests.Fakes
{
    /// <summary>Repository sách giả lưu trong bộ nhớ – giúp test BLL không cần SQL Server.</summary>
    public class FakeBookRepository : IBookRepository
    {
        public List<BookDTO> Books { get; } = new();
        public Func<int, int> ActiveBorrowCounter { get; set; } = _ => 0;
        public bool ThrowOnWrite { get; set; }
        public int InsertCalls { get; private set; }
        public string? LastKeyword { get; private set; }
        public string? LastCategory { get; private set; }
        private int _nextId = 5001;

        public List<BookDTO> Search(string keyword, string category)
        {
            LastKeyword = keyword;
            LastCategory = category;
            return Books.Where(b =>
                    (string.IsNullOrEmpty(keyword) ||
                     b.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     b.Author.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                     b.Category.Contains(keyword, StringComparison.OrdinalIgnoreCase)) &&
                    (category == "Tất cả" || b.Category == category))
                .ToList();
        }

        public BookDTO? GetById(int bookId) => Books.FirstOrDefault(b => b.BookId == bookId);

        public int Insert(BookDTO book)
        {
            InsertCalls++;
            if (ThrowOnWrite) throw new InvalidOperationException("Mô phỏng lỗi CSDL");
            book.BookId = _nextId++;
            Books.Add(book);
            return book.BookId;
        }

        public bool Update(BookDTO book)
        {
            if (ThrowOnWrite) throw new InvalidOperationException("Mô phỏng lỗi CSDL");
            var existing = GetById(book.BookId);
            if (existing == null) return false;
            existing.Title = book.Title;
            existing.Author = book.Author;
            existing.Category = book.Category;
            existing.PublishYear = book.PublishYear;
            existing.TotalQuantity = book.TotalQuantity;
            return true;
        }

        public bool Delete(int bookId)
        {
            if (ThrowOnWrite) throw new InvalidOperationException("Mô phỏng lỗi CSDL");
            return Books.RemoveAll(b => b.BookId == bookId) > 0;
        }

        public List<string> GetCategories() => Books.Select(b => b.Category).Distinct().OrderBy(c => c).ToList();

        public int CountActiveBorrows(int bookId) => ActiveBorrowCounter(bookId);

        public BookDTO Seed(string title, int total, int borrowed = 0, string category = "CNTT")
        {
            var b = new BookDTO
            {
                BookId = _nextId++,
                Title = title,
                Author = "Tác giả " + title,
                Category = category,
                PublishYear = 2023,
                TotalQuantity = total,
                BorrowedQuantity = borrowed
            };
            Books.Add(b);
            return b;
        }
    }

    public class FakeReaderRepository : IReaderRepository
    {
        public List<ReaderDTO> Readers { get; } = new();
        public Func<int, int> ActiveBorrowCounter { get; set; } = _ => 0;
        public int InsertCalls { get; private set; }
        private int _nextId = 1001;

        public List<ReaderDTO> Search(string keyword) => Readers.Where(r =>
            string.IsNullOrEmpty(keyword) ||
            r.ReaderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            r.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            r.Class.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();

        public ReaderDTO? GetById(int readerId) => Readers.FirstOrDefault(r => r.ReaderId == readerId);

        public int Insert(ReaderDTO reader)
        {
            InsertCalls++;
            reader.ReaderId = _nextId++;
            Readers.Add(reader);
            return reader.ReaderId;
        }

        public bool Update(ReaderDTO reader)
        {
            var existing = GetById(reader.ReaderId);
            if (existing == null) return false;
            existing.FullName = reader.FullName;
            existing.Class = reader.Class;
            existing.Email = reader.Email;
            existing.PhoneNumber = reader.PhoneNumber;
            return true;
        }

        public bool Delete(int readerId) => Readers.RemoveAll(r => r.ReaderId == readerId) > 0;

        public bool CodeExists(string readerCode, int excludeReaderId) => Readers.Any(r =>
            r.ReaderId != excludeReaderId && string.Equals(r.ReaderCode, readerCode.Trim(), StringComparison.OrdinalIgnoreCase));

        public bool EmailExists(string email, int excludeReaderId) => Readers.Any(r =>
            r.ReaderId != excludeReaderId && string.Equals(r.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

        public int CountActiveBorrows(int readerId) => ActiveBorrowCounter(readerId);

        public ReaderDTO Seed(string code, string name, string email)
        {
            var r = new ReaderDTO
            {
                ReaderId = _nextId++,
                ReaderCode = code,
                FullName = name,
                Class = "CNTT01",
                Email = email,
                PhoneNumber = "0901234567"
            };
            Readers.Add(r);
            return r;
        }
    }

    /// <summary>Repository phiếu mượn giả: mô phỏng đúng Transaction mượn/trả (cập nhật BorrowedQuantity của sách).</summary>
    public class FakeBorrowRepository : IBorrowRepository
    {
        private readonly FakeBookRepository _books;
        private readonly FakeReaderRepository _readers;
        private int _nextId = 1;

        public List<BorrowRecordDTO> Records { get; } = new();
        public Dictionary<DateTime, int> CountByDay { get; } = new();
        public int CreateCalls { get; private set; }

        public FakeBorrowRepository(FakeBookRepository books, FakeReaderRepository readers)
        {
            _books = books;
            _readers = readers;
            books.ActiveBorrowCounter = id => Records.Count(r => r.BookId == id && r.Status == BorrowRecordDTO.StatusBorrowing);
            readers.ActiveBorrowCounter = id => Records.Count(r => r.ReaderId == id && r.Status == BorrowRecordDTO.StatusBorrowing);
        }

        public List<BorrowRecordDTO> Search(string keyword, string statusFilter) => Records.Where(r =>
                (string.IsNullOrEmpty(keyword) || r.ReaderName.Contains(keyword) || r.BookTitle.Contains(keyword)) &&
                (statusFilter == "Tất cả" ||
                 (statusFilter == "Đang mượn" && r.Status == BorrowRecordDTO.StatusBorrowing) ||
                 (statusFilter == "Đã trả" && r.Status == BorrowRecordDTO.StatusReturned) ||
                 (statusFilter == "Quá hạn" && r.IsOverdue)))
            .ToList();

        public BorrowRecordDTO? GetById(int recordId) => Records.FirstOrDefault(r => r.RecordId == recordId);

        public bool CreateBorrowTicket(int readerId, int bookId, DateTime dueDate, string? notes,
                                       out int newRecordId, out string errorMessage)
        {
            CreateCalls++;
            newRecordId = 0;
            var book = _books.GetById(bookId);
            if (book == null || book.AvailableQuantity <= 0)
            {
                errorMessage = "Sách này đã hết, không thể cho mượn!";
                return false;
            }
            var reader = _readers.GetById(readerId);
            book.BorrowedQuantity++;
            var record = new BorrowRecordDTO
            {
                RecordId = _nextId++,
                ReaderId = readerId,
                ReaderCode = reader?.ReaderCode ?? string.Empty,
                ReaderName = reader?.FullName ?? string.Empty,
                BookId = bookId,
                BookTitle = book.Title,
                BorrowDate = DateTime.Today,
                DueDate = dueDate,
                Status = BorrowRecordDTO.StatusBorrowing,
                Notes = notes
            };
            Records.Add(record);
            newRecordId = record.RecordId;
            errorMessage = string.Empty;
            return true;
        }

        public bool ReturnBook(int recordId, out string errorMessage)
        {
            var record = GetById(recordId);
            if (record == null || record.Status != BorrowRecordDTO.StatusBorrowing)
            {
                errorMessage = "Phiếu mượn đã được trả trước đó hoặc không tồn tại!";
                return false;
            }
            record.Status = BorrowRecordDTO.StatusReturned;
            record.ReturnDate = DateTime.Today;
            var book = _books.GetById(record.BookId);
            if (book != null && book.BorrowedQuantity > 0) book.BorrowedQuantity--;
            errorMessage = string.Empty;
            return true;
        }

        public int CountActiveByReader(int readerId) =>
            Records.Count(r => r.ReaderId == readerId && r.Status == BorrowRecordDTO.StatusBorrowing);

        public int CountOverdueByReader(int readerId) =>
            Records.Count(r => r.ReaderId == readerId && r.IsOverdue);

        public bool HasActiveBorrow(int readerId, int bookId) =>
            Records.Any(r => r.ReaderId == readerId && r.BookId == bookId && r.Status == BorrowRecordDTO.StatusBorrowing);

        public DashboardStatsDTO GetDashboardStats()
        {
            int total = _books.Books.Sum(b => b.TotalQuantity);
            int borrowed = _books.Books.Sum(b => b.BorrowedQuantity);
            return new DashboardStatsDTO
            {
                TotalBookTitles = _books.Books.Count,
                TotalCopies = total,
                BorrowedCopies = borrowed,
                AvailableCopies = total - borrowed,
                TotalReaders = _readers.Readers.Count,
                ActiveTickets = Records.Count(r => r.Status == BorrowRecordDTO.StatusBorrowing),
                OverdueTickets = Records.Count(r => r.IsOverdue),
                ReturnedTickets = Records.Count(r => r.Status == BorrowRecordDTO.StatusReturned)
            };
        }

        public List<CategoryStatDTO> GetCategoryStats() => _books.Books
            .GroupBy(b => b.Category)
            .Select(g => new CategoryStatDTO { Category = g.Key, TitleCount = g.Count(), TotalCopies = g.Sum(b => b.TotalQuantity) })
            .ToList();

        public List<TopBookDTO> GetTopBooks(int top) => Records
            .GroupBy(r => r.BookTitle)
            .Select(g => new TopBookDTO { Title = g.Key, BorrowCount = g.Count() })
            .OrderByDescending(t => t.BorrowCount)
            .Take(top)
            .ToList();

        public Dictionary<DateTime, int> GetBorrowCountByDay(int days) => new(CountByDay);

        /// <summary>Thêm sẵn 1 phiếu (dùng để dựng tình huống quá hạn/đang mượn).</summary>
        public BorrowRecordDTO SeedRecord(ReaderDTO reader, BookDTO book, DateTime borrowDate, DateTime dueDate,
                                          bool returned = false)
        {
            var record = new BorrowRecordDTO
            {
                RecordId = _nextId++,
                ReaderId = reader.ReaderId,
                ReaderCode = reader.ReaderCode,
                ReaderName = reader.FullName,
                BookId = book.BookId,
                BookTitle = book.Title,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                ReturnDate = returned ? dueDate : null,
                Status = returned ? BorrowRecordDTO.StatusReturned : BorrowRecordDTO.StatusBorrowing
            };
            if (!returned) book.BorrowedQuantity++;
            Records.Add(record);
            return record;
        }
    }

    public class FakeUserRepository : IUserRepository
    {
        public List<UserDTO> Users { get; } = new();
        public bool ThrowOnRead { get; set; }

        public UserDTO? GetByUsername(string username)
        {
            if (ThrowOnRead) throw new InvalidOperationException("Mô phỏng mất kết nối CSDL");
            return Users.FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        }
    }
}
