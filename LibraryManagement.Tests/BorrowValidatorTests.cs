using LibraryManagement.BLL.Validators;

namespace LibraryManagement.Tests
{
    /// <summary>Boundary Value cho thời hạn mượn (0 → 60 ngày) và hạn mức 5 cuốn/độc giả.</summary>
    [TestClass]
    public class BorrowValidatorTests
    {
        private readonly BorrowValidator _validator = new();
        private static readonly DateTime Today = new(2026, 11, 2);

        [TestMethod]
        public void IsValidDueDate_DueBeforeBorrow_BelowBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidDueDate(Today, Today.AddDays(-1)));

        [TestMethod]
        public void IsValidDueDate_SameDay_AtLowerBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidDueDate(Today, Today));

        [TestMethod]
        public void IsValidDueDate_60Days_AtUpperBoundary_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidDueDate(Today, Today.AddDays(BorrowValidator.MaxLoanDays)));

        [TestMethod]
        public void IsValidDueDate_61Days_AboveUpperBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidDueDate(Today, Today.AddDays(BorrowValidator.MaxLoanDays + 1)));

        [TestMethod]
        public void IsWithinBorrowLimit_4Active_ReturnsTrue()
            => Assert.IsTrue(_validator.IsWithinBorrowLimit(BorrowValidator.MaxActiveBorrowsPerReader - 1));

        [TestMethod]
        public void IsWithinBorrowLimit_5Active_AtLimit_ReturnsFalse()
            => Assert.IsFalse(_validator.IsWithinBorrowLimit(BorrowValidator.MaxActiveBorrowsPerReader));

        [TestMethod]
        public void CalculateOverdueDays_ReturnedEarly_ReturnsZero()
            => Assert.AreEqual(0, _validator.CalculateOverdueDays(Today, Today.AddDays(-3)));

        [TestMethod]
        public void CalculateOverdueDays_Returned5DaysLate_Returns5()
            => Assert.AreEqual(5, _validator.CalculateOverdueDays(Today, Today.AddDays(5)));
    }
}
