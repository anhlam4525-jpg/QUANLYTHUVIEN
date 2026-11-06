namespace LibraryManagement.BLL.Common
{
    /// <summary>Kết quả trả về của một nghiệp vụ ghi dữ liệu (Thêm/Sửa/Xóa/Mượn/Trả/Đăng nhập).</summary>
    public class OperationResult
    {
        public bool Success { get; }
        public string Message { get; }
        /// <summary>Mã bản ghi mới tạo (nếu có).</summary>
        public int Id { get; }

        private OperationResult(bool success, string message, int id)
        {
            Success = success;
            Message = message;
            Id = id;
        }

        public static OperationResult Ok(string message, int id = 0) => new(true, message, id);
        public static OperationResult Fail(string message) => new(false, message, 0);

        public override string ToString() => $"{(Success ? "OK" : "FAIL")}: {Message}";
    }
}
