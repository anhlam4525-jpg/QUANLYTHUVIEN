using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class ReaderDAL : RepositoryBase, IReaderRepository
    {
        private const string SelectColumns =
            "ReaderId, ReaderCode, FullName, Class, Email, PhoneNumber, CreatedAt";

        public ReaderDAL(ILogger? logger = null) : base(logger) { }

        public List<ReaderDTO> Search(string keyword)
        {
            return Execute("Reader.Search", () =>
            {
                var list = new List<ReaderDTO>();
                string sql = $@"SELECT {SelectColumns}
                                FROM Readers
                                WHERE (@Keyword IS NULL
                                       OR ReaderCode LIKE '%' + @Keyword + '%'
                                       OR FullName   LIKE N'%' + @Keyword + N'%'
                                       OR Class      LIKE '%' + @Keyword + '%'
                                       OR Email      LIKE '%' + @Keyword + '%')
                                ORDER BY ReaderCode";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = DbValue(keyword);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
                return list;
            });
        }

        public ReaderDTO? GetById(int readerId)
        {
            return Execute("Reader.GetById", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand($"SELECT {SelectColumns} FROM Readers WHERE ReaderId = @ReaderId", conn);
                cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                conn.Open();
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? Map(reader) : null;
            });
        }

        public int Insert(ReaderDTO reader)
        {
            return Execute("Reader.Insert", () =>
            {
                string sql = @"INSERT INTO Readers (ReaderCode, FullName, Class, Email, PhoneNumber)
                               VALUES (@ReaderCode, @FullName, @Class, @Email, @PhoneNumber);
                               SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@ReaderCode", SqlDbType.VarChar, 20).Value = reader.ReaderCode.Trim().ToUpperInvariant();
                AddReaderParameters(cmd, reader);

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        public bool Update(ReaderDTO reader)
        {
            return Execute("Reader.Update", () =>
            {
                string sql = @"UPDATE Readers
                               SET FullName = @FullName, Class = @Class, Email = @Email, PhoneNumber = @PhoneNumber
                               WHERE ReaderId = @ReaderId";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = reader.ReaderId;
                AddReaderParameters(cmd, reader);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        /// <summary>Xóa độc giả và lịch sử phiếu ĐÃ TRẢ của độc giả trong một Transaction.</summary>
        public bool Delete(int readerId)
        {
            return Execute("Reader.Delete", () =>
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    using (var cmdHistory = new SqlCommand(
                        "DELETE FROM BorrowRecords WHERE ReaderId = @ReaderId AND Status = N'Đã trả'", conn, trans))
                    {
                        cmdHistory.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                        cmdHistory.ExecuteNonQuery();
                    }

                    int rows;
                    using (var cmd = new SqlCommand("DELETE FROM Readers WHERE ReaderId = @ReaderId", conn, trans))
                    {
                        cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                        rows = cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return rows > 0;
                }
                catch
                {
                    BookDAL.SafeRollback(trans);
                    throw;
                }
            });
        }

        public bool CodeExists(string readerCode, int excludeReaderId)
        {
            return Execute("Reader.CodeExists", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Readers WHERE ReaderCode = @Code AND ReaderId <> @ExcludeId", conn);
                cmd.Parameters.Add("@Code", SqlDbType.VarChar, 20).Value = readerCode.Trim();
                cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = excludeReaderId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            });
        }

        public bool EmailExists(string email, int excludeReaderId)
        {
            return Execute("Reader.EmailExists", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Readers WHERE Email = @Email AND ReaderId <> @ExcludeId", conn);
                cmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = email.Trim();
                cmd.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = excludeReaderId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            });
        }

        public int CountActiveBorrows(int readerId)
        {
            return Execute("Reader.CountActiveBorrows", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM BorrowRecords WHERE ReaderId = @ReaderId AND Status = N'Đang mượn'", conn);
                cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        private static void AddReaderParameters(SqlCommand cmd, ReaderDTO reader)
        {
            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = reader.FullName.Trim();
            cmd.Parameters.Add("@Class", SqlDbType.VarChar, 20).Value = reader.Class.Trim().ToUpperInvariant();
            cmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = reader.Email.Trim().ToLowerInvariant();
            cmd.Parameters.Add("@PhoneNumber", SqlDbType.VarChar, 15).Value = DbValue(reader.PhoneNumber);
        }

        private static ReaderDTO Map(SqlDataReader reader)
        {
            return new ReaderDTO
            {
                ReaderId = Convert.ToInt32(reader["ReaderId"]),
                ReaderCode = reader["ReaderCode"].ToString() ?? string.Empty,
                FullName = reader["FullName"].ToString() ?? string.Empty,
                Class = reader["Class"].ToString() ?? string.Empty,
                Email = reader["Email"].ToString() ?? string.Empty,
                PhoneNumber = reader["PhoneNumber"] == DBNull.Value ? null : reader["PhoneNumber"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }
    }
}
