using Microsoft.Extensions.Logging;

namespace LibraryManagement.BLL.Common
{
    /// <summary>EventId cho log nghiệp vụ tầng BLL (1xxx).</summary>
    public static class BllLogEvents
    {
        public static readonly EventId BookInserted = new(1001, "BookInserted");
        public static readonly EventId BookUpdated = new(1002, "BookUpdated");
        public static readonly EventId BookDeleted = new(1003, "BookDeleted");
        public static readonly EventId BookSearched = new(1004, "BookSearched");

        public static readonly EventId ReaderInserted = new(1101, "ReaderInserted");
        public static readonly EventId ReaderUpdated = new(1102, "ReaderUpdated");
        public static readonly EventId ReaderDeleted = new(1103, "ReaderDeleted");
        public static readonly EventId ReaderSearched = new(1104, "ReaderSearched");

        public static readonly EventId BorrowCreated = new(1201, "BorrowCreated");
        public static readonly EventId BookReturned = new(1202, "BookReturned");
        public static readonly EventId BorrowRejected = new(1203, "BorrowRejected");

        public static readonly EventId LoginSuccess = new(1301, "LoginSuccess");
        public static readonly EventId LoginFailed = new(1302, "LoginFailed");
        public static readonly EventId Logout = new(1303, "Logout");

        public static readonly EventId StatisticsLoaded = new(1401, "StatisticsLoaded");

        public static readonly EventId ValidationFailed = new(1900, "ValidationFailed");
        public static readonly EventId BusinessError = new(1999, "BusinessError");
    }
}
