using LibraryManagement.DTO;

namespace LibraryManagement.DAL.Interfaces
{
    /// <summary>Hợp đồng truy cập dữ liệu Sách. BLL chỉ phụ thuộc interface này nên có thể Unit Test bằng repository giả.</summary>
    public interface IBookRepository
    {
        List<BookDTO> Search(string keyword, string category);
        BookDTO? GetById(int bookId);
        int Insert(BookDTO book);
        bool Update(BookDTO book);
        bool Delete(int bookId);
        List<string> GetCategories();
        int CountActiveBorrows(int bookId);
    }
}
