namespace LibraryManagement.DTO
{
    public class BookDTO
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int PublishYear { get; set; }
        public int TotalQuantity { get; set; }
        public int BorrowedQuantity { get; set; }
        public int AvailableQuantity => Math.Max(0, TotalQuantity - BorrowedQuantity);
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"{BookId} - {Title} (Còn {AvailableQuantity}/{TotalQuantity})";
        }
    }
}
