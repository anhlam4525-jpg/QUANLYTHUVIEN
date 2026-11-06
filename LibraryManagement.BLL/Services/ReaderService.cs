using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LibraryManagement.BLL.Common;
using LibraryManagement.BLL.Validators;
using LibraryManagement.DAL;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class ReaderService
    {
        private readonly IReaderRepository _repo;
        private readonly ILogger _logger;
        private readonly ReaderValidator _validator = new();

        public ReaderService() : this(new ReaderDAL()) { }

        public ReaderService(IReaderRepository repo, ILogger? logger = null)
        {
            _repo = repo;
            _logger = logger ?? NullLogger.Instance;
        }

        public List<ReaderDTO> GetAllReaders() => _repo.Search(string.Empty);

        public List<ReaderDTO> SearchReaders(string? keyword)
        {
            string kw = keyword?.Trim() ?? string.Empty;
            var result = _repo.Search(kw);
            if (kw.Length > 0)
            {
                _logger.LogInformation(BllLogEvents.ReaderSearched,
                    "Tìm độc giả Keyword={Keyword} → {Count} kết quả", kw, result.Count);
            }
            return result;
        }

        public ReaderDTO? GetById(int readerId) => readerId > 0 ? _repo.GetById(readerId) : null;

        public OperationResult AddReader(ReaderDTO reader)
        {
            if (!_validator.IsValidReaderCode(reader.ReaderCode))
                return Invalid("AddReader", "Mã độc giả gồm 3–20 ký tự chữ hoặc số, không dấu, không khoảng trắng!");

            string? error = ValidateCommon(reader);
            if (error != null) return Invalid("AddReader", error);

            try
            {
                if (_repo.CodeExists(reader.ReaderCode, 0))
                    return Invalid("AddReader", $"Mã độc giả \"{reader.ReaderCode.Trim().ToUpperInvariant()}\" đã tồn tại!");
                if (_repo.EmailExists(reader.Email, 0))
                    return Invalid("AddReader", "Email này đã được dùng cho độc giả khác!");

                int id = _repo.Insert(reader);
                _logger.LogInformation(BllLogEvents.ReaderInserted,
                    "Thêm độc giả thành công ReaderId={ReaderId}, ReaderCode={ReaderCode}", id, reader.ReaderCode);
                return OperationResult.Ok($"Đã thêm độc giả {reader.FullName.Trim()}.", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex,
                    "Thêm độc giả thất bại ReaderCode={ReaderCode}", reader.ReaderCode);
                return OperationResult.Fail("Không thể thêm độc giả do lỗi cơ sở dữ liệu.");
            }
        }

        public OperationResult UpdateReader(ReaderDTO reader)
        {
            if (reader.ReaderId <= 0) return Invalid("UpdateReader", "Vui lòng chọn độc giả cần cập nhật!");

            string? error = ValidateCommon(reader);
            if (error != null) return Invalid("UpdateReader", error);

            try
            {
                if (_repo.EmailExists(reader.Email, reader.ReaderId))
                    return Invalid("UpdateReader", "Email này đã được dùng cho độc giả khác!");

                bool ok = _repo.Update(reader);
                if (!ok) return Invalid("UpdateReader", "Không tìm thấy độc giả cần cập nhật!");

                _logger.LogInformation(BllLogEvents.ReaderUpdated,
                    "Cập nhật độc giả thành công ReaderId={ReaderId}, ReaderCode={ReaderCode}",
                    reader.ReaderId, reader.ReaderCode);
                return OperationResult.Ok("Đã cập nhật thông tin độc giả.", reader.ReaderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex,
                    "Cập nhật độc giả thất bại ReaderId={ReaderId}", reader.ReaderId);
                return OperationResult.Fail("Không thể cập nhật độc giả do lỗi cơ sở dữ liệu.");
            }
        }

        public OperationResult DeleteReader(int readerId)
        {
            if (readerId <= 0) return Invalid("DeleteReader", "Vui lòng chọn độc giả cần xóa!");

            try
            {
                int active = _repo.CountActiveBorrows(readerId);
                if (active > 0)
                    return Invalid("DeleteReader",
                        $"Không thể xóa: độc giả còn {active} cuốn sách chưa trả.");

                bool ok = _repo.Delete(readerId);
                if (!ok) return Invalid("DeleteReader", "Độc giả không tồn tại hoặc đã bị xóa.");

                _logger.LogInformation(BllLogEvents.ReaderDeleted, "Xóa độc giả thành công ReaderId={ReaderId}", readerId);
                return OperationResult.Ok("Đã xóa độc giả.", readerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Xóa độc giả thất bại ReaderId={ReaderId}", readerId);
                return OperationResult.Fail("Không thể xóa độc giả do lỗi cơ sở dữ liệu.");
            }
        }

        private string? ValidateCommon(ReaderDTO reader)
        {
            if (!_validator.IsValidFullName(reader.FullName))
                return "Họ tên độc giả phải từ 2 đến 100 ký tự!";
            if (!_validator.IsValidClass(reader.Class))
                return "Lớp gồm 2–20 ký tự chữ/số (VD: CNTT01)!";
            if (!_validator.IsValidEmail(reader.Email))
                return "Địa chỉ Email không hợp lệ (VD: ten@sv.edu.vn)!";
            if (!_validator.IsValidPhoneNumber(reader.PhoneNumber))
                return "Số điện thoại phải gồm 10–11 chữ số và bắt đầu bằng 0!";
            return null;
        }

        private OperationResult Invalid(string action, string message)
        {
            _logger.LogWarning(BllLogEvents.ValidationFailed, "{Action} bị từ chối: {Reason}", action, message);
            return OperationResult.Fail(message);
        }
    }
}
