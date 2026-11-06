using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryManagement.DAL
{
    /// <summary>
    /// Lớp cơ sở cho các Repository: gom phần ghi log Debug (thời gian thực thi) và log Error (kèm Exception) vào một chỗ.
    /// </summary>
    public abstract class RepositoryBase
    {
        protected readonly ILogger Logger;

        protected RepositoryBase(ILogger? logger)
        {
            Logger = logger ?? NullLogger.Instance;
        }

        protected T Execute<T>(string operation, Func<T> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                T result = action();
                Logger.LogDebug(DalLogEvents.DbQuery,
                    "DAL {Operation} hoàn tất sau {ElapsedMs} ms", operation, sw.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(DalLogEvents.DbError, ex,
                    "DAL {Operation} thất bại sau {ElapsedMs} ms", operation, sw.ElapsedMilliseconds);
                throw;
            }
        }

        protected static object DbValue(string? value) =>
            string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
