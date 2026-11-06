namespace LibraryManagement.BLL.Validators
{
    public class BorrowValidator
    {
        /// <summary>Số ngày mượn mặc định.</summary>
        public const int DefaultLoanDays = 14;
        /// <summary>Thời hạn mượn tối đa (ngày).</summary>
        public const int MaxLoanDays = 60;
        /// <summary>Số phiếu đang mượn tối đa của một độc giả.</summary>
        public const int MaxActiveBorrowsPerReader = 5;

        /// <summary>Hạn trả phải từ ngày mượn trở đi và không quá <see cref="MaxLoanDays"/> ngày.</summary>
        public bool IsValidDueDate(DateTime borrowDate, DateTime dueDate)
        {
            int days = (dueDate.Date - borrowDate.Date).Days;
            return days >= 0 && days <= MaxLoanDays;
        }

        public bool IsWithinBorrowLimit(int activeBorrowCount)
        {
            return activeBorrowCount < MaxActiveBorrowsPerReader;
        }

        public int CalculateOverdueDays(DateTime dueDate, DateTime returnDate)
        {
            return Math.Max(0, (returnDate.Date - dueDate.Date).Days);
        }
    }
}
