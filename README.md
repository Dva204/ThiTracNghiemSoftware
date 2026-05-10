# ROADMAP PHÁT TRIỂN PHẦN MỀM LUYỆN THI CÁ NHÂN

## Ý tưởng cốt lõi

Đây KHÔNG phải phần mềm bán ngân hàng đề.

Đây là:

> Một Exam Simulation Engine + Study Management System

Người dùng:
- tự import câu hỏi
- tự tạo bộ đề
- tự luyện thi như thi thật

Giá trị cốt lõi:
- trải nghiệm thi
- quản lý ôn tập
- thống kê học tập
- workflow học tập cá nhân

---

# MÔ HÌNH HOẠT ĐỘNG

```text
Word/PDF/TXT
      ↓
   Parser
      ↓
Question Database (SQLite)
      ↓
 Exam Engine
      ↓
Thi thử + Thống kê + Ôn tập