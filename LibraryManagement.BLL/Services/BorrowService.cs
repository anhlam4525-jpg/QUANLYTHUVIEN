using LibraryManagement.DAL;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class BorrowService
    {
        private readonly BorrowDAL _borrowDAL = new();

        public List<BorrowRecordDTO> GetAllRecords() => _borrowDAL.GetAll();

        public DashboardStatsDTO GetDashboardStats() => _borrowDAL.GetDashboardStats();

        public (bool Success, string Message) BorrowBook(int readerId, int bookId, DateTime dueDate)
        {
            if (readerId <= 0)
                return (false, "Vui lòng chọn độc giả hợp lệ!");

            if (bookId <= 0)
                return (false, "Vui lòng chọn sách cần mượn!");

            if (dueDate.Date < DateTime.Today)
                return (false, "Hạn trả sách không được nhỏ hơn ngày mượn (hôm nay)!");

            bool ok = _borrowDAL.CreateBorrowTicket(readerId, bookId, dueDate, out string err);
            return ok ? (true, "Tạo phiếu mượn sách thành công!") : (false, err);
        }

        public (bool Success, string Message) ReturnBook(int recordId, int bookId)
        {
            if (recordId <= 0 || bookId <= 0)
                return (false, "Thông tin phiếu mượn không hợp lệ!");

            bool ok = _borrowDAL.ReturnBook(recordId, bookId, out string err);
            return ok ? (true, "Ghi nhận trả sách thành công! Đã khôi phục số lượng sách trong kho.") : (false, err);
        }
    }
}
