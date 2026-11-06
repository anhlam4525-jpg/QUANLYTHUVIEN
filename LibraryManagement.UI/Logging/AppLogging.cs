using Microsoft.Extensions.Logging;

namespace LibraryManagement.UI.Logging
{
    /// <summary>
    /// Cấu hình Logging Pipeline cho toàn ứng dụng (Microsoft.Extensions.Logging):
    ///  - Console Provider (SimpleConsole, có Scope) – xem được khi chạy với tham số --console
    ///  - Debug Provider – hiện trong cửa sổ Output của Visual Studio
    ///  - FileLoggerProvider tự viết – ghi ra Logs/LogData_ddMMyyyy.log (append, theo ngày)
    /// Filter theo tầng: UI ≥ Information, BLL ≥ Information, DAL ≥ Debug, Microsoft ≥ Warning.
    /// </summary>
    public static class AppLogging
    {
        public static string LogFolder { get; } = Path.Combine(AppContext.BaseDirectory, "Logs");

        public static ILoggerFactory Factory { get; } = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });
            builder.AddDebug();
            builder.AddProvider(new FileLoggerProvider(LogFolder));

            builder.AddFilter("LibraryManagement.UI", LogLevel.Information);
            builder.AddFilter("LibraryManagement.BLL", LogLevel.Information);
            builder.AddFilter("LibraryManagement.DAL", LogLevel.Debug);
            builder.AddFilter("Microsoft", LogLevel.Warning);
        });

        public static ILogger CreateLogger(string category) => Factory.CreateLogger(category);
    }

    /// <summary>EventId cho log tầng giao diện (3xxx).</summary>
    public static class UiLogEvents
    {
        public static readonly EventId AppStarted = new(3001, "AppStarted");
        public static readonly EventId AppExit = new(3002, "AppExit");
        public static readonly EventId Navigate = new(3003, "Navigate");
        public static readonly EventId UserAction = new(3004, "UserAction");
        public static readonly EventId UiError = new(3900, "UiError");
        public static readonly EventId DbUnavailable = new(3998, "DbUnavailable");
        public static readonly EventId Unhandled = new(3999, "UnhandledException");
    }
}
