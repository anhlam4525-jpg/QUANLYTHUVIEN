using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Services;
using LibraryManagement.DAL;

namespace LibraryManagement.UI
{
    /// <summary>
    /// "Composition root": tạo DAL → BLL một lần, gắn Logger đúng Category cho từng tầng
    /// (LibraryManagement.DAL.*, LibraryManagement.BLL.*) để Filter theo tầng hoạt động.
    /// </summary>
    public sealed class AppServices
    {
        public BookService Books { get; }
        public ReaderService Readers { get; }
        public BorrowService Borrows { get; }
        public StatisticsService Statistics { get; }
        public AuthService Auth { get; }

        public AppServices(ILoggerFactory factory)
        {
            var bookDal = new BookDAL(factory.CreateLogger("LibraryManagement.DAL.BookDAL"));
            var readerDal = new ReaderDAL(factory.CreateLogger("LibraryManagement.DAL.ReaderDAL"));
            var borrowDal = new BorrowDAL(factory.CreateLogger("LibraryManagement.DAL.BorrowDAL"));
            var userDal = new UserDAL(factory.CreateLogger("LibraryManagement.DAL.UserDAL"));

            Books = new BookService(bookDal, factory.CreateLogger("LibraryManagement.BLL.BookService"));
            Readers = new ReaderService(readerDal, factory.CreateLogger("LibraryManagement.BLL.ReaderService"));
            Borrows = new BorrowService(borrowDal, bookDal, readerDal, factory.CreateLogger("LibraryManagement.BLL.BorrowService"));
            Statistics = new StatisticsService(borrowDal, factory.CreateLogger("LibraryManagement.BLL.StatisticsService"));
            Auth = new AuthService(userDal, factory.CreateLogger("LibraryManagement.BLL.AuthService"));
        }
    }
}
