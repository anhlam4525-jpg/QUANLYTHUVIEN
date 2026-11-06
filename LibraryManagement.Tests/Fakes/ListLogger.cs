using Microsoft.Extensions.Logging;

namespace LibraryManagement.Tests.Fakes
{
    /// <summary>Logger ghi lại các dòng log vào danh sách để kiểm tra trong Unit Test.</summary>
    public class ListLogger : ILogger
    {
        public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, eventId, formatter(state, exception), exception));
        }

        public bool Has(LogLevel level, int eventId) => Entries.Any(e => e.Level == level && e.EventId.Id == eventId);
    }
}
