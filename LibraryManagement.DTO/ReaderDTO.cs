namespace LibraryManagement.DTO
{
    public class ReaderDTO
    {
        public int ReaderId { get; set; }
        public string ReaderCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"{ReaderCode} - {FullName} ({Class})";
        }
    }
}
