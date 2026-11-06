-- ============================================================================
-- ĐỒ ÁN MÔN HỌC: CÔNG CỤ VÀ MÔI TRƯỜNG PHÁT TRIỂN PHẦN MỀM
-- GIẢNG VIÊN HƯỚNG DẪN: ThS. ĐẶNG THẾ HÂN
-- ĐỀ TÀI: HỆ THỐNG QUẢN LÝ THƯ VIỆN (LIBRARY MANAGEMENT SYSTEM)
-- FILE: LibraryDB.sql - TẠO CSDL, RÀNG BUỘC, DỮ LIỆU MẪU VÀ TRUY VẤN THỐNG KÊ
-- Chạy toàn bộ file này trong SSMS (F5). Có thể chạy lại nhiều lần.
-- ============================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibraryDB')
BEGIN
    CREATE DATABASE LibraryDB;
END
GO

USE LibraryDB;
GO

-- 2. XÓA BẢNG CŨ (theo thứ tự khóa ngoại)
IF OBJECT_ID(N'dbo.BorrowRecords', N'U') IS NOT NULL DROP TABLE dbo.BorrowRecords;
IF OBJECT_ID(N'dbo.Books', N'U')         IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID(N'dbo.Readers', N'U')       IS NOT NULL DROP TABLE dbo.Readers;
IF OBJECT_ID(N'dbo.Users', N'U')         IS NOT NULL DROP TABLE dbo.Users;
GO

