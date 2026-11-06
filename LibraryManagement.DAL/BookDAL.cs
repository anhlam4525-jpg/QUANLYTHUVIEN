using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class BookDAL : RepositoryBase, IBookRepository
    {
        private const string SelectColumns =
            "BookId, Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity, CreatedAt";

        public BookDAL(ILogger? logger = null) : base(logger) { }

        /// <summary>Tìm theo từ khóa (tên sách / tác giả / thể loại) và lọc theo thể loại.</summary>
        public List<BookDTO> Search(string keyword, string category)
        {
            return Execute("Book.Search", () =>
            {
                var list = new List<BookDTO>();
                string sql = $@"SELECT {SelectColumns}
                                FROM Books
                                WHERE (@Keyword IS NULL
                                       OR Title    LIKE N'%' + @Keyword + N'%'
                                       OR Author   LIKE N'%' + @Keyword + N'%'
                                       OR Category LIKE N'%' + @Keyword + N'%')
                                  AND (@Category IS NULL OR Category = @Category)
                                ORDER BY BookId DESC";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 200).Value = DbValue(keyword);
                bool allCategories = string.IsNullOrWhiteSpace(category) || category == "Tất cả";
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value =
                    allCategories ? DBNull.Value : category.Trim();

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
                return list;
            });
        }

        public BookDTO? GetById(int bookId)
        {
            return Execute("Book.GetById", () =>
            {
                string sql = $"SELECT {SelectColumns} FROM Books WHERE BookId = @BookId";
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;

                conn.Open();
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? Map(reader) : null;
            });
        }

        public int Insert(BookDTO book)
        {
            return Execute("Book.Insert", () =>
            {
                string sql = @"INSERT INTO Books (Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity)
                               VALUES (@Title, @Author, @Category, @PublishYear, @TotalQuantity, 0);
                               SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                AddBookParameters(cmd, book);

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        public bool Update(BookDTO book)
        {
            return Execute("Book.Update", () =>
            {
                string sql = @"UPDATE Books
                               SET Title = @Title, Author = @Author, Category = @Category,
                                   PublishYear = @PublishYear, TotalQuantity = @TotalQuantity
                               WHERE BookId = @BookId";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = book.BookId;
                AddBookParameters(cmd, book);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            });
        }

        /// <summary>Xóa sách và lịch sử phiếu ĐÃ TRẢ của sách đó trong một Transaction.</summary>
        public bool Delete(int bookId)
        {
            return Execute("Book.Delete", () =>
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    using (var cmdHistory = new SqlCommand(
                        "DELETE FROM BorrowRecords WHERE BookId = @BookId AND Status = N'Đã trả'", conn, trans))
                    {
                        cmdHistory.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        cmdHistory.ExecuteNonQuery();
                    }

                    int rows;
                    using (var cmd = new SqlCommand("DELETE FROM Books WHERE BookId = @BookId", conn, trans))
                    {
                        cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        rows = cmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                    return rows > 0;
                }
                catch
                {
                    SafeRollback(trans);
                    throw;
                }
            });
        }

        public List<string> GetCategories()
        {
            return Execute("Book.GetCategories", () =>
            {
                var categories = new List<string>();
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand("SELECT DISTINCT Category FROM Books ORDER BY Category", conn);

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string cat = reader.GetString(0);
                    if (!string.IsNullOrWhiteSpace(cat))
                        categories.Add(cat);
                }
                return categories;
            });
        }

        public int CountActiveBorrows(int bookId)
        {
            return Execute("Book.CountActiveBorrows", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM BorrowRecords WHERE BookId = @BookId AND Status = N'Đang mượn'", conn);
                cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        private static void AddBookParameters(SqlCommand cmd, BookDTO book)
        {
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = book.Title.Trim();
            cmd.Parameters.Add("@Author", SqlDbType.NVarChar, 100).Value = book.Author.Trim();
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = book.Category.Trim();
            cmd.Parameters.Add("@PublishYear", SqlDbType.Int).Value = book.PublishYear;
            cmd.Parameters.Add("@TotalQuantity", SqlDbType.Int).Value = book.TotalQuantity;
        }

        private static BookDTO Map(SqlDataReader reader)
        {
            return new BookDTO
            {
                BookId = Convert.ToInt32(reader["BookId"]),
                Title = reader["Title"].ToString() ?? string.Empty,
                Author = reader["Author"].ToString() ?? string.Empty,
                Category = reader["Category"].ToString() ?? string.Empty,
                PublishYear = Convert.ToInt32(reader["PublishYear"]),
                TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                BorrowedQuantity = Convert.ToInt32(reader["BorrowedQuantity"]),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
            };
        }

        internal static void SafeRollback(SqlTransaction trans)
        {
            try { trans.Rollback(); }
            catch { /* kết nối có thể đã đóng; bỏ qua để giữ Exception gốc */ }
        }
    }
}
