using LibraryManagement.DTO;

namespace LibraryManagement.DAL.Interfaces
{
    public interface IBorrowRepository
    {
        /// <param name="statusFilter">"Tất cả" | "Đang mượn" | "Quá hạn" | "Đã trả"</param>
        List<BorrowRecordDTO> Search(string keyword, string statusFilter);
        BorrowRecordDTO? GetById(int recordId);
        bool CreateBorrowTicket(int readerId, int bookId, DateTime dueDate, string? notes, out int newRecordId, out string errorMessage);
        bool ReturnBook(int recordId, out string errorMessage);
        int CountActiveByReader(int readerId);
        int CountOverdueByReader(int readerId);
        bool HasActiveBorrow(int readerId, int bookId);
        DashboardStatsDTO GetDashboardStats();
        List<CategoryStatDTO> GetCategoryStats();
        List<TopBookDTO> GetTopBooks(int top);
        /// <summary>Số phiếu mượn được tạo mỗi ngày trong <paramref name="days"/> ngày gần nhất.</summary>
        Dictionary<DateTime, int> GetBorrowCountByDay(int days);
    }
}
