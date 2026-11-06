using Microsoft.Extensions.Logging;

namespace LibraryManagement.DAL
{
    /// <summary>EventId dùng cho log tầng DAL (2xxx).</summary>
    public static class DalLogEvents
    {
        public static readonly EventId DbQuery = new(2001, "DbQuery");
        public static readonly EventId DbCommand = new(2002, "DbCommand");
        public static readonly EventId DbTransaction = new(2003, "DbTransaction");
        public static readonly EventId DbError = new(2900, "DbError");
    }
}
