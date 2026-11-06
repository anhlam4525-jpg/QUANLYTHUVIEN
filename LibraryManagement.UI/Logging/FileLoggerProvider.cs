using System.Text;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.UI.Logging
{
    /// <summary>
    /// File Logger Provider tự xây dựng (Bài 1.5 – Buổi 4): mỗi ngày một file Logs/LogData_ddMMyyyy.log,
    /// ghi nối tiếp (append), dùng lock để an toàn đa luồng, hỗ trợ Scope.
    /// </summary>
    [ProviderAlias("File")]
    public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly string _logFolder;
        private readonly object _fileLock = new();
        private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public FileLoggerProvider(string logFolder = "Logs")
        {
            _logFolder = logFolder;
            Directory.CreateDirectory(_logFolder);
        }

        public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

        internal IExternalScopeProvider ScopeProvider => _scopeProvider;

        internal void Write(string line)
        {
            string filePath = Path.Combine(_logFolder, $"LogData_{DateTime.Now:ddMMyyyy}.log");
            lock (_fileLock)
            {
                try
                {
                    File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (IOException)
                {
                    // File đang bị khóa bởi chương trình khác: bỏ qua dòng log, không làm hỏng ứng dụng
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        public void Dispose() { }
    }

    public sealed class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string categoryName, FileLoggerProvider provider)
        {
            _categoryName = categoryName;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _provider.ScopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var sb = new StringBuilder();
            sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("] ");
            sb.Append('[').Append(logLevel.ToString().ToUpperInvariant().PadRight(11)).Append("] ");
            sb.Append('[').Append(_categoryName).Append("] ");
            sb.Append("[EventId ").Append(eventId.Id);
            if (!string.IsNullOrEmpty(eventId.Name)) sb.Append(' ').Append(eventId.Name);
            sb.Append("] ");
            sb.Append(formatter(state, exception));

            // Scope (vd. Username, SessionId) để biết log thuộc phiên làm việc nào
            _provider.ScopeProvider.ForEachScope((scope, builder) =>
            {
                builder.Append(" | Scope: ").Append(scope);
            }, sb);

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append("    ").Append(exception.ToString().Replace(Environment.NewLine, Environment.NewLine + "    "));
            }

            _provider.Write(sb.ToString());
        }
    }
}
