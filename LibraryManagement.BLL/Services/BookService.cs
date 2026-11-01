using LibraryManagement.BLL.Validators;
using LibraryManagement.DAL;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class BookService
    {
        private readonly BookDAL _bookDAL = new();
        private readonly BookValidator _validator = new();

        public List<BookDTO> GetAllBooks() => _bookDAL.GetAll();

        public List<BookDTO> SearchBooks(string keyword, string category) => _bookDAL.Search(keyword, category);

        public List<string> GetCategories() => _bookDAL.GetCategories();

        public (bool Success, string Message, int NewId) AddBook(BookDTO book)
        {
            if (!_validator.IsValidTitle(book.Title))
                return (false, "Tên sách không được để trống và phải từ 2 ký tự trở lên!", 0);

            if (!_validator.IsValidAuthor(book.Author))
                return (false, "Tên tác giả không được để trống!", 0);

            if (string.IsNullOrWhiteSpace(book.Category))
                return (false, "Vui lòng chọn thể loại sách!", 0);

            if (!_validator.IsValidPublishYear(book.PublishYear))
                return (false, $"Năm xuất bản phải từ 1900 đến {DateTime.Now.Year}!", 0);

            if (!_validator.IsValidQuantity(book.TotalQuantity))
                return (false, "Số lượng sách phải là số nguyên không âm!", 0);

            try
            {
                int id = _bookDAL.Insert(book);
                return (true, "Thêm sách mới thành công!", id);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi hệ thống khi thêm sách: {ex.Message}", 0);
            }
        }

        public (bool Success, string Message) UpdateBook(BookDTO book)
        {
            if (book.BookId <= 0)
                return (false, "Mã sách không hợp lệ!");

            if (!_validator.IsValidTitle(book.Title))
                return (false, "Tên sách không được để trống!");

            if (!_validator.IsValidAuthor(book.Author))
                return (false, "Tác giả không được để trống!");

            if (!_validator.IsValidPublishYear(book.PublishYear))
                return (false, "Năm xuất bản không hợp lệ!");

            if (!_validator.IsValidQuantity(book.TotalQuantity))
                return (false, "Số lượng không hợp lệ!");

            try
            {
                bool ok = _bookDAL.Update(book);
                return ok ? (true, "Cập nhật thông tin sách thành công!") : (false, "Không tìm thấy sách để cập nhật!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật: {ex.Message}");
            }
        }

        public (bool Success, string Message) DeleteBook(int bookId)
        {
            try
            {
                bool ok = _bookDAL.Delete(bookId);
                return ok ? (true, "Xóa sách thành công!") : (false, "Không thể xóa sách hoặc sách không tồn tại!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xóa sách: Có thể sách này đang có phiếu mượn liên quan ({ex.Message})");
            }
        }
    }
}
