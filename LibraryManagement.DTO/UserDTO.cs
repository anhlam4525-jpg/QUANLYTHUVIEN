namespace LibraryManagement.DTO
{
    public class UserDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        /// <summary>Chuỗi băm SHA-256 (64 ký tự hex). Không bao giờ lưu/ghi log mật khẩu gốc.</summary>
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Thủ thư";
        public bool IsActive { get; set; } = true;
    }
}
