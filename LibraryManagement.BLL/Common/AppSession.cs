using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Common
{
    /// <summary>Lưu thông tin người dùng đang đăng nhập trong phiên làm việc.</summary>
    public static class AppSession
    {
        public static UserDTO? CurrentUser { get; set; }
        public static string Username => CurrentUser?.Username ?? "anonymous";
        public static bool IsLoggedIn => CurrentUser != null;
    }
}
