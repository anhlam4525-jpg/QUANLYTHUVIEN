using LibraryManagement.DTO;

namespace LibraryManagement.DAL.Interfaces
{
    public interface IUserRepository
    {
        UserDTO? GetByUsername(string username);
    }
}
