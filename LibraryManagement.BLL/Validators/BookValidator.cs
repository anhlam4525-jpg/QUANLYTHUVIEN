namespace LibraryManagement.BLL.Validators
{
    public class BookValidator
    {
        public bool IsValidTitle(string? title)
        {
            return !string.IsNullOrWhiteSpace(title) && title.Trim().Length >= 2 && title.Trim().Length <= 200;
        }

        public bool IsValidAuthor(string? author)
        {
            return !string.IsNullOrWhiteSpace(author) && author.Trim().Length >= 2 && author.Trim().Length <= 100;
        }

        public bool IsValidPublishYear(int year)
        {
            int currentYear = DateTime.Now.Year;
            return year >= 1900 && year <= currentYear;
        }

        public bool IsValidQuantity(int quantity)
        {
            return quantity >= 0 && quantity <= 10000;
        }

        public bool CanBorrow(int totalQuantity, int borrowedQuantity)
        {
            return (totalQuantity - borrowedQuantity) > 0;
        }
    }
}
