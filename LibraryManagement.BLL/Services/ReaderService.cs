using LibraryManagement.BLL.Validators;
using LibraryManagement.DAL;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class ReaderService
    {
        private readonly ReaderDAL _readerDAL = new();
        private readonly ReaderValidator _validator = new();

        public List<ReaderDTO> GetAllReaders() => _readerDAL.GetAll();

        public List<ReaderDTO> SearchReaders(string keyword) => _readerDAL.Search(keyword);

        public (bool Success, string Message, int NewId) AddReader(ReaderDTO reader)
        {
            if (!_validator.IsValidReaderCode(reader.ReaderCode))
                return (false, "Mã độc giả phải từ 3 đến 20 ký tự (chữ và số)!", 0);

            if (!_validator.IsValidFullName(reader.FullName))
                return (false, "Họ và tên độc giả không được để trống!", 0);

            if (string.IsNullOrWhiteSpace(reader.Class))
                return (false, "Lớp không được để trống!", 0);

            if (!_validator.IsValidEmail(reader.Email))
                return (false, "Địa chỉ Email không hợp lệ!", 0);

            if (!_validator.IsValidPhoneNumber(reader.PhoneNumber))
                return (false, "Số điện thoại phải từ 10 đến 11 chữ số!", 0);

            try
            {
                int id = _readerDAL.Insert(reader);
                return (true, "Thêm độc giả mới thành công!", id);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi thêm độc giả (có thể trùng Mã hoặc Email): {ex.Message}", 0);
            }
        }

        public (bool Success, string Message) UpdateReader(ReaderDTO reader)
        {
            if (reader.ReaderId <= 0)
                return (false, "Mã độc giả không hợp lệ!");

            if (!_validator.IsValidFullName(reader.FullName))
                return (false, "Họ tên độc giả không được rỗng!");

            if (!_validator.IsValidEmail(reader.Email))
                return (false, "Email không hợp lệ!");

            try
            {
                bool ok = _readerDAL.Update(reader);
                return ok ? (true, "Cập nhật thông tin độc giả thành công!") : (false, "Không tìm thấy độc giả để cập nhật!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật độc giả: {ex.Message}");
            }
        }

        public (bool Success, string Message) DeleteReader(int readerId)
        {
            try
            {
                bool ok = _readerDAL.Delete(readerId);
                return ok ? (true, "Xóa độc giả thành công!") : (false, "Không thể xóa độc giả!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xóa độc giả: {ex.Message}");
            }
        }
    }
}
