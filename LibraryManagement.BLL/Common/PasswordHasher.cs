using System.Security.Cryptography;
using System.Text;

namespace LibraryManagement.BLL.Common
{
    /// <summary>Băm mật khẩu bằng SHA-256 (đủ cho phạm vi đồ án; hệ thống thật nên dùng PBKDF2/BCrypt có salt).</summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty));
            return Convert.ToHexString(bytes); // 64 ký tự HEX viết hoa
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;
            return string.Equals(Hash(password), storedHash.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
