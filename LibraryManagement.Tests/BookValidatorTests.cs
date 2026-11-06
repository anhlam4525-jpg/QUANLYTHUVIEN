using LibraryManagement.BLL.Validators;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử quy tắc dữ liệu Sách – có Boundary Value cho Năm xuất bản và Số lượng.</summary>
    [TestClass]
    public class BookValidatorTests
    {
        private BookValidator _validator = null!;

        [TestInitialize]
        public void Setup() => _validator = new BookValidator();

        // ----- Boundary: Năm xuất bản 1900 → năm hiện tại -----
        [TestMethod]
        public void IsValidPublishYear_1899_BelowLowerBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidPublishYear(1899));

        [TestMethod]
        public void IsValidPublishYear_1900_AtLowerBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidPublishYear(1900));

        [TestMethod]
        public void IsValidPublishYear_1901_AboveLowerBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidPublishYear(1901));

        [TestMethod]
        public void IsValidPublishYear_CurrentYear_AtUpperBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidPublishYear(DateTime.Now.Year));

        [TestMethod]
        public void IsValidPublishYear_NextYear_AboveUpperBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidPublishYear(DateTime.Now.Year + 1));

        // ----- Boundary: Số lượng 0 → 10000 -----
        [TestMethod]
        public void IsValidQuantity_MinusOne_BelowBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidQuantity(-1));

        [TestMethod]
        public void IsValidQuantity_Zero_AtBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidQuantity(0));

        [TestMethod]
        public void IsValidQuantity_Max_AtUpperBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidQuantity(BookValidator.MaxQuantity));

        [TestMethod]
        public void IsValidQuantity_MaxPlusOne_AboveUpperBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidQuantity(BookValidator.MaxQuantity + 1));

        [TestMethod]
        public void IsValidQuantityForUpdate_BelowBorrowed_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidQuantityForUpdate(newTotalQuantity: 2, borrowedQuantity: 3));

        [TestMethod]
        public void IsValidQuantityForUpdate_EqualBorrowed_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidQuantityForUpdate(newTotalQuantity: 3, borrowedQuantity: 3));

        // ----- Tên sách / Tác giả / Thể loại -----
        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("A")]
        public void IsValidTitle_EmptyOrTooShort_ReturnsFalse(string title)
            => Assert.IsFalse(_validator.IsValidTitle(title));

        [TestMethod]
        public void IsValidTitle_Null_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidTitle(null));

        [TestMethod]
        public void IsValidTitle_ValidName_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidTitle("Lập trình C# cơ bản"));

        [TestMethod]
        public void IsValidTitle_201Characters_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidTitle(new string('a', 201)));

        [TestMethod]
        public void IsValidAuthor_Empty_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidAuthor(" "));

        [TestMethod]
        public void IsValidCategory_AllKeyword_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidCategory("Tất cả"));

        [TestMethod]
        public void IsValidCategory_Normal_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidCategory("CNTT"));

        // ----- Điều kiện cho mượn -----
        [TestMethod]
        public void CanBorrow_AvailableGreaterThanZero_ReturnsTrue()
            => Assert.IsTrue(_validator.CanBorrow(10, 8));

        [TestMethod]
        public void CanBorrow_OutOfStock_ReturnsFalse()
            => Assert.IsFalse(_validator.CanBorrow(5, 5));
    }
}
