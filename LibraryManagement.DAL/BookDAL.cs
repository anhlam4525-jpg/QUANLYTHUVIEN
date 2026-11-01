using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class BookDAL
    {
        public List<BookDTO> GetAll()
        {
            return Search(string.Empty, "Tất cả");
        }

        public List<BookDTO> Search(string keyword, string category)
        {
            var list = new List<BookDTO>();
            string sql = @"SELECT BookId, Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity, CreatedAt
                           FROM Books
                           WHERE (@Keyword IS NULL OR Title LIKE '%' + @Keyword + '%' OR Author LIKE '%' + @Keyword + '%')
                             AND (@Category = N'Tất cả' OR Category = @Category)
                           ORDER BY BookId DESC";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            
            cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar).Value = 
                string.IsNullOrWhiteSpace(keyword) ? (object)DBNull.Value : keyword.Trim();
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar).Value = 
                string.IsNullOrWhiteSpace(category) ? "Tất cả" : category;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BookDTO
                {
                    BookId = Convert.ToInt32(reader["BookId"]),
                    Title = reader["Title"].ToString() ?? string.Empty,
                    Author = reader["Author"].ToString() ?? string.Empty,
                    Category = reader["Category"].ToString() ?? string.Empty,
                    PublishYear = Convert.ToInt32(reader["PublishYear"]),
                    TotalQuantity = Convert.ToInt32(reader["TotalQuantity"]),
                    BorrowedQuantity = Convert.ToInt32(reader["BorrowedQuantity"]),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                });
            }
            return list;
        }

        public BookDTO? GetById(int bookId)
        {
            string sql = @"SELECT BookId, Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity, CreatedAt
                           FROM Books WHERE BookId = @BookId";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
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
            return null;
        }

        public int Insert(BookDTO book)
        {
            string sql = @"INSERT INTO Books (Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity)
                           VALUES (@Title, @Author, @Category, @PublishYear, @TotalQuantity, 0);
                           SELECT SCOPE_IDENTITY();";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar).Value = book.Title;
            cmd.Parameters.Add("@Author", SqlDbType.NVarChar).Value = book.Author;
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar).Value = book.Category;
            cmd.Parameters.Add("@PublishYear", SqlDbType.Int).Value = book.PublishYear;
            cmd.Parameters.Add("@TotalQuantity", SqlDbType.Int).Value = book.TotalQuantity;

            conn.Open();
            var newId = cmd.ExecuteScalar();
            return Convert.ToInt32(newId);
        }

        public bool Update(BookDTO book)
        {
            string sql = @"UPDATE Books
                           SET Title = @Title,
                               Author = @Author,
                               Category = @Category,
                               PublishYear = @PublishYear,
                               TotalQuantity = @TotalQuantity
                           WHERE BookId = @BookId";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = book.BookId;
            cmd.Parameters.Add("@Title", SqlDbType.NVarChar).Value = book.Title;
            cmd.Parameters.Add("@Author", SqlDbType.NVarChar).Value = book.Author;
            cmd.Parameters.Add("@Category", SqlDbType.NVarChar).Value = book.Category;
            cmd.Parameters.Add("@PublishYear", SqlDbType.Int).Value = book.PublishYear;
            cmd.Parameters.Add("@TotalQuantity", SqlDbType.Int).Value = book.TotalQuantity;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int bookId)
        {
            string sql = "DELETE FROM Books WHERE BookId = @BookId";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<string> GetCategories()
        {
            var categories = new List<string> { "Tất cả" };
            string sql = "SELECT DISTINCT Category FROM Books ORDER BY Category";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string cat = reader["Category"].ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(cat) && !categories.Contains(cat))
                {
                    categories.Add(cat);
                }
            }
            return categories;
        }
    }
}
