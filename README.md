# 📚 Smart Library – Hệ thống Quản lý Thư viện

Đồ án cuối kỳ **Công cụ và Môi trường Phát triển Phần mềm** – GVHD: ThS. Đặng Thế Hân

C# .NET 8 · Windows Forms · SQL Server · ADO.NET · Microsoft.Extensions.Logging · MSTest · Git/GitHub

## Chức năng
| Nhóm | Chi tiết |
|---|---|
| Đăng nhập | Mật khẩu băm SHA-256, khóa sau 5 lần sai |
| Quản lý sách | Hiển thị, thêm, sửa, xóa, tìm theo **tên / tác giả / thể loại**, lọc thể loại |
| Quản lý độc giả | Hiển thị, thêm, sửa, xóa, tìm; chặn trùng mã/email |
| Mượn – trả | Tạo phiếu, lưu hạn trả, ghi nhận ngày trả, **không cho mượn khi hết sách**, cảnh báo quá hạn, tối đa 5 cuốn/độc giả |
| Thống kê | Tổng đầu sách, số cuốn còn mượn được, số phiếu đang mượn/quá hạn, biểu đồ |

## Kiến trúc
```
LibraryManagement.UI     → WinForms: Forms/, Pages/, Controls/ (giao diện tự vẽ), Logging/
LibraryManagement.BLL    → Services + Validators (chỉ phụ thuộc interface DAL)
LibraryManagement.DAL    → ADO.NET: I*Repository + BookDAL/ReaderDAL/BorrowDAL/UserDAL
LibraryManagement.DTO    → BookDTO, ReaderDTO, BorrowRecordDTO, UserDTO, DashboardStatsDTO
LibraryManagement.Tests  → MSTest + Fake repositories (không cần SQL Server)
```

## Cách chạy
1. Mở **SSMS** → mở `LibraryDB.sql` → **Execute** (tạo CSDL + dữ liệu mẫu).
2. Nếu không dùng LocalDB, sửa chuỗi kết nối trong `LibraryManagement.UI/appsettings.json`
   (ví dụ `Server=.\\SQLEXPRESS;...`).
3. Mở `LibraryManagement.slnx` (hoặc `.sln`) → Startup Project: `LibraryManagement.UI` → **Rebuild Solution** → **F5**.
4. Đăng nhập: `admin / admin123` hoặc `thuthu / 123456`.

Muốn xem log trực tiếp trên Console: chọn profile **“LibraryManagement.UI (kèm cửa sổ Console log)”** cạnh nút Run (chạy với `--console`).

## Logging
- Provider: SimpleConsole (có Scope) + Debug (Output của VS) + **FileLoggerProvider tự viết**.
- File: `LibraryManagement.UI/bin/Debug/net8.0-windows/Logs/LogData_ddMMyyyy.log` (mỗi ngày một file, ghi nối tiếp).
- Định dạng: `[Timestamp] [LEVEL] [Category] [EventId n Name] Message | Scope: User=… Session=…` + Stack Trace nếu có Exception.
- Filter: UI ≥ Information · BLL ≥ Information · DAL ≥ Debug · Microsoft ≥ Warning.
- Quy ước Level: Debug (thời gian truy vấn, commit/rollback) · Information (đăng nhập, CRUD, mượn/trả thành công) · Warning (dữ liệu sai, đăng nhập sai, từ chối mượn) · Error (lỗi CSDL kèm Exception) · Critical (không kết nối được CSDL lúc khởi động).
- Không bao giờ ghi mật khẩu vào log (có Unit Test kiểm tra).

## Unit Test
- 106 Test Method (≈125 test case với DataRow) cho toàn bộ Validators và Services: Thêm/Sửa/Xóa/Tìm sách & độc giả, Mượn/Trả, Thống kê, Đăng nhập, có Boundary Value.
- Chạy: **Test → Run All Tests** trong Visual Studio hoặc `dotnet test`.

## Lỗi thường gặp
| Lỗi | Cách xử lý |
|---|---|
| “The debug executable … does not exist” | Build đang lỗi → mở **Error List** (Build Only) xem lỗi thật |
| Không kết nối được SQL Server | Chạy `LibraryDB.sql`; kiểm tra `appsettings.json`; với LocalDB: `sqllocaldb start MSSQLLocalDB` |
| File .exe bị khóa (MSB3027) | Tắt `LibraryManagement.UI.exe` trong Task Manager rồi build lại |

## Tài liệu
- Báo cáo: `BAO_CAO_CUOI_KY_QUAN_LY_THU_VIEN.md`
- Sơ đồ Use Case, ERD, kiến trúc, Sequence: `docs/diagrams/`
