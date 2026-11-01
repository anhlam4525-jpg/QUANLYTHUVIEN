-- ============================================================================
-- ĐỒ ÁN MÔN HỌC: CÔNG CỤ VÀ MÔI TRƯỜNG PHÁT TRIỂN PHẦN MỀM
-- GIẢNG VIÊN HƯỚNG DẪN: ThS. ĐẶNG THẾ HÂN
-- ĐỀ TÀI: HỆ THỐNG QUẢN LÝ THƯ VIỆN (LIBRARY MANAGEMENT SYSTEM)
-- FILE: LibraryDB.sql - TẬP LỆNH TẠO CSDL, RÀNG BUỘC VÀ DỮ LIỆU MẪU
-- ============================================================================

-- 1. TẠO CƠ SỞ DỮ LIỆU
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LibraryDB')
BEGIN
    CREATE DATABASE LibraryDB;
    PRINT N'-> Đã tạo thành công cơ sở dữ liệu LibraryDB.';
END
GO

USE LibraryDB;
GO

-- 2. DỌN DẸP BẢNG CŨ (NẾU ĐÃ TỒN TẠI)
IF OBJECT_ID(N'dbo.BorrowRecords', N'U') IS NOT NULL DROP TABLE dbo.BorrowRecords;
IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID(N'dbo.Readers', N'U') IS NOT NULL DROP TABLE dbo.Readers;
GO