-- 3. BẢNG USERS (TÀI KHOẢN ĐĂNG NHẬP) - mật khẩu lưu dạng băm SHA-256, KHÔNG lưu mật khẩu gốc
CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    Username     VARCHAR(50)   NOT NULL UNIQUE,
    PasswordHash CHAR(64)      NOT NULL,
    FullName     NVARCHAR(100) NOT NULL,
    Role         NVARCHAR(30)  NOT NULL DEFAULT N'Thủ thư',
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 4. BẢNG READERS (ĐỘC GIẢ)
CREATE TABLE Readers (
    ReaderId    INT IDENTITY(1001,1) PRIMARY KEY,
    ReaderCode  VARCHAR(20)   NOT NULL UNIQUE,
    FullName    NVARCHAR(100) NOT NULL,
    Class       VARCHAR(20)   NOT NULL,
    Email       VARCHAR(100)  NOT NULL UNIQUE,
    PhoneNumber VARCHAR(15)   NULL,
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

-- 5. BẢNG BOOKS (SÁCH)
CREATE TABLE Books (
    BookId           INT IDENTITY(5001,1) PRIMARY KEY,
    Title            NVARCHAR(200) NOT NULL,
    Author           NVARCHAR(100) NOT NULL,
    Category         NVARCHAR(50)  NOT NULL,
    PublishYear      INT           NOT NULL CHECK (PublishYear >= 1900 AND PublishYear <= 2100),
    TotalQuantity    INT           NOT NULL CHECK (TotalQuantity >= 0),
    BorrowedQuantity INT           NOT NULL DEFAULT 0 CHECK (BorrowedQuantity >= 0),
    CreatedAt        DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT CK_Book_Quantity CHECK (BorrowedQuantity <= TotalQuantity)
);
GO

-- 6. BẢNG BORROWRECORDS (PHIẾU MƯỢN)
--    Không dùng ON DELETE CASCADE: tầng BLL chặn xóa sách/độc giả còn phiếu đang mượn,
--    và CSDL cũng từ chối xóa nếu còn phiếu tham chiếu (bảo vệ lịch sử mượn trả).
CREATE TABLE BorrowRecords (
    RecordId   INT IDENTITY(1,1) PRIMARY KEY,
    ReaderId   INT           NOT NULL,
    BookId     INT           NOT NULL,
    BorrowDate DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    DueDate    DATE          NOT NULL,
    ReturnDate DATE          NULL,
    Status     NVARCHAR(30)  NOT NULL DEFAULT N'Đang mượn',   -- 'Đang mượn' | 'Đã trả'  (Quá hạn được tính khi truy vấn)
    Notes      NVARCHAR(255) NULL,
    CONSTRAINT FK_Borrow_Reader FOREIGN KEY (ReaderId) REFERENCES Readers(ReaderId),
    CONSTRAINT FK_Borrow_Book   FOREIGN KEY (BookId)   REFERENCES Books(BookId),
    CONSTRAINT CK_DueDate_BorrowDate CHECK (DueDate >= BorrowDate),
    CONSTRAINT CK_Borrow_Status CHECK (Status IN (N'Đang mượn', N'Đã trả'))
);
GO

CREATE INDEX IX_Borrow_Status ON BorrowRecords(Status);
CREATE INDEX IX_Books_Title   ON Books(Title);
GO

-- 7. DỮ LIỆU MẪU
-- Tài khoản: admin / admin123  và  thuthu / 123456
INSERT INTO Users (Username, PasswordHash, FullName, Role) VALUES
('admin',  '240BE518FABD2724DDB6F04EEB1DA5967448D7E831C08C8FA822809F74C720A9', N'Quản trị viên', N'Quản trị'),
('thuthu', '8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92', N'Nguyễn Thủ Thư', N'Thủ thư');
GO

INSERT INTO Readers (ReaderCode, FullName, Class, Email, PhoneNumber) VALUES
('DG001', N'Nguyễn Văn An',    'CNTT01', 'an@sv.edu.vn',   '0901234561'),
('DG002', N'Trần Thị Bình',    'CNTT01', 'binh@sv.edu.vn', '0901234562'),
('DG003', N'Lê Minh Châu',     'CNTT02', 'chau@sv.edu.vn', '0901234563'),
('DG004', N'Phạm Quốc Dũng',   'CNTT02', 'dung@sv.edu.vn', '0901234564'),
('DG005', N'Võ Thị Hà',        'CNTT03', 'ha@sv.edu.vn',   '0901234565'),
('DG006', N'Đặng Gia Huy',     'KTPM01', 'huy@sv.edu.vn',  '0901234566'),
('DG007', N'Hoàng Thảo My',    'KTPM01', 'my@sv.edu.vn',   '0901234567'),
('DG008', N'Bùi Đức Nam',      'HTTT01', 'nam@sv.edu.vn',  '0901234568'),
('DG009', N'Đỗ Phương Oanh',   'HTTT01', 'oanh@sv.edu.vn', '0901234569'),
('DG010', N'Nguyễn Tiến Phát', 'CNTT03', 'phat@sv.edu.vn', '0901234570');
GO

INSERT INTO Books (Title, Author, Category, PublishYear, TotalQuantity) VALUES
(N'Lập trình C# cơ bản',            N'Nguyễn Văn Hưng', N'CNTT',     2023, 10),  -- 5001
(N'Cơ sở dữ liệu SQL Server',       N'Trần Thị Mai',    N'CNTT',     2022, 8),   -- 5002
(N'Giải thuật và Cấu trúc dữ liệu', N'Lê Minh Tuấn',    N'CNTT',     2021, 7),   -- 5003
(N'Quản trị dự án phần mềm',        N'Phạm Quốc Dũng',  N'CNTT',     2020, 5),   -- 5004
(N'Kỹ năng mềm trong CNTT',         N'Vũ Thị Hà',       N'Kỹ năng',  2022, 12),  -- 5005
(N'Thiết kế giao diện WinForms',    N'Trịnh Xuân Bách', N'CNTT',     2024, 6),   -- 5006
(N'Kiểm thử phần mềm với MSTest',   N'Hoàng Anh Tuấn',  N'CNTT',     2023, 4),   -- 5007
(N'Nhập môn DevOps và Git',         N'Ngô Bảo Châu',    N'DevOps',   2022, 5),   -- 5008
(N'Lập trình mạng máy tính',        N'Đinh Trọng Đức',  N'Mạng',     2021, 9),   -- 5009
(N'Học máy và Ứng dụng AI',         N'Phan Thanh Thảo', N'AI',       2024, 15),  -- 5010
(N'Giao tiếp và thuyết trình',      N'Lê Thu Trang',    N'Kỹ năng',  2019, 6),   -- 5011
(N'Bảo mật hệ thống thông tin',     N'Trần Quang Khải', N'Mạng',     2023, 1);   -- 5012
GO

-- Phiếu mượn mẫu: ngày tính tương đối so với hôm nay để luôn có phiếu đang mượn / quá hạn / đã trả
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
INSERT INTO BorrowRecords (ReaderId, BookId, BorrowDate, DueDate, ReturnDate, Status, Notes) VALUES
(1001, 5001, DATEADD(DAY,-30,@Today), DATEADD(DAY,-16,@Today), DATEADD(DAY,-18,@Today), N'Đã trả',    N'Trả đúng hạn'),
(1001, 5003, DATEADD(DAY,-5, @Today), DATEADD(DAY, 9, @Today), NULL,                    N'Đang mượn', N'Làm tiểu luận'),
(1002, 5001, DATEADD(DAY,-20,@Today), DATEADD(DAY,-6, @Today), NULL,                    N'Đang mượn', N'Quá hạn - cần nhắc'),
(1003, 5002, DATEADD(DAY,-25,@Today), DATEADD(DAY,-11,@Today), DATEADD(DAY,-12,@Today), N'Đã trả',    NULL),
(1004, 5005, DATEADD(DAY,-3, @Today), DATEADD(DAY, 11,@Today), NULL,                    N'Đang mượn', NULL),
(1005, 5010, DATEADD(DAY,-2, @Today), DATEADD(DAY, 12,@Today), NULL,                    N'Đang mượn', NULL),
(1006, 5008, DATEADD(DAY,-18,@Today), DATEADD(DAY,-4, @Today), NULL,                    N'Đang mượn', N'Quá hạn'),
(1007, 5010, DATEADD(DAY,-10,@Today), DATEADD(DAY, 4, @Today), NULL,                    N'Đang mượn', NULL),
(1008, 5009, DATEADD(DAY,-40,@Today), DATEADD(DAY,-26,@Today), DATEADD(DAY,-27,@Today), N'Đã trả',    NULL),
(1009, 5012, DATEADD(DAY,-1, @Today), DATEADD(DAY, 13,@Today), NULL,                    N'Đang mượn', N'Bản duy nhất'),
(1010, 5006, DATEADD(DAY,-7, @Today), DATEADD(DAY, 7, @Today), NULL,                    N'Đang mượn', NULL),
(1003, 5005, DATEADD(DAY,-15,@Today), DATEADD(DAY,-1, @Today), DATEADD(DAY,-2, @Today), N'Đã trả',    NULL);
GO

-- Đồng bộ BorrowedQuantity = số phiếu đang mượn của từng sách (dữ liệu luôn nhất quán)
UPDATE b SET BorrowedQuantity = ISNULL(x.Cnt, 0)
FROM Books b
LEFT JOIN (SELECT BookId, COUNT(*) AS Cnt FROM BorrowRecords WHERE Status = N'Đang mượn' GROUP BY BookId) x
       ON x.BookId = b.BookId;
GO

-- 8. TRUY VẤN THỐNG KÊ MẪU (dùng cho chương 4 của báo cáo)
-- 8.1 Tổng quan: tổng đầu sách, tổng số cuốn, số cuốn còn có thể mượn, số phiếu đang mượn
SELECT
    (SELECT COUNT(*) FROM Books)                                         AS TongDauSach,
    (SELECT ISNULL(SUM(TotalQuantity),0) FROM Books)                     AS TongSoCuon,
    (SELECT ISNULL(SUM(TotalQuantity - BorrowedQuantity),0) FROM Books)  AS SoCuonConMuonDuoc,
    (SELECT COUNT(*) FROM BorrowRecords WHERE Status = N'Đang mượn')     AS SoPhieuDangMuon,
    (SELECT COUNT(*) FROM BorrowRecords
      WHERE Status = N'Đang mượn' AND DueDate < CAST(GETDATE() AS DATE)) AS SoPhieuQuaHan;

-- 8.2 Số đầu sách và số cuốn theo thể loại
SELECT Category, COUNT(*) AS SoDauSach, SUM(TotalQuantity) AS TongSoCuon
FROM Books GROUP BY Category ORDER BY TongSoCuon DESC;

-- 8.3 Top sách được mượn nhiều nhất
SELECT TOP 5 b.Title, COUNT(*) AS LuotMuon
FROM BorrowRecords br JOIN Books b ON b.BookId = br.BookId
GROUP BY b.Title ORDER BY LuotMuon DESC;
GO
