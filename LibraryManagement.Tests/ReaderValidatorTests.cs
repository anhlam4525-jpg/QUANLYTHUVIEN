using LibraryManagement.BLL.Validators;

namespace LibraryManagement.Tests
{
    [TestClass]
    public class ReaderValidatorTests
    {
        private ReaderValidator _validator = null!;

        [TestInitialize]
        public void Setup() => _validator = new ReaderValidator();

        [TestMethod]
        [DataRow("DG001")]
        [DataRow("SV22123456")]
        [DataRow("ABC")]
        public void IsValidReaderCode_ValidCode_ReturnsTrue(string code)
            => Assert.IsTrue(_validator.IsValidReaderCode(code));

        [TestMethod]
        [DataRow("")]
        [DataRow("D")]
        [DataRow("DG")]
        [DataRow("DG 001")]
        [DataRow("DG-001")]
        public void IsValidReaderCode_InvalidCode_ReturnsFalse(string code)
            => Assert.IsFalse(_validator.IsValidReaderCode(code));

        [TestMethod]
        public void IsValidReaderCode_21Characters_AboveBoundary_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidReaderCode(new string('A', 21)));

        [TestMethod]
        [DataRow("student@sv.edu.vn")]
        [DataRow("nguyen.an@gmail.com")]
        public void IsValidEmail_StandardEmail_ReturnsTrue(string email)
            => Assert.IsTrue(_validator.IsValidEmail(email));

        [TestMethod]
        [DataRow("invalid-email")]
        [DataRow("student@.com")]
        [DataRow("@domain.com")]
        [DataRow("an@sv")]
        [DataRow("")]
        public void IsValidEmail_InvalidFormat_ReturnsFalse(string email)
            => Assert.IsFalse(_validator.IsValidEmail(email));

        [TestMethod]
        public void IsValidFullName_Empty_ReturnsFalse()
        {
            Assert.IsFalse(_validator.IsValidFullName(""));
            Assert.IsFalse(_validator.IsValidFullName("   "));
        }

        [TestMethod]
        public void IsValidFullName_ValidName_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidFullName("Nguyễn Văn An"));

        [TestMethod]
        public void IsValidPhoneNumber_Empty_IsOptional_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidPhoneNumber(null));

        [TestMethod]
        [DataRow("0901234567")]
        [DataRow("09012345678")]
        public void IsValidPhoneNumber_10Or11Digits_ReturnsTrue(string phone)
            => Assert.IsTrue(_validator.IsValidPhoneNumber(phone));

        [TestMethod]
        [DataRow("090123456")]      // 9 số – dưới biên
        [DataRow("090123456789")]   // 12 số – trên biên
        [DataRow("1901234567")]     // không bắt đầu bằng 0
        [DataRow("09012abc67")]
        public void IsValidPhoneNumber_Invalid_ReturnsFalse(string phone)
            => Assert.IsFalse(_validator.IsValidPhoneNumber(phone));

        [TestMethod]
        public void IsValidClass_Valid_ReturnsTrue()
            => Assert.IsTrue(_validator.IsValidClass("CNTT01"));

        [TestMethod]
        public void IsValidClass_Empty_ReturnsFalse()
            => Assert.IsFalse(_validator.IsValidClass(" "));
    }
}