-- 3. TẠO BẢNG READERS (ĐỘC GIẢ)
CREATE TABLE Readers (
    ReaderId INT PRIMARY KEY IDENTITY(1001, 1),
    ReaderCode VARCHAR(20) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Class VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PhoneNumber VARCHAR(15) NULL,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- 4. TẠO BẢNG BOOKS (SÁCH)
CREATE TABLE Books (
    BookId INT PRIMARY KEY IDENTITY(5001, 1),
    Title NVARCHAR(200) NOT NULL,
    Author NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    PublishYear INT NOT NULL CHECK (PublishYear >= 1900 AND PublishYear <= 2030),
    TotalQuantity INT NOT NULL CHECK (TotalQuantity >= 0),
    BorrowedQuantity INT NOT NULL DEFAULT 0 CHECK (BorrowedQuantity >= 0),
    CreatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT CK_Book_Quantity CHECK (BorrowedQuantity <= TotalQuantity)
);
GO

-- 5. TẠO BẢNG BORROWRECORDS (PHIẾU MƯỢN TRẢ SÁCH)
CREATE TABLE BorrowRecords (
    RecordId INT PRIMARY KEY IDENTITY(1, 1),
    ReaderId INT NOT NULL,
    BookId INT NOT NULL,
    BorrowDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    DueDate DATE NOT NULL,
    ReturnDate DATE NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT N'Đang mượn', -- 'Đang mượn', 'Đã trả', 'Quá hạn'
    Notes NVARCHAR(255) NULL,
    CONSTRAINT FK_Borrow_Reader FOREIGN KEY (ReaderId) REFERENCES Readers(ReaderId) ON DELETE CASCADE,
    CONSTRAINT FK_Borrow_Book FOREIGN KEY (BookId) REFERENCES Books(BookId) ON DELETE CASCADE,
    CONSTRAINT CK_DueDate_BorrowDate CHECK (DueDate >= BorrowDate)
);
GO

-- 6. THÊM DỮ LIỆU MẪU (SEED DATA)
-- Thêm độc giả mẫu
INSERT INTO Readers (ReaderCode, FullName, Class, Email, PhoneNumber) VALUES
('DG001', N'Nguyễn Văn An', 'CNTT01', 'an@sv.edu.vn', '0901234561'),
('DG002', N'Trần Thị Bình', 'CNTT01', 'binh@sv.edu.vn', '0901234562'),
('DG003', N'Lê Minh Châu', 'CNTT02', 'chau@sv.edu.vn', '0901234563'),
('DG004', N'Phạm Quốc Dũng', 'CNTT02', 'dung@sv.edu.vn', '0901234564'),
('DG005', N'Võ Thị Hà', 'CNTT03', 'ha@sv.edu.vn', '0901234565'),
('DG006', N'Đặng Gia Huy', 'KTPM01', 'huy@sv.edu.vn', '0901234566'),
('DG007', N'Hoàng Thảo My', 'KTPM01', 'my@sv.edu.vn', '0901234567'),
('DG008', N'Bùi Đức Nam', 'HTTT01', 'nam@sv.edu.vn', '0901234568'),
('DG009', N'Đỗ Phương Oanh', 'HTTT01', 'oanh@sv.edu.vn', '0901234569'),
('DG010', N'Nguyễn Tiến Phát', 'CNTT03', 'phat@sv.edu.vn', '0901234570');
GO

-- Thêm sách mẫu (theo đúng hình mẫu đề bài buổi 5)
INSERT INTO Books (Title, Author, Category, PublishYear, TotalQuantity, BorrowedQuantity) VALUES
(N'Lập trình C# cơ bản', N'Nguyễn Văn Hưng', N'CNTT', 2023, 10, 2),
(N'Cơ sở dữ liệu SQL Server', N'Trần Thị Mai', N'CNTT', 2022, 8, 2),
(N'Giải thuật và Cấu trúc dữ liệu', N'Lê Minh Tuấn', N'CNTT', 2021, 7, 1),
(N'Quản trị dự án phần mềm', N'Phạm Quốc Dũng', N'CNTT', 2020, 5, 0),
(N'Kỹ năng mềm trong CNTT', N'Vũ Thị Hà', N'Kỹ năng', 2022, 12, 4),
(N'Thiết kế giao diện WinForms', N'Trịnh Xuân Bách', N'CNTT', 2024, 6, 1),
(N'Kiểm thử phần mềm với MSTest', N'Hoàng Anh Tuấn', N'CNTT', 2023, 4, 0),
(N'Nhập môn DevOps và Git', N'Ngô Bảo Châu', N'DevOps', 2022, 5, 3),
(N'Lập trình mạng máy tính', N'Đinh Trọng Đức', N'Mạng', 2021, 9, 2),
(N'Học máy và Ứng dụng AI', N'Phan Thanh Thảo', N'AI', 2024, 15, 5);
GO

-- Thêm giao dịch mượn mẫu
INSERT INTO BorrowRecords (ReaderId, BookId, BorrowDate, DueDate, ReturnDate, Status, Notes) VALUES
(1001, 5001, '2026-10-10', '2026-10-24', '2026-10-20', N'Đã trả', N'Trả đúng hạn, sách nguyên vẹn'),
(1001, 5003, '2026-10-25', '2026-11-08', NULL, N'Đang mượn', N'Mượn phục vụ làm tiểu luận'),
(1002, 5001, '2026-10-15', '2026-10-29', NULL, N'Đang mượn', N'Gia hạn thêm'),
(1003, 5002, '2026-10-01', '2026-10-15', '2026-10-14', N'Đã trả', N'Đã trả'),
(1004, 5005, '2026-10-18', '2026-11-01', NULL, N'Đang mượn', N'Đang mượn');
GO

-- 7. CÁC TRUY VẤN THỐNG KÊ MẪU DÀNH CHO THỐNG KÊ HỆ THỐNG
-- Thống kê tổng quan:
SELECT 
    COUNT(*) AS TotalBookTitles,
    SUM(TotalQuantity) AS TotalCopies,
    SUM(TotalQuantity - BorrowedQuantity) AS AvailableCopies,
    SUM(BorrowedQuantity) AS TotalBorrowingCopies
FROM Books;

-- Thống kê số lượng sách theo từng thể loại:
SELECT Category, COUNT(*) AS NumberOfTitles, SUM(TotalQuantity) AS TotalInStock
FROM Books
GROUP BY Category
ORDER BY TotalInStock DESC;
GO
