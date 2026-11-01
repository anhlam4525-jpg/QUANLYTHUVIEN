using Microsoft.Extensions.Logging;

namespace LibraryManagement.UI.Logging
{
    public static class AppLogging
    {
        public static ILoggerFactory Factory { get; } = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.AddProvider(new FileLoggerProvider("Logs"));
            builder.SetMinimumLevel(LogLevel.Trace);

            // Cấu hình Filter theo kiến trúc nhiều tầng (Buổi 4 & Buổi 5)
            builder.AddFilter("LibraryManagement.UI", LogLevel.Information);
            builder.AddFilter("LibraryManagement.BLL", LogLevel.Information);
            builder.AddFilter("LibraryManagement.DAL", LogLevel.Debug);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        public static ILogger CreateLogger<T>() => Factory.CreateLogger<T>();
        public static ILogger CreateLogger(string category) => Factory.CreateLogger(category);
    }
}
