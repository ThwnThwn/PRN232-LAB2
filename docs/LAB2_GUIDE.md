# Hướng dẫn Lab 2

## 1. Cấu trúc kế thừa từ Lab 1

- `PRN232.LMS.API`: controller, model binding, formatter, middleware, JWT configuration, Swagger.
- `PRN232.LMS.Services`: nghiệp vụ CRUD, query, validation, authentication, password/token generator.
- `PRN232.LMS.Repositories`: entity, DbContext, repository, seed dữ liệu LMS, truy vấn SQL.

Controller chỉ gọi service và bọc response. User/RefreshToken entity không trả trực tiếp cho client.
Các bảng LMS và dữ liệu mẫu được giữ từ Lab 1. Database Lab 2 chạy riêng với Compose project
`prn232-lms-lab2`, không sử dụng container/database của bài Lab 1.

## 2. Đối chiếu yêu cầu

| Yêu cầu Lab 2 | Phần triển khai |
| --- | --- |
| 3 layers, DI | 3 project; `AddRepositories`, `AddApplicationServices` |
| User table | `Repositories/Entities/User.cs`; BCrypt hash; role Admin/Student |
| Content negotiation | JSON và XML; XML serializer + custom formatter cho dictionary/expand; 406 khi Accept không hỗ trợ |
| Model binding | `[FromRoute]`, `[FromQuery]`, `[FromBody]`, `[FromHeader(Name="X-Request-Id")]` |
| Data validation | Đủ 6 loại attribute; FluentValidation cho student/enrollment; `[FptuStudentCode]` |
| Routing | URL version, integer constraints, nested course/students, named route tạo Location |
| Middleware | Global exception handling; request logging gồm query marker, status, duration |
| Login và JWT | `AuthController`, `AuthService`, `JwtTokenGenerator`; issuer/audience/signature/lifetime validation |
| Authorization | Resource `[Authorize]`; POST/PUT/DELETE và users `[Authorize(Roles="Admin")]` |
| Refresh token | Hash token trong database; rotation nguyên tử; token đã dùng trả 401 và thu hồi phiên refresh |
| Docker | API + SQL Server, health dependency, tự seed; JWT key truyền bằng environment variable |
| Swagger | 2 document v1/v2; Bearer Authorize; login/refresh được khai báo anonymous |

## 3. Chạy và demo bằng Swagger

```powershell
docker compose up --build -d
```

1. Vào `http://localhost:8080/swagger` và chọn document **v1**.
2. Mở `POST /api/v1/auth/login`, chọn Try it out, nhập:

```json
{ "username": "admin", "password": "Admin@123" }
```

3. Sao chép `data.accessToken`, bấm **Authorize**, dán token (không cần thêm chữ Bearer).
4. Gọi list/detail hoặc thêm/sửa/xóa. Đổi response type sang XML để kiểm tra content negotiation.
5. Dùng `student / Student@123` để demo đọc thành công nhưng ghi hoặc GET users trả **403**.
6. Gửi `data.refreshToken` đến refresh endpoint. Token cũ chỉ dùng được một lần.

Đăng nhập và refresh còn có alias `/api/auth/login` và `/api/auth/refresh-token` theo handout.
Các resource Lab 1 dùng đường dẫn versioned `/api/v1/...` trong bản nâng cấp.

| Tình huống | HTTP |
| --- | --- |
| Chưa có JWT hoặc JWT sai | 401 |
| JWT đúng nhưng role không được phép | 403 |
| Dữ liệu thiếu/sai, field JSON không thuộc request model | 400 + errors chứa tên field |
| ID không tồn tại | 404 |
| Accept không hỗ trợ | 406 |
| Lỗi server không dự kiến | 500 + thông báo `Internal server error` |

## 4. Query và phiên bản

```text
/api/v1/students?search=nguyen&sort=fullName,-dateOfBirth&page=1&size=5
/api/v1/students?fields=studentId,fullName,email
/api/v1/students?expand=enrollments
/api/v1/enrollments?search=active&sort=-enrollDate&size=20&expand=student,course
/api/v1/courses/1/students?sort=fullName&size=5
/api/v2/students?fields=studentId,studentCode,phoneNumber,age
/api/v2/courses?fields=courseId,enrollmentsCount
/api/v2/semesters?fields=semesterId,coursesCount
/api/v2/subjects?fields=subjectCode,displayName
/api/v2/enrollments?fields=enrollmentId,semesterName
```

V1 giữ response model gốc và đầy đủ CRUD. V2 dùng service hiện có, mở rộng API đọc:
student có studentCode/phoneNumber/age; course có enrollmentsCount; semester có coursesCount;
subject có displayName; enrollment có semesterId/semesterName.
Mã sinh viên nhận tiền tố SE/CE/IA/AI/MC/GD và 5-6 chữ số; bỏ qua thì service tự tạo mã.
List response có data là mảng và pagination ở ngoài data, đúng contract Lab 1.

## 5. Cấu hình

Compose chạy ngay với cấu hình demo. Muốn ghi đè, sao chép `.env.example` thành `.env`:

```powershell
Copy-Item .env.example .env
```

- `API_PORT`: cổng host, mặc định 8080.
- `JWT_SECRET`: khóa HMAC, tối thiểu 32 byte; truyền vào container thành `Jwt__Key`.
- `DB_PASSWORD`: mật khẩu SQL Server demo; không publish cổng SQL ra host.
- `JWT_EXPIRY_SECONDS`: thời gian sống access token, mặc định 3600.
- `JWT_USE_COOKIE=true`: refresh token qua HttpOnly/SameSite=Strict cookie, hỗ trợ refresh body trống.
- `JWT_COOKIE_SECURE=true`: chỉ gửi cookie qua HTTPS.

Chạy trực tiếp bằng .NET cần SQL Server/LocalDB và khóa JWT qua biến môi trường:

```powershell
$env:Jwt__Key = 'prn232-lab2-local-development-signing-key'
dotnet run --project PRN232.LMS.API
```

Cấu hình connection string local tại `appsettings.Development.json` hoặc
`ConnectionStrings__DefaultConnection`. Docker tự ghi đè connection string.
`EnsureCreated` chỉ tạo schema khi database mới; thay đổi schema phải tạo database mới hoặc bổ sung migrations.

## 6. Kiểm tra và đóng gói

```powershell
dotnet build PRN232.LMS.sln -c Release
pwsh -File scripts/Test-Lab2.ps1
pwsh -File scripts/Pack-Submission.ps1
```

Script integration kiểm tra login, phân quyền, signature, refresh/replay, input validation,
CRUD student và Location, XML lớn, hai phiên bản, paging/fields/expand. Kết quả ghi ở
`test-results/integration.json`, không đưa vào ZIP.

ZIP là `PRN232_LAB2_SE182422_VANCHAUTRUONGTHUAN.zip`, solution nằm ngay root,
không có bin/obj/.vs/.git/.env hay database backup. `submission.json` khai báo chính xác endpoints/tài khoản.
Có thể dùng `Grade-Lab2.ps1` trong bộ chấm kèm đề để chấm ZIP trên Docker từ database mới.

Dừng container nhưng giữ dữ liệu: `docker compose down`.
Chỉ khi muốn xóa dữ liệu Lab 2 và seed lại: `docker compose down -v` rồi chạy lại.
