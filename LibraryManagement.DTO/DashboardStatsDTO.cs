namespace LibraryManagement.DTO
{
    public class DashboardStatsDTO
    {
        public int TotalBookTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopies { get; set; }
        public int TotalReaders { get; set; }
        public int ActiveTickets { get; set; }
        public int OverdueTickets { get; set; }
        public int ReturnedTickets { get; set; }
    }

    public class CategoryStatDTO
    {
        public string Category { get; set; } = string.Empty;
        public int TitleCount { get; set; }
        public int TotalCopies { get; set; }
    }

    public class TopBookDTO
    {
        public string Title { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }
}
