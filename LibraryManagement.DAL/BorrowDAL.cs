using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.DAL
{
    public class BorrowDAL : RepositoryBase, IBorrowRepository
    {
        private const string SelectSql = @"
            SELECT br.RecordId, br.ReaderId, r.ReaderCode, r.FullName AS ReaderName,
                   br.BookId, b.Title AS BookTitle, br.BorrowDate, br.DueDate, br.ReturnDate,
                   br.Status, br.Notes
            FROM BorrowRecords br
            INNER JOIN Readers r ON br.ReaderId = r.ReaderId
            INNER JOIN Books   b ON br.BookId   = b.BookId";

        public BorrowDAL(ILogger? logger = null) : base(logger) { }

        public List<BorrowRecordDTO> Search(string keyword, string statusFilter)
        {
            return Execute("Borrow.Search", () =>
            {
                var list = new List<BorrowRecordDTO>();
                string sql = SelectSql + @"
                    WHERE (@Keyword IS NULL
                           OR r.ReaderCode LIKE '%' + @Keyword + '%'
                           OR r.FullName   LIKE N'%' + @Keyword + N'%'
                           OR b.Title      LIKE N'%' + @Keyword + N'%')
                      AND (   @Status = N'Tất cả'
                           OR (@Status = N'Đang mượn' AND br.Status = N'Đang mượn')
                           OR (@Status = N'Đã trả'    AND br.Status = N'Đã trả')
                           OR (@Status = N'Quá hạn'   AND br.Status = N'Đang mượn' AND br.DueDate < CAST(GETDATE() AS DATE)))
                    ORDER BY CASE WHEN br.Status = N'Đang mượn' THEN 0 ELSE 1 END, br.RecordId DESC";

                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 200).Value = DbValue(keyword);
                cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value =
                    string.IsNullOrWhiteSpace(statusFilter) ? "Tất cả" : statusFilter;

                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(Map(reader));
                }
                return list;
            });
        }

        public BorrowRecordDTO? GetById(int recordId)
        {
            return Execute("Borrow.GetById", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(SelectSql + " WHERE br.RecordId = @RecordId", conn);
                cmd.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                conn.Open();
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? Map(reader) : null;
            });
        }

        /// <summary>
        /// Tạo phiếu mượn trong 1 Transaction: khóa dòng sách (UPDLOCK) → kiểm tra còn sách → thêm phiếu → tăng BorrowedQuantity.
        /// </summary>
        public bool CreateBorrowTicket(int readerId, int bookId, DateTime dueDate, string? notes,
                                       out int newRecordId, out string errorMessage)
        {
            var result = Execute("Borrow.Create", () =>
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    // Bước 1: khóa và kiểm tra số lượng còn lại
                    using (var cmdCheck = new SqlCommand(
                        "SELECT TotalQuantity - BorrowedQuantity FROM Books WITH (UPDLOCK, ROWLOCK) WHERE BookId = @BookId",
                        conn, trans))
                    {
                        cmdCheck.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        object? available = cmdCheck.ExecuteScalar();
                        if (available == null || available == DBNull.Value)
                        {
                            BookDAL.SafeRollback(trans);
                            return (false, 0, "Không tìm thấy sách cần mượn!");
                        }
                        if (Convert.ToInt32(available) <= 0)
                        {
                            BookDAL.SafeRollback(trans);
                            return (false, 0, "Sách này đã hết, không thể cho mượn!");
                        }
                    }

                    // Bước 2: thêm phiếu mượn
                    int recordId;
                    using (var cmdInsert = new SqlCommand(@"
                        INSERT INTO BorrowRecords (ReaderId, BookId, BorrowDate, DueDate, Status, Notes)
                        VALUES (@ReaderId, @BookId, CAST(GETDATE() AS DATE), @DueDate, N'Đang mượn', @Notes);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, trans))
                    {
                        cmdInsert.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                        cmdInsert.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        cmdInsert.Parameters.Add("@DueDate", SqlDbType.Date).Value = dueDate.Date;
                        cmdInsert.Parameters.Add("@Notes", SqlDbType.NVarChar, 255).Value = DbValue(notes);
                        recordId = Convert.ToInt32(cmdInsert.ExecuteScalar());
                    }

                    // Bước 3: tăng số lượng đang mượn
                    using (var cmdUpdate = new SqlCommand(
                        "UPDATE Books SET BorrowedQuantity = BorrowedQuantity + 1 WHERE BookId = @BookId", conn, trans))
                    {
                        cmdUpdate.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        cmdUpdate.ExecuteNonQuery();
                    }

                    trans.Commit();
                    Logger.LogDebug(DalLogEvents.DbTransaction,
                        "Commit transaction tạo phiếu mượn RecordId={RecordId}", recordId);
                    return (true, recordId, string.Empty);
                }
                catch
                {
                    BookDAL.SafeRollback(trans);
                    Logger.LogWarning(DalLogEvents.DbTransaction,
                        "Rollback transaction tạo phiếu mượn ReaderId={ReaderId}, BookId={BookId}", readerId, bookId);
                    throw;
                }
            });

            newRecordId = result.Item2;
            errorMessage = result.Item3;
            return result.Item1;
        }

        /// <summary>Ghi nhận trả sách trong 1 Transaction: cập nhật phiếu → giảm BorrowedQuantity.</summary>
        public bool ReturnBook(int recordId, out string errorMessage)
        {
            var result = Execute("Borrow.Return", () =>
            {
                using var conn = DbHelper.GetConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    int bookId;
                    using (var cmdGet = new SqlCommand(
                        "SELECT BookId FROM BorrowRecords WITH (UPDLOCK, ROWLOCK) WHERE RecordId = @RecordId AND Status = N'Đang mượn'",
                        conn, trans))
                    {
                        cmdGet.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                        object? value = cmdGet.ExecuteScalar();
                        if (value == null || value == DBNull.Value)
                        {
                            BookDAL.SafeRollback(trans);
                            return (false, "Phiếu mượn đã được trả trước đó hoặc không tồn tại!");
                        }
                        bookId = Convert.ToInt32(value);
                    }

                    using (var cmdTicket = new SqlCommand(@"
                        UPDATE BorrowRecords SET ReturnDate = CAST(GETDATE() AS DATE), Status = N'Đã trả'
                        WHERE RecordId = @RecordId", conn, trans))
                    {
                        cmdTicket.Parameters.Add("@RecordId", SqlDbType.Int).Value = recordId;
                        cmdTicket.ExecuteNonQuery();
                    }

                    using (var cmdBook = new SqlCommand(@"
                        UPDATE Books SET BorrowedQuantity = CASE WHEN BorrowedQuantity > 0 THEN BorrowedQuantity - 1 ELSE 0 END
                        WHERE BookId = @BookId", conn, trans))
                    {
                        cmdBook.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                        cmdBook.ExecuteNonQuery();
                    }

                    trans.Commit();
                    Logger.LogDebug(DalLogEvents.DbTransaction,
                        "Commit transaction trả sách RecordId={RecordId}, BookId={BookId}", recordId, bookId);
                    return (true, string.Empty);
                }
                catch
                {
                    BookDAL.SafeRollback(trans);
                    throw;
                }
            });

            errorMessage = result.Item2;
            return result.Item1;
        }

        public int CountActiveByReader(int readerId) => ScalarInt("Borrow.CountActiveByReader",
            "SELECT COUNT(*) FROM BorrowRecords WHERE ReaderId = @Id AND Status = N'Đang mượn'", readerId);

        public int CountOverdueByReader(int readerId) => ScalarInt("Borrow.CountOverdueByReader",
            "SELECT COUNT(*) FROM BorrowRecords WHERE ReaderId = @Id AND Status = N'Đang mượn' AND DueDate < CAST(GETDATE() AS DATE)",
            readerId);

        public bool HasActiveBorrow(int readerId, int bookId)
        {
            return Execute("Borrow.HasActiveBorrow", () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM BorrowRecords WHERE ReaderId = @ReaderId AND BookId = @BookId AND Status = N'Đang mượn'",
                    conn);
                cmd.Parameters.Add("@ReaderId", SqlDbType.Int).Value = readerId;
                cmd.Parameters.Add("@BookId", SqlDbType.Int).Value = bookId;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            });
        }

        public DashboardStatsDTO GetDashboardStats()
        {
            return Execute("Borrow.GetDashboardStats", () =>
            {
                string sql = @"
                    SELECT
                        (SELECT COUNT(*) FROM Books)                                        AS TotalTitles,
                        (SELECT ISNULL(SUM(TotalQuantity), 0) FROM Books)                   AS TotalCopies,
                        (SELECT ISNULL(SUM(BorrowedQuantity), 0) FROM Books)                AS BorrowedCopies,
                        (SELECT COUNT(*) FROM Readers)                                      AS TotalReaders,
                        (SELECT COUNT(*) FROM BorrowRecords WHERE Status = N'Đang mượn')    AS ActiveTickets,
                        (SELECT COUNT(*) FROM BorrowRecords WHERE Status = N'Đang mượn'
                                                             AND DueDate < CAST(GETDATE() AS DATE)) AS OverdueTickets,
                        (SELECT COUNT(*) FROM BorrowRecords WHERE Status = N'Đã trả')       AS ReturnedTickets";

                var stats = new DashboardStatsDTO();
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    stats.TotalBookTitles = Convert.ToInt32(reader["TotalTitles"]);
                    stats.TotalCopies = Convert.ToInt32(reader["TotalCopies"]);
                    stats.BorrowedCopies = Convert.ToInt32(reader["BorrowedCopies"]);
                    stats.AvailableCopies = Math.Max(0, stats.TotalCopies - stats.BorrowedCopies);
                    stats.TotalReaders = Convert.ToInt32(reader["TotalReaders"]);
                    stats.ActiveTickets = Convert.ToInt32(reader["ActiveTickets"]);
                    stats.OverdueTickets = Convert.ToInt32(reader["OverdueTickets"]);
                    stats.ReturnedTickets = Convert.ToInt32(reader["ReturnedTickets"]);
                }
                return stats;
            });
        }

        public List<CategoryStatDTO> GetCategoryStats()
        {
            return Execute("Borrow.GetCategoryStats", () =>
            {
                var list = new List<CategoryStatDTO>();
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(@"
                    SELECT Category, COUNT(*) AS TitleCount, ISNULL(SUM(TotalQuantity), 0) AS TotalCopies
                    FROM Books GROUP BY Category ORDER BY TotalCopies DESC", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new CategoryStatDTO
                    {
                        Category = reader["Category"].ToString() ?? string.Empty,
                        TitleCount = Convert.ToInt32(reader["TitleCount"]),
                        TotalCopies = Convert.ToInt32(reader["TotalCopies"])
                    });
                }
                return list;
            });
        }

        public List<TopBookDTO> GetTopBooks(int top)
        {
            return Execute("Borrow.GetTopBooks", () =>
            {
                var list = new List<TopBookDTO>();
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(@"
                    SELECT TOP (@Top) b.Title, COUNT(*) AS BorrowCount
                    FROM BorrowRecords br INNER JOIN Books b ON b.BookId = br.BookId
                    GROUP BY b.Title ORDER BY BorrowCount DESC, b.Title", conn);
                cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new TopBookDTO
                    {
                        Title = reader["Title"].ToString() ?? string.Empty,
                        BorrowCount = Convert.ToInt32(reader["BorrowCount"])
                    });
                }
                return list;
            });
        }

        public Dictionary<DateTime, int> GetBorrowCountByDay(int days)
        {
            return Execute("Borrow.GetBorrowCountByDay", () =>
            {
                var result = new Dictionary<DateTime, int>();
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(@"
                    SELECT BorrowDate, COUNT(*) AS Cnt
                    FROM BorrowRecords
                    WHERE BorrowDate >= DATEADD(DAY, -@Days + 1, CAST(GETDATE() AS DATE))
                    GROUP BY BorrowDate", conn);
                cmd.Parameters.Add("@Days", SqlDbType.Int).Value = days;
                conn.Open();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result[Convert.ToDateTime(reader["BorrowDate"]).Date] = Convert.ToInt32(reader["Cnt"]);
                }
                return result;
            });
        }

        private int ScalarInt(string operation, string sql, int id)
        {
            return Execute(operation, () =>
            {
                using var conn = DbHelper.GetConnection();
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            });
        }

        private static BorrowRecordDTO Map(SqlDataReader reader)
        {
            return new BorrowRecordDTO
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
                Status = reader["Status"].ToString() ?? BorrowRecordDTO.StatusBorrowing,
                Notes = reader["Notes"] == DBNull.Value ? null : reader["Notes"].ToString()
            };
        }
    }
}
