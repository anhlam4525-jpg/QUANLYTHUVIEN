using Microsoft.Data.SqlClient;

namespace LibraryManagement.DAL
{
    public static class DbHelper
    {
        // Chuỗi kết nối SQL Server Local (hỗ trợ Windows Authentication)
        public static string ConnectionString { get; set; } = 
            "Server=.;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool TestConnection(out string message)
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                message = "Kết nối SQL Server thành công!";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }
    }
}
