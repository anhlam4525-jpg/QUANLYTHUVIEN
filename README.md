# HỆ THỐNG QUẢN LÝ THƯ VIỆN (LIBRARY MANAGEMENT SYSTEM)
**Đồ án kết thúc học phần môn: Công cụ và Môi trường Phát triển Phần mềm**  
**Giảng viên hướng dẫn:** ThS. Đặng Thế Hân  

---

## 1. Giới thiệu tổng quan
Ứng dụng **Quản Lý Thư Viện** được xây dựng trên nền tảng **C# Windows Forms (.NET 8)**, áp dụng kiến trúc đa tầng (N-Tier Architecture), kết nối hệ quản trị cơ sở dữ liệu **Microsoft SQL Server**, tích hợp hệ thống ghi nhật ký chuẩn **Microsoft.Extensions.Logging** (ghi log theo ngày ra file text) và bộ kiểm thử tự động **MSTest**.

## 2. Kiến trúc giải pháp (Solution Architecture)
Solution bao gồm 5 dự án thành phần:
- `LibraryManagement.UI`: Tầng giao diện người dùng Windows Forms (.NET 8).
- `LibraryManagement.BLL`: Tầng xử lý nghiệp vụ, kiểm tra tính hợp lệ (Validation) và quy tắc mượn/trả sách.
- `LibraryManagement.DAL`: Tầng truy xuất dữ liệu an toàn với `Microsoft.Data.SqlClient`, sử dụng Parameterized Queries và Transaction.
- `LibraryManagement.DTO`: Tầng chứa các lớp mô hình dữ liệu (Data Transfer Objects).
- `LibraryManagement.Tests`: Tầng kiểm thử đơn vị với MSTest kiểm thử các trường hợp dữ liệu biên (Boundary Value Testing).

## 3. Hướng dẫn thiết lập môi trường & Chạy ứng dụng

### 3.1. Yêu cầu môi trường
- .NET 8 SDK (phiên bản 8.0.xxx)
- Visual Studio 2022 (với Workload *.NET Desktop Development*)
- Microsoft SQL Server & SQL Server Management Studio (SSMS)

### 3.2. Cài đặt Cơ sở dữ liệu
1. Mở SSMS và kết nối đến SQL Server instance của bạn (`.` hoặc `localhost`).
2. Mở file script `LibraryDB.sql` (được trích dẫn chi tiết trong Báo cáo cuối kỳ).
3. Thực thi toàn bộ script để tạo Database `LibraryDB` và các bảng `Books`, `Readers`, `BorrowRecords` kèm dữ liệu mẫu.

### 3.3. Cấu hình Chuỗi kết nối (Connection String)
Chuỗi kết nối được thiết lập trong tầng DAL hoặc file cấu hình `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "LibraryDB": "Server=.;Database=LibraryDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3.4. Chạy ứng dụng và Chạy Unit Test
- **Chạy ứng dụng bằng Visual Studio:** Nhấn `F5` (chế độ Debug) hoặc `Ctrl + F5` (chế độ Release).
- **Chạy ứng dụng qua CLI:**
  ```bash
  dotnet run --project LibraryManagement.UI
  ```
- **Chạy toàn bộ Unit Tests:**
  ```bash
  dotnet test
  ```

## 4. Hệ thống Ghi nhật ký (Logging)
- Log được ghi nhận đồng thời ra màn hình **Console** và file văn bản trong thư mục `Logs/LogData_ddMMyyyy.log`.
- Định dạng mỗi dòng log:
  ```text
  [Timestamp] [LogLevel] [Category] [EventId - EventName] Message (kèm StackTrace nếu là Exception)
  ```
- Phân tầng ghi log (Category Filter):
  - `LibraryManagement.UI`: Mức `Information`
  - `LibraryManagement.BLL`: Mức `Information`
  - `LibraryManagement.DAL`: Mức `Debug`

## 5. Quy trình Quản trị mã nguồn (Git Workflow)
Dự án áp dụng mô hình **Feature Branch Workflow**:
- Nhánh `main`: Bản release ổn định
- Nhánh `feature-database-dal`: Xây dựng script SQL và tầng DAL
- Nhánh `feature-bll-validation`: Xây dựng logic nghiệp vụ
- Nhánh `feature-ui-forms`: Thiết kế giao diện WinForms
- Nhánh `feature-logging-test`: Xây dựng File Logger và bộ Unit Test MSTest
