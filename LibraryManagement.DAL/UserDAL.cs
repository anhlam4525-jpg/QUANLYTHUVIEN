using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class UserDAL : RepositoryBase, IUserRepository
    {
        public UserDAL(ILogger? logger = null) : base(logger) { }

        public UserDTO? GetByUsername(string username)
        {
            return Execute("User.GetByUsername", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(@"
                    SELECT UserId, Username, PasswordHash, FullName, Role, IsActive
                    FROM Users WHERE Username = @Username", conn);
                cmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username.Trim();
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return null;
                return new UserDTO
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    Username = reader["Username"].ToString() ?? string.Empty,
                    PasswordHash = (reader["PasswordHash"].ToString() ?? string.Empty).Trim(),
                    FullName = reader["FullName"].ToString() ?? string.Empty,
                    Role = reader["Role"].ToString() ?? string.Empty,
                    IsActive = Convert.ToBoolean(reader["IsActive"])
                };
            });
        }
    }
}
