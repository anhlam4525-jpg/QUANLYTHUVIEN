using LibraryManagement.BLL.Validators;

namespace LibraryManagement.Tests
{
    [TestClass]
    public class ReaderValidatorTests
    {
        private ReaderValidator _validator = null!;

        [TestInitialize]
        public void Setup()
        {
            _validator = new ReaderValidator();
        }

        [TestMethod]
        public void IsValidReaderCode_ValidCode_ReturnsTrue()
        {
            Assert.IsTrue(_validator.IsValidReaderCode("DG001"));
            Assert.IsTrue(_validator.IsValidReaderCode("SV22123456"));
        }

        [TestMethod]
        public void IsValidReaderCode_TooShortOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(_validator.IsValidReaderCode(""));
            Assert.IsFalse(_validator.IsValidReaderCode("D"));
        }

        [TestMethod]
        public void IsValidEmail_StandardEmail_ReturnsTrue()
        {
            Assert.IsTrue(_validator.IsValidEmail("student@sv.edu.vn"));
            Assert.IsTrue(_validator.IsValidEmail("nguyen.an@gmail.com"));
        }

        [TestMethod]
        public void IsValidEmail_InvalidFormat_ReturnsFalse()
        {
            Assert.IsFalse(_validator.IsValidEmail("invalid-email"));
            Assert.IsFalse(_validator.IsValidEmail("student@.com"));
            Assert.IsFalse(_validator.IsValidEmail("@domain.com"));
        }

        [TestMethod]
        public void IsValidFullName_EmptyName_ReturnsFalse()
        {
            Assert.IsFalse(_validator.IsValidFullName(""));
            Assert.IsFalse(_validator.IsValidFullName("   "));
        }

        [TestMethod]
        public void IsValidFullName_ValidName_ReturnsTrue()
        {
            Assert.IsTrue(_validator.IsValidFullName("Nguyễn Văn An"));
        }
    }
}
