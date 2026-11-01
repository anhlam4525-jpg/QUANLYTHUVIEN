using LibraryManagement.BLL.Validators;

namespace LibraryManagement.Tests
{
    [TestClass]
    public class BookValidatorTests
    {
        private BookValidator _validator = null!;

        [TestInitialize]
        public void Setup()
        {
            _validator = new BookValidator();
        }

        #region Kiểm thử giá trị biên (Boundary Value Analysis) cho Năm xuất bản (PublishYear: 1900 -> Năm hiện tại)
        [TestMethod]
        public void IsValidPublishYear_YearIs1899_BelowBoundary_ReturnsFalse()
        {
            // Ngay dưới biên dưới (1900 - 1 = 1899)
            bool result = _validator.IsValidPublishYear(1899);
            Assert.IsFalse(result, "Năm 1899 nhỏ hơn mốc 1900 nên phải không hợp lệ.");
        }

        [TestMethod]
        public void IsValidPublishYear_YearIs1900_AtLowerBoundary_ReturnsTrue()
        {
            // Ngay tại biên dưới (1900)
            bool result = _validator.IsValidPublishYear(1900);
            Assert.IsTrue(result, "Năm 1900 là mốc biên dưới hợp lệ.");
        }

        [TestMethod]
        public void IsValidPublishYear_CurrentYear_AtUpperBoundary_ReturnsTrue()
        {
            // Ngay tại biên trên (Năm hiện tại)
            int currentYear = DateTime.Now.Year;
            bool result = _validator.IsValidPublishYear(currentYear);
            Assert.IsTrue(result, "Năm xuất bản bằng năm hiện tại phải hợp lệ.");
        }

        [TestMethod]
        public void IsValidPublishYear_FutureYear_AboveUpperBoundary_ReturnsFalse()
        {
            // Vượt quá biên trên (Năm hiện tại + 1)
            int futureYear = DateTime.Now.Year + 1;
            bool result = _validator.IsValidPublishYear(futureYear);
            Assert.IsFalse(result, "Năm xuất bản trong tương lai không được phép.");
        }

        [TestMethod]
        public void IsValidPublishYear_ValidMidRangeYear_ReturnsTrue()
        {
            // Giá trị thông thường trong khoảng hợp lệ
            bool result = _validator.IsValidPublishYear(2023);
            Assert.IsTrue(result);
        }
        #endregion

        #region Kiểm thử giá trị biên cho Số lượng sách (TotalQuantity >= 0)
        [TestMethod]
        public void IsValidQuantity_ZeroQuantity_AtBoundary_ReturnsTrue()
        {
            // Ngay tại biên 0
            bool result = _validator.IsValidQuantity(0);
            Assert.IsTrue(result, "Số lượng bằng 0 hợp lệ cho sách mới nhập chưa về hàng.");
        }

        [TestMethod]
        public void IsValidQuantity_NegativeOne_BelowBoundary_ReturnsFalse()
        {
            // Dưới biên (-1)
            bool result = _validator.IsValidQuantity(-1);
            Assert.IsFalse(result, "Số lượng sách không được âm.");
        }

        [TestMethod]
        public void IsValidQuantity_PositiveValue_ReturnsTrue()
        {
            bool result = _validator.IsValidQuantity(50);
            Assert.IsTrue(result);
        }
        #endregion

        #region Kiểm thử quy tắc Tên sách
        [TestMethod]
        public void IsValidTitle_EmptyString_ReturnsFalse()
        {
            bool result = _validator.IsValidTitle("");
            Assert.IsFalse(result, "Tên sách rỗng phải không hợp lệ.");
        }

        [TestMethod]
        public void IsValidTitle_WhitespaceOnly_ReturnsFalse()
        {
            bool result = _validator.IsValidTitle("     ");
            Assert.IsFalse(result, "Tên sách chỉ gồm khoảng trắng phải không hợp lệ.");
        }

        [TestMethod]
        public void IsValidTitle_ValidName_ReturnsTrue()
        {
            bool result = _validator.IsValidTitle("Lập trình C# cơ bản");
            Assert.IsTrue(result);
        }
        #endregion

        #region Kiểm thử điều kiện Mượn sách (CanBorrow)
        [TestMethod]
        public void CanBorrow_AvailableGreaterThanZero_ReturnsTrue()
        {
            // Tổng 10 cuốn, đã mượn 8 cuốn => còn 2 cuốn
            bool canBorrow = _validator.CanBorrow(10, 8);
            Assert.IsTrue(canBorrow, "Sách còn trong kho phải cho phép mượn.");
        }

        [TestMethod]
        public void CanBorrow_AvailableIsZero_OutOfStock_ReturnsFalse()
        {
            // Tổng 5 cuốn, đã mượn 5 cuốn => còn 0 cuốn
            bool canBorrow = _validator.CanBorrow(5, 5);
            Assert.IsFalse(canBorrow, "Sách đã hết bản cuốn trong kho không được cho mượn.");
        }
        #endregion
    }
}
