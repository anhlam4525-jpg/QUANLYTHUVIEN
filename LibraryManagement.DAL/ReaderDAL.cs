using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class ReaderDAL
    {
        public List<ReaderDTO> GetAll()
        {
            return Search(string.Empty);
        }

        public List<ReaderDTO> Search(string keyword)
        {
            var list = new List<ReaderDTO>();
            string sql = @"SELECT ReaderId, ReaderCode, FullName, Class, Email, PhoneNumber, CreatedAt
                           FROM Readers
                           WHERE (@Keyword IS NULL OR ReaderCode LIKE '%' + @Keyword + '%' 
                                                  OR FullName LIKE '%' + @Keyword + '%' 
                                                  OR Class LIKE '%' + @Keyword + '%')
                           ORDER BY ReaderId DESC";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar).Value = 
                string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim();

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ReaderDTO
                {
                    ReaderId = Convert.ToInt32(reader["ReaderId"]),
                    ReaderCode = reader["ReaderCode"].ToString() ?? string.Empty,
                    FullName = reader["FullName"].ToString() ?? string.Empty,
                    Class = reader["Class"].ToString() ?? string.Empty,
                    Email = reader["Email"].ToString() ?? string.Empty,
                    PhoneNumber = reader["PhoneNumber"] == DBNull.Value ? null : reader["PhoneNumber"].ToString(),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                });
            }
            return list;
        }

        public int Insert(ReaderDTO reader)
        {
            string sql = @"INSERT INTO Readers (ReaderCode, FullName, Class, Email, PhoneNumber)
                           VALUES (@ReaderCode, @FullName, @Class, @Email, @PhoneNumber);
                           SELECT SCOPE_IDENTITY();";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ReaderCode", SqlDbType.VarChar).Value = reader.ReaderCode;
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar).Value = reader.FullName;
            cmd.Parameters.Add("@Class", SqlDbType.VarChar).Value = reader.Class;
            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = reader.Email;
            cmd.Parameters.Add("@PhoneNumber", SqlDbType.VarChar).Value = 
                string.IsNullOrWhiteSpace(reader.PhoneNumber) ? (object)DBNull.Value : reader.PhoneNumber;

            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public bool Update(ReaderDTO reader)
        {
            string sql = @"UPDATE Readers
                           SET FullName = @FullName,
                               Class = @Class,
                               Email = @Email,
                               PhoneNumber = @PhoneNumber
                           WHERE ReaderId = @ReaderId";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = reader.ReaderId;
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar).Value = reader.FullName;
            cmd.Parameters.Add("@Class", SqlDbType.VarChar).Value = reader.Class;
            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = reader.Email;
            cmd.Parameters.Add("@PhoneNumber", SqlDbType.VarChar).Value = 
                string.IsNullOrWhiteSpace(reader.PhoneNumber) ? (object)DBNull.Value : reader.PhoneNumber;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int readerId)
        {
            string sql = "DELETE FROM Readers WHERE ReaderId = @ReaderId";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
