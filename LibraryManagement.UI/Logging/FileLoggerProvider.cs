using System.Text;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.UI.Logging
{
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logFolder;
        private static readonly object _fileLock = new();

        public FileLoggerProvider(string logFolder = "Logs")
        {
            _logFolder = logFolder;
            if (!Directory.Exists(_logFolder))
            {
                Directory.CreateDirectory(_logFolder);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(categoryName, _logFolder, _fileLock);
        }

        public void Dispose() { }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFolder;
        private readonly object _lock;

        public FileLogger(string categoryName, string logFolder, object fileLock)
        {
            _categoryName = categoryName;
            _logFolder = logFolder;
            _lock = fileLock;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string message = formatter(state, exception);
            string fileName = $"LogData_{DateTime.Now:ddMMyyyy}.log";
            string filePath = Path.Combine(_logFolder, fileName);

            var sb = new StringBuilder();
            sb.Append($"[{timestamp}] [{logLevel.ToString().ToUpper(),-11}] [{_categoryName}] [EventId: {eventId.Id,4} - {eventId.Name,-15}] {message}");

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"   Exception Type : {exception.GetType().FullName}");
                sb.AppendLine();
                sb.Append($"   Exception Msg  : {exception.Message}");
                sb.AppendLine();
                sb.Append($"   Stack Trace    : {exception.StackTrace}");
            }

            lock (_lock)
            {
                File.AppendAllText(filePath, sb.ToString() + Environment.NewLine);
            }
        }
    }
}
