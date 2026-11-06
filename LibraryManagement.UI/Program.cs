using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using LibraryManagement.BLL.Common;
using LibraryManagement.DAL;
using LibraryManagement.UI.Controls;
using LibraryManagement.UI.Forms;
using LibraryManagement.UI.Logging;

namespace LibraryManagement.UI
{
    internal static class Program
    {
        private static ILogger _logger = null!;

        [STAThread]
        private static void Main(string[] args)
        {
            // Chạy với tham số --console (profile thứ 2 trong Visual Studio) để mở cửa sổ Console xem log trực tiếp.
            // Phải mở Console TRƯỚC khi tạo LoggerFactory.
            if (args.Any(a => a.Equals("--console", StringComparison.OrdinalIgnoreCase)))
            {
                NativeMethods.AllocConsole();
                Console.OutputEncoding = Encoding.UTF8;
                Console.Title = "Smart Library - Console Log";
            }

            ApplicationConfiguration.Initialize();
            Theme.InitDpi();

            _logger = AppLogging.CreateLogger("LibraryManagement.UI.Program");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => HandleFatal(e.Exception, isCritical: false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleFatal(e.ExceptionObject as Exception ?? new Exception("Lỗi không xác định"), isCritical: true);

            LoadConnectionString();
            _logger.LogInformation(UiLogEvents.AppStarted,
                "Ứng dụng khởi động. CSDL={Database}, Log={LogFolder}", DbHelper.DescribeConnection(), AppLogging.LogFolder);

            if (!EnsureDatabase()) return;

            var services = new AppServices(AppLogging.Factory);

            // Vòng lặp Đăng nhập → Màn hình chính → (Đăng xuất) → Đăng nhập lại
            while (true)
            {
                using (var login = new frmLogin(services))
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                }

                bool logout;
                // Scope gắn Username + SessionId vào mọi dòng log trong phiên làm việc
                using (_logger.BeginScope("User={Username} Session={SessionId}",
                           AppSession.Username, Guid.NewGuid().ToString("N")[..8]))
                {
                    using var main = new frmMain(services);
                    Application.Run(main);
                    logout = main.LogoutRequested;
                }

                if (!logout) break;
                services.Auth.Logout();
            }

            _logger.LogInformation(UiLogEvents.AppExit, "Ứng dụng đóng.");
            AppLogging.Factory.Dispose(); // flush Console logger
        }

        /// <summary>Đọc chuỗi kết nối từ appsettings.json (nếu có) để đổi máy chủ SQL không cần biên dịch lại.</summary>
        private static void LoadConnectionString()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                if (!File.Exists(path)) return;

                using var doc = JsonDocument.Parse(File.ReadAllText(path),
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("LibraryDB", out var value) &&
                    !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    DbHelper.ConnectionString = value.GetString()!;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không đọc được appsettings.json, dùng chuỗi kết nối mặc định.");
            }
        }

        /// <summary>Kiểm tra kết nối CSDL; nếu lỗi ghi log Critical và cho phép Thử lại / Thoát.</summary>
        private static bool EnsureDatabase()
        {
            while (true)
            {
                if (DbHelper.TestConnection(out string message))
                {
                    _logger.LogInformation(UiLogEvents.AppStarted, "{Message}", message);
                    return true;
                }

                _logger.LogCritical(UiLogEvents.DbUnavailable,
                    "Không thể kết nối CSDL {Database}: {Error}", DbHelper.DescribeConnection(), message);

                bool retry = frmConfirm.AskRetry(null, "Không kết nối được SQL Server",
                    "Ứng dụng không kết nối được cơ sở dữ liệu " + DbHelper.DescribeConnection() + ".\n\n" +
                    "• Hãy chạy file LibraryDB.sql trong SSMS để tạo CSDL.\n" +
                    "• Kiểm tra SQL Server / LocalDB đang chạy.\n" +
                    "• Sửa chuỗi kết nối trong appsettings.json nếu dùng SQL Server Express.\n\n" +
                    "Chi tiết: " + message);
                if (!retry) return false;
            }
        }

        private static void HandleFatal(Exception ex, bool isCritical)
        {
            if (isCritical)
                _logger.LogCritical(UiLogEvents.Unhandled, ex, "Lỗi nghiêm trọng không được xử lý");
            else
                _logger.LogError(UiLogEvents.Unhandled, ex, "Lỗi không được xử lý trên giao diện");

            try
            {
                frmConfirm.Alert(null, "Đã xảy ra lỗi",
                    "Chương trình gặp lỗi ngoài dự kiến và đã ghi vào file log.\n\n" + ex.Message, ToastKind.Error);
            }
            catch
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
