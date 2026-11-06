using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Common;
using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.Tests.Fakes;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử Đăng nhập + quy tắc bảo mật log (không ghi mật khẩu).</summary>
    [TestClass]
    public class AuthServiceTests
    {
        private const string Password = "admin123";
        private FakeUserRepository _repo = null!;
        private ListLogger _log = null!;
        private AuthService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            AppSession.CurrentUser = null;
            _repo = new FakeUserRepository();
            _repo.Users.Add(new UserDTO
            {
                UserId = 1,
                Username = "admin",
                PasswordHash = PasswordHasher.Hash(Password),
                FullName = "Quản trị viên",
                Role = "Quản trị"
            });
            _log = new ListLogger();
            _service = new AuthService(_repo, _log);
        }

        [TestCleanup]
        public void Cleanup() => AppSession.CurrentUser = null;

        [TestMethod]
        public void Login_CorrectCredentials_SucceedsAndSetsSession()
        {
            var result = _service.Login("admin", Password);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(AppSession.IsLoggedIn);
            Assert.AreEqual("admin", AppSession.Username);
            Assert.IsTrue(_log.Has(LogLevel.Information, 1301), "Login thành công phải ghi Information.");
        }

        [TestMethod]
        public void Login_UsernameCaseInsensitive_Succeeds()
            => Assert.IsTrue(_service.Login("ADMIN", Password).Success);

        [TestMethod]
        public void Login_WrongPassword_FailsAndLogsWarning()
        {
            var result = _service.Login("admin", "sai-mat-khau");

            Assert.IsFalse(result.Success);
            Assert.IsFalse(AppSession.IsLoggedIn);
            Assert.IsTrue(_log.Has(LogLevel.Warning, 1302), "Login thất bại phải ghi Warning.");
        }

        [TestMethod]
        public void Login_UnknownUser_Fails()
            => Assert.IsFalse(_service.Login("khongton", Password).Success);

        [TestMethod]
        public void Login_EmptyInput_Fails()
        {
            Assert.IsFalse(_service.Login("", "").Success);
            Assert.IsFalse(_service.Login(null, null).Success);
        }

        [TestMethod]
        public void Login_InactiveUser_Fails()
        {
            _repo.Users[0].IsActive = false;
            Assert.IsFalse(_service.Login("admin", Password).Success);
        }

        [TestMethod]
        public void Login_FiveWrongAttempts_LocksEvenWithCorrectPassword()
        {
            for (int i = 0; i < AuthService.MaxFailedAttempts; i++)
            {
                _service.Login("admin", "sai");
            }

            Assert.IsTrue(_service.IsLocked);
            Assert.IsFalse(_service.Login("admin", Password).Success);
        }

        [TestMethod]
        public void Login_DatabaseDown_FailsAndLogsError()
        {
            _repo.ThrowOnRead = true;

            var result = _service.Login("admin", Password);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(_log.Entries.Any(e => e.Level == LogLevel.Error && e.Exception != null));
        }

        [TestMethod]
        public void Login_NeverWritesPasswordToLog()
        {
            _service.Login("admin", "MatKhauBiMat!");
            _service.Login("admin", Password);

            Assert.IsFalse(_log.Entries.Any(e => e.Message.Contains("MatKhauBiMat!") || e.Message.Contains(Password)),
                "Không được ghi mật khẩu vào log.");
        }

        [TestMethod]
        public void Logout_ClearsSession()
        {
            _service.Login("admin", Password);
            _service.Logout();
            Assert.IsFalse(AppSession.IsLoggedIn);
        }

        [TestMethod]
        public void PasswordHasher_SameInput_SameHash_DifferentInput_DifferentHash()
        {
            Assert.AreEqual(PasswordHasher.Hash("abc"), PasswordHasher.Hash("abc"));
            Assert.AreNotEqual(PasswordHasher.Hash("abc"), PasswordHasher.Hash("abd"));
            Assert.AreEqual(64, PasswordHasher.Hash("abc").Length);
        }

        [TestMethod]
        public void PasswordHasher_MatchesSeedHashInSqlScript()
        {
            // Hash của "admin123" được dùng trong LibraryDB.sql
            Assert.AreEqual("240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9",
                PasswordHasher.Hash("admin123"));
        }
    }
}
