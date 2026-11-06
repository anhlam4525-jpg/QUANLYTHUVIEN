using Microsoft.Data.SqlClient;

namespace LibraryManagement.DAL
{
    public static class DbHelper
    {
        /// <summary>
        /// Chuỗi kết nối mặc định (SQL Server LocalDB đi kèm Visual Studio).
        /// Có thể ghi đè bằng file appsettings.json ở thư mục chạy chương trình (xem Program.cs).
        /// Ví dụ SQL Server Express: Server=.\SQLEXPRESS;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;
        /// </summary>
        public static string ConnectionString { get; set; } =
            "Server=(localdb)\\MSSQLLocalDB;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=8;";

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static bool TestConnection(out string message)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                message = $"Kết nối thành công tới {conn.DataSource} / {conn.Database}";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        /// <summary>Tên máy chủ + CSDL để hiển thị trên thanh trạng thái (không chứa mật khẩu).</summary>
        public static string DescribeConnection()
        {
            try
            {
                var b = new SqlConnectionStringBuilder(ConnectionString);
                return $"{b.InitialCatalog} @ {b.DataSource}";
            }
            catch
            {
                return "LibraryDB";
            }
        }
    }
}
