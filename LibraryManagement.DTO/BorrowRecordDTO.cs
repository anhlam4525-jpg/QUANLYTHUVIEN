namespace LibraryManagement.DTO
{
    public class BorrowRecordDTO
    {
        public int RecordId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "Đang mượn";
        public string? Notes { get; set; }
    }

    public class DashboardStatsDTO
    {
        public int TotalBookTitles { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopies { get; set; }
        public int TotalReaders { get; set; }
    }
}
