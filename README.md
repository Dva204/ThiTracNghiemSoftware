# ANEDUCATION - Smart Technical Exam Software

**ANEDUCATION** là phần mềm luyện thi và quản lý ngân hàng câu hỏi kỹ thuật thông minh, được thiết kế để hỗ trợ người dùng ôn tập hiệu quả thông qua các bộ đề thi trắc nghiệm chuyên nghiệp.

---

## 🚀 Tính Năng Chính

### 1. Thi & Luyện Tập (Exam & Practice)
*   Giao diện thi hiện đại, sạch sẽ và tập trung cao.
*   Hỗ trợ đếm ngược thời gian làm bài.
*   Hệ thống âm thanh cảnh báo và thông báo kết quả sinh động.
*   Hiển thị hình ảnh minh họa cho câu hỏi (nếu có).

### 2. Quản Lý Bộ Sưu Tập (Collection Manager)
*   Tổ chức câu hỏi theo từng danh mục/chủ đề chuyên biệt.
*   **Smart Import:** Nhập liệu tự động hàng nghìn câu hỏi từ file Microsoft Word (.docx).
*   Quản lý file đề thi cục bộ (Local Storage) linh hoạt.

### 3. Trình Soạn Thảo Câu Hỏi Thông Minh (Smart Question Builder - Pro Only)
*   Cho phép thêm, sửa, xóa câu hỏi thủ công.
*   **Hỗ trợ Hình ảnh:** Đính kèm hình ảnh minh họa cho từng câu hỏi dễ dàng qua nút Browse.
*   Tự động quản lý thư mục `Media` để đảm bảo bộ đề vẫn đầy đủ hình ảnh khi di chuyển.

### 4. Thống Kê & Phân Tích (History & Analytics)
*   Lưu lịch sử mọi lần thi.
*   Theo dõi độ chính xác (Accuracy), tổng số câu đã làm và chuỗi ngày học tập (Streak).
*   **Smart Practice:** Tính năng luyện tập lại các câu đã trả lời sai (Chỉ dành cho Premium).

---

## 💎 Hệ Thống Bản Quyền (Premium System)

Hệ thống sử dụng cơ chế kích hoạt **Offline dựa trên Hardware ID (Mã máy)** để đảm bảo tính duy nhất và vĩnh viễn cho thiết bị.

### 💰 Chính Sách Ưu Đãi
*   **Giá gốc:** 350.000 VNĐ.
*   **Ưu đãi ra mắt:** **99.000 VNĐ** (Vĩnh viễn theo máy).
*   **Liên hệ:** Zalo **0386137558** để lấy mã kích hoạt.

### 🛡️ Cơ Chế Kỹ Thuật
*   **Mã máy (HWID):** Được băm (Hash) từ các thông tin phần cứng duy nhất của thiết bị.
*   **Thuật toán:** SHA256 kết hợp với SECRET_KEY bí mật.
*   **Master Key:** `TN-MASTER-FULL-ACCESS` - Cho phép truy cập toàn quyền trên mọi thiết bị (Dùng cho cứu hộ/test).

---

## 🛠️ Cấu Trúc Dự Án (Technical Structure)

*   **Models/:** Định nghĩa các đối tượng dữ liệu (`Question`, `ExamBank`, `Collection`, `HistoryRecord`, `AppSettings`).
*   **Services/:** Chứa logic xử lý nghiệp vụ chính:
    *   `LicenseService.cs`: Quản lý bản quyền và lấy mã máy.
    *   `QuestionParserService.cs`: Trình phân tích file Word.
    *   `StorageService.cs`: Quản lý lưu trữ file JSON và hình ảnh.
    *   `HistoryService.cs`: Quản lý lịch sử thi.
*   **Views/:** Các cửa sổ giao diện XAML (`HomeWindow`, `ThiTracNghiemWindow`, `EditorWindow`, `SettingsWindow`, `KeyGenWindow`).
*   **Storage/ (Thư mục):** Nơi lưu trữ dữ liệu người dùng và các bộ đề.

---

## 🔑 Hướng Dẫn Cho Quản Trị Viên (Admin)

### Cách Tạo Key Cho Khách Hàng
1.  Truy cập vào cửa sổ **KeyGenWindow** (Công cụ tạo key nội bộ).
2.  Yêu cầu khách hàng gửi mã máy hiện trên ứng dụng của họ.
3.  Nhập mã máy vào ô `DEVICE ID`.
4.  Nhấn nút **GENERATE** để nhận mã kích hoạt và gửi cho khách.

### Thông Tin Bí Mật
*   **Chuỗi bí mật (Secret Key):** `dovanan250820004` (Dùng trong thuật toán băm key).
*   **Master Key:** `TN-MASTER-FULL-ACCESS`.

---

## 💻 Yêu Cầu Hệ Thống
*   Hệ điều hành: Windows 10/11.
*   Framework: .NET Core / .NET Desktop SDK.
*   Microsoft Word (Để dùng tính năng Import).

---
© 2026 **DVA204** - Developed with Antigravity AI.