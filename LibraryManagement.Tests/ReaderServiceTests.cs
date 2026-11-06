using LibraryManagement.BLL.Services;
using LibraryManagement.DTO;
using LibraryManagement.Tests.Fakes;

namespace LibraryManagement.Tests
{
    /// <summary>Kiểm thử chức năng Quản lý độc giả: Thêm / Sửa / Xóa / Tìm kiếm.</summary>
    [TestClass]
    public class ReaderServiceTests
    {
        private FakeReaderRepository _repo = null!;
        private ReaderService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repo = new FakeReaderRepository();
            _service = new ReaderService(_repo, new ListLogger());
        }

        private static ReaderDTO NewReader(string code = "DG011", string email = "moi@sv.edu.vn") => new()
        {
            ReaderCode = code,
            FullName = "Nguyễn Văn Mới",
            Class = "CNTT01",
            Email = email,
            PhoneNumber = "0901234567"
        };

        [TestMethod]
        public void AddReader_ValidData_ReturnsSuccess()
        {
            var result = _service.AddReader(NewReader());
            Assert.IsTrue(result.Success);
            Assert.HasCount(1, _repo.Readers);
        }

        [TestMethod]
        public void AddReader_DuplicateCode_ReturnsFail()
        {
            _repo.Seed("DG011", "Người cũ", "cu@sv.edu.vn");
            var result = _service.AddReader(NewReader("dg011"));
            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, _repo.InsertCalls);
        }

        [TestMethod]
        public void AddReader_DuplicateEmail_ReturnsFail()
        {
            _repo.Seed("DG001", "Người cũ", "moi@sv.edu.vn");
            Assert.IsFalse(_service.AddReader(NewReader()).Success);
        }

        [TestMethod]
        public void AddReader_InvalidEmail_ReturnsFail()
            => Assert.IsFalse(_service.AddReader(NewReader(email: "khong-hop-le")).Success);

        [TestMethod]
        public void AddReader_InvalidCode_ReturnsFail()
            => Assert.IsFalse(_service.AddReader(NewReader(code: "D")).Success);

        [TestMethod]
        public void AddReader_InvalidPhone_ReturnsFail()
        {
            var r = NewReader();
            r.PhoneNumber = "123";
            Assert.IsFalse(_service.AddReader(r).Success);
        }

        [TestMethod]
        public void UpdateReader_ValidData_Updates()
        {
            var existing = _repo.Seed("DG001", "Tên cũ", "a@sv.edu.vn");
            var r = NewReader("DG001", "a@sv.edu.vn");
            r.ReaderId = existing.ReaderId;
            r.FullName = "Tên mới";

            Assert.IsTrue(_service.UpdateReader(r).Success);
            Assert.AreEqual("Tên mới", _repo.GetById(existing.ReaderId)!.FullName);
        }

        [TestMethod]
        public void UpdateReader_EmailOfAnotherReader_ReturnsFail()
        {
            var a = _repo.Seed("DG001", "A", "a@sv.edu.vn");
            _repo.Seed("DG002", "B", "b@sv.edu.vn");
            var r = NewReader("DG001", "b@sv.edu.vn");
            r.ReaderId = a.ReaderId;

            Assert.IsFalse(_service.UpdateReader(r).Success);
        }

        [TestMethod]
        public void DeleteReader_NoActiveBorrows_Removes()
        {
            var r = _repo.Seed("DG001", "A", "a@sv.edu.vn");
            Assert.IsTrue(_service.DeleteReader(r.ReaderId).Success);
            Assert.IsEmpty(_repo.Readers);
        }

        [TestMethod]
        public void DeleteReader_HasActiveBorrows_ReturnsFail()
        {
            var r = _repo.Seed("DG001", "A", "a@sv.edu.vn");
            _repo.ActiveBorrowCounter = _ => 2;
            Assert.IsFalse(_service.DeleteReader(r.ReaderId).Success);
            Assert.HasCount(1, _repo.Readers);
        }

        [TestMethod]
        public void SearchReaders_ByName_ReturnsMatching()
        {
            _repo.Seed("DG001", "Nguyễn Văn An", "an@sv.edu.vn");
            _repo.Seed("DG002", "Trần Thị Bình", "binh@sv.edu.vn");
            var result = _service.SearchReaders("Bình");
            Assert.HasCount(1, result);
            Assert.AreEqual("DG002", result[0].ReaderCode);
        }

        [TestMethod]
        public void SearchReaders_EmptyKeyword_ReturnsAll()
        {
            _repo.Seed("DG001", "A", "a@sv.edu.vn");
            _repo.Seed("DG002", "B", "b@sv.edu.vn");
            Assert.HasCount(2, _service.SearchReaders(null));
        }
    }
}
