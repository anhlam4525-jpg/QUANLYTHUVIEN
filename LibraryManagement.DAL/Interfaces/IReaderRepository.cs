using LibraryManagement.DTO;

namespace LibraryManagement.DAL.Interfaces
{
    public interface IReaderRepository
    {
        List<ReaderDTO> Search(string keyword);
        ReaderDTO? GetById(int readerId);
        int Insert(ReaderDTO reader);
        bool Update(ReaderDTO reader);
        bool Delete(int readerId);
        bool CodeExists(string readerCode, int excludeReaderId);
        bool EmailExists(string email, int excludeReaderId);
        int CountActiveBorrows(int readerId);
    }
}
