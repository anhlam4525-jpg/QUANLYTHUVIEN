using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LibraryManagement.BLL.Common;
using LibraryManagement.DAL;
using LibraryManagement.DAL.Interfaces;
using LibraryManagement.DTO;

namespace LibraryManagement.BLL.Services
{
    public class AuthService
    {
        public const int MaxFailedAttempts = 5;

        private readonly IUserRepository _repo;
        private readonly ILogger _logger;
        private int _failedAttempts;

        public AuthService() : this(new UserDAL()) { }

        public AuthService(IUserRepository repo, ILogger? logger = null)
        {
            _repo = repo;
            _logger = logger ?? NullLogger.Instance;
        }

        public int FailedAttempts => _failedAttempts;
        public bool IsLocked => _failedAttempts >= MaxFailedAttempts;

        /// <summary>Đăng nhập. Mật khẩu KHÔNG bao giờ được ghi vào log.</summary>
        public OperationResult Login(string? username, string? password)
        {
            if (IsLocked)
            {
                _logger.LogWarning(BllLogEvents.LoginFailed,
                    "Đăng nhập bị khóa sau {Attempts} lần sai. Username={Username}", _failedAttempts, username);
                return OperationResult.Fail($"Bạn đã nhập sai {MaxFailedAttempts} lần. Vui lòng khởi động lại ứng dụng.");
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning(BllLogEvents.LoginFailed, "Đăng nhập thiếu thông tin. Username={Username}", username);
                return OperationResult.Fail("Vui lòng nhập tên đăng nhập và mật khẩu!");
            }

            try
            {
                var user = _repo.GetByUsername(username.Trim());
                if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
                {
                    _failedAttempts++;
                    _logger.LogWarning(BllLogEvents.LoginFailed,
                        "Đăng nhập thất bại Username={Username}, Attempt={Attempt}", username.Trim(), _failedAttempts);
                    int left = MaxFailedAttempts - _failedAttempts;
                    return OperationResult.Fail(left > 0
                        ? $"Sai tên đăng nhập hoặc mật khẩu! (còn {left} lần thử)"
                        : "Sai tên đăng nhập hoặc mật khẩu! Tài khoản đã bị tạm khóa.");
                }

                _failedAttempts = 0;
                AppSession.CurrentUser = user;
                _logger.LogInformation(BllLogEvents.LoginSuccess,
                    "Đăng nhập thành công Username={Username}, Role={Role}", user.Username, user.Role);
                return OperationResult.Ok($"Xin chào {user.FullName}!", user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(BllLogEvents.BusinessError, ex, "Lỗi khi đăng nhập Username={Username}", username);
                return OperationResult.Fail("Không kết nối được cơ sở dữ liệu. Kiểm tra SQL Server và chuỗi kết nối.");
            }
        }

        public void Logout()
        {
            if (AppSession.CurrentUser != null)
            {
                _logger.LogInformation(BllLogEvents.Logout, "Đăng xuất Username={Username}", AppSession.Username);
            }
            AppSession.CurrentUser = null;
        }
    }
}
