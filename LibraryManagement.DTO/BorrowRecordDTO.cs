namespace LibraryManagement.DTO
{
    public class BorrowRecordDTO
    {
        public const string StatusBorrowing = "Đang mượn";
        public const string StatusReturned = "Đã trả";
        public const string StatusOverdue = "Quá hạn";

        public int RecordId { get; set; }
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = StatusBorrowing;
        public string? Notes { get; set; }

        /// <summary>Phiếu chưa trả và đã qua hạn trả.</summary>
        public bool IsOverdue => ReturnDate == null && Status == StatusBorrowing && DueDate.Date < DateTime.Today;

        /// <summary>Trạng thái hiển thị: Đang mượn / Quá hạn / Đã trả.</summary>
        public string DisplayStatus => IsOverdue ? StatusOverdue : Status;

        /// <summary>Số ngày quá hạn (0 nếu chưa quá hạn).</summary>
        public int OverdueDays => IsOverdue ? (DateTime.Today - DueDate.Date).Days : 0;
    }
}
