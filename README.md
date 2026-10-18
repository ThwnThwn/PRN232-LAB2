# PRN232 - LAB 2: Advanced REST API & Security

**Sinh viên:** SE182422 - VAN CHAU TRUONG THUAN | **Lớp:** SE1917  
**Nền tảng:** ASP.NET Core 8, EF Core 8, SQL Server 2022; nâng cấp từ Lab 1.

## Chạy

Cần Docker Desktop đang chạy. Mở terminal tại thư mục chứa solution:

```powershell
docker compose up --build -d
```

Swagger: http://localhost:8080/swagger | Health: http://localhost:8080/health  
Đổi cổng bằng biến `API_PORT`. Compose tự tạo database và seed khi khởi động.

## Tài khoản và phân quyền

| Username | Password | Role |
| --- | --- | --- |
| admin | Admin@123 | Admin |
| student | Student@123 | Student |

Gọi `POST /api/v1/auth/login`, sao chép `data.accessToken` vào **Authorize** trên Swagger.
Mọi resource cần JWT; Student được đọc, Admin được thêm/sửa/xóa và `GET /api/v1/users`.
`POST /api/v1/auth/refresh-token` nhận `{ "refreshToken": "..." }` và trả cặp token mới.
Mật khẩu dùng BCrypt; refresh token lưu SHA-256, luân chuyển và chống sử dụng lại.

## Dữ liệu và API

Seed: **5 semesters, 10 subjects, 20 courses, 50 students, 500 enrollments, 2 users**.
V1 có CRUD cho cả 5 resource; V2 mở rộng dữ liệu trả về khi đọc.
Các list API giữ `search`, `sort`, `page`, `size`, `fields`, `expand` và metadata `pagination`.
Hỗ trợ JSON/XML qua `Accept`; định dạng không hỗ trợ trả 406.
Data annotations, FluentValidation, custom student-code validation; lỗi trả envelope thống nhất.
Nested route: `/api/v1/courses/1/students`; `X-Request-Id` được trả lại trong response header.
Middleware ghi method, path + query, status và thời gian ra stdout; lỗi 500 không lộ chi tiết.

## Kiểm tra và giới hạn

`dotnet build PRN232.LMS.sln -c Release`; `pwsh -File scripts/Test-Lab2.ps1` kiểm tra API đang chạy.
`pwsh -File scripts/Pack-Submission.ps1` tạo ZIP nộp bài, loại bin/obj và `.env`.
Hướng dẫn chi tiết: [docs/LAB2_GUIDE.md](docs/LAB2_GUIDE.md).
V2 chỉ có API đọc. Dự án dùng `EnsureCreated`, chưa dùng EF migrations; schema mới cần database mới.
Compose có khóa/mật khẩu demo để chấm tự động; có thể ghi đè qua `.env` theo `.env.example`.
