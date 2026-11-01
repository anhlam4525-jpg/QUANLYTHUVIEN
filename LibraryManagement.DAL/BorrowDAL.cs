using System.Data;
using Microsoft.Data.SqlClient;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class BorrowDAL
    {
        public List<BorrowRecordDTO> GetAll()
        {
            var list = new List<BorrowRecordDTO>();
            string sql = @"SELECT br.RecordId, br.ReaderId, r.ReaderCode, r.FullName AS ReaderName,
                                  br.BookId, b.Title AS BookTitle, br.BorrowDate, br.DueDate, br.ReturnDate,
                                  br.Status, br.Notes
                           FROM BorrowRecords br
                           INNER JOIN Readers r ON br.ReaderId = r.ReaderId
                           INNER JOIN Books b ON br.BookId = b.BookId
                           ORDER BY br.RecordId DESC";

            using var conn = DbHelper.GetConnection();
            using var cmd = new SqlCommand(sql, conn);

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new BorrowRecordDTO
                {
                    RecordId = Convert.ToInt32(reader["RecordId"]),
                    ReaderId = Convert.ToInt32(reader["ReaderId"]),
                    ReaderCode = reader["ReaderCode"].ToString() ?? string.Empty,
                    ReaderName = reader["ReaderName"].ToString() ?? string.Empty,
                    BookId = Convert.ToInt32(reader["BookId"]),
                    BookTitle = reader["BookTitle"].ToString() ?? string.Empty,
                    BorrowDate = Convert.ToDateTime(reader["BorrowDate"]),
                    DueDate = Convert.ToDateTime(reader["DueDate"]),
                    ReturnDate = reader["ReturnDate"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReturnDate"]),
                    Status = reader["Status"].ToString() ?? "Đang mượn",
                    Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString()
                });
            }
            return list;
        }

        public bool CreateBorrowTicket(int readerId, int bookId, DateTime dueDate, out string errorMessage)
        {
            errorMessage = string.Empty;
            using var conn = DbHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Bước 1: Kiểm tra số lượng sách còn khả dụng
                string checkSql = "SELECT TotalQuantity - BorrowedQuantity FROM Books WITH (UPDLOCK) WHERE BookId = @BookId";
                using (var cmdCheck = new SqlCommand(checkSql, conn, trans))
                {
                    cmdCheck.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                    var result = cmdCheck.ExecuteScalar();
                    if (result == null || Convert.ToInt32(result) <= 0)
                    {
                        trans.Rollback();
                        errorMessage = "Sách này đã hết bản cuốn trong thư viện, không thể cho mượn!";
                        return false;
                    }
                }

                // Bước 2: Thêm bản ghi phiếu mượn
                string insertSql = @"INSERT INTO BorrowRecords (ReaderId, BookId, BorrowDate, DueDate, Status)
                                     VALUES (@ReaderId, @BookId, CAST(GETDATE() AS DATE), @DueDate, N'Đang mượn')";
                using (var cmdInsert = new SqlCommand(insertSql, conn, trans))
                {
                    cmdInsert.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                    cmdInsert.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                    cmdInsert.Parameters.Add("@DueDate", SqlDbType.Date).Value = dueDate.Date;
                    cmdInsert.ExecuteNonQuery();
                }

                // Bước 3: Tăng số lượng đã mượn
                string updateSql = "UPDATE Books SET BorrowedQuantity = BorrowedQuantity + 1 WHERE BookId = @BookId";
                using (var cmdUpdate = new SqlCommand(updateSql, conn, trans))
                {
                    cmdUpdate.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                    cmdUpdate.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool ReturnBook(int recordId, int bookId, out string errorMessage)
        {
            errorMessage = string.Empty;
            using var conn = DbHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Bước 1: Cập nhật phiếu mượn sang 'Đã trả'
                string updateTicketSql = @"UPDATE BorrowRecords 
                                           SET ReturnDate = CAST(GETDATE() AS DATE), Status = N'Đã trả'
                                           WHERE RecordId = @RecordId AND Status = N'Đang mượn'";
                using (var cmdTicket = new SqlCommand(updateTicketSql, conn, trans))
                {
                    cmdTicket.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                    int rows = cmdTicket.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        trans.Rollback();
                        errorMessage = "Phiếu mượn này đã được trả trước đó hoặc không tồn tại!";
                        return false;
                    }
                }

                // Bước 2: Giảm số lượng đã mượn trong bảng Books
                string updateBookSql = @"UPDATE Books 
                                         SET BorrowedQuantity = CASE WHEN BorrowedQuantity > 0 THEN BorrowedQuantity - 1 ELSE 0 END
                                         WHERE BookId = @BookId";
                using (var cmdBook = new SqlCommand(updateBookSql, conn, trans))
                {
                    cmdBook.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                    cmdBook.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                errorMessage = ex.Message;
                return false;
            }
        }

        public DashboardStatsDTO GetDashboardStats()
        {
            var stats = new DashboardStatsDTO();
            using var conn = DbHelper.GetConnection();
            conn.Open();

            string bookSql = @"SELECT COUNT(*) AS TotalTitles, 
                                      ISNULL(SUM(TotalQuantity), 0) AS TotalCopies, 
                                      ISNULL(SUM(BorrowedQuantity), 0) AS BorrowedCopies
                               FROM Books";
            using (var cmd = new SqlCommand(bookSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    stats.TotalBookTitles = Convert.ToInt32(reader["TotalTitles"]);
                    stats.TotalCopies = Convert.ToInt32(reader["TotalCopies"]);
                    stats.BorrowedCopies = Convert.ToInt32(reader["BorrowedCopies"]);
                    stats.AvailableCopies = Math.Max(0, stats.TotalCopies - stats.BorrowedCopies);
                }
            }

            string readerSql = "SELECT COUNT(*) FROM Readers";
            using (var cmdReader = new SqlCommand(readerSql, conn))
            {
                stats.TotalReaders = Convert.ToInt32(cmdReader.ExecuteScalar() ?? 0);
            }

            return stats;
        }
    }
}
