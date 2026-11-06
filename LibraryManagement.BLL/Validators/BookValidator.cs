namespace LibraryManagement.BLL.Validators
{
    public class BookValidator
    {
        public const int MinPublishYear = 1900;
        public const int MaxQuantity = 10000;

        public bool IsValidTitle(string? title)
        {
            return !string.IsNullOrWhiteSpace(title) && title.Trim().Length >= 2 && title.Trim().Length <= 200;
        }

        public bool IsValidAuthor(string? author)
        {
            return !string.IsNullOrWhiteSpace(author) && author.Trim().Length >= 2 && author.Trim().Length <= 100;
        }

        public bool IsValidCategory(string? category)
        {
            return !string.IsNullOrWhiteSpace(category) && category.Trim().Length <= 50
                   && !string.Equals(category.Trim(), "Tất cả", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsValidPublishYear(int year)
        {
            return year >= MinPublishYear && year <= DateTime.Now.Year;
        }

        public bool IsValidQuantity(int quantity)
        {
            return quantity >= 0 && quantity <= MaxQuantity;
        }

        /// <summary>Khi sửa sách, tổng số lượng không được nhỏ hơn số cuốn đang cho mượn.</summary>
        public bool IsValidQuantityForUpdate(int newTotalQuantity, int borrowedQuantity)
        {
            return IsValidQuantity(newTotalQuantity) && newTotalQuantity >= borrowedQuantity;
        }

        public bool CanBorrow(int totalQuantity, int borrowedQuantity)
        {
            return (totalQuantity - borrowedQuantity) > 0;
        }
    }
}
