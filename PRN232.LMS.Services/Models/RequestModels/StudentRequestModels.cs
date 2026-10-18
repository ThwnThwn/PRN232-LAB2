using System.ComponentModel.DataAnnotations;
using PRN232.LMS.Services.Validation;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>
/// Payload for creating a student.
/// Lab 2: every client input is validated (data annotations + custom rule + FluentValidation).
/// </summary>
public class CreateStudentRequest
{
    // [LAB2-03] Data annotations đặt trên INPUT model để MVC kiểm tra dữ liệu client gửi.
    // [Required]: không chấp nhận null/chuỗi rỗng hoặc chỉ có khoảng trắng.
    // [StringLength]: giới hạn chiều dài, tránh input quá dài so với hợp đồng/database.
    // Ví dụ thiếu fullName -> ModelState lỗi; factory ở Program.cs tạo 400 + errors.
    [Required(ErrorMessage = "FullName is required.")]
    [StringLength(100, ErrorMessage = "FullName must not exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    // [EmailAddress] kiểm tra định dạng, không chứng minh email tồn tại hay thuộc người gọi.
    // Email có bị trùng không phải kiểm tra database trong StudentService (nghiệp vụ).
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    // Thiếu ngày sinh thì DateTime nhận giá trị mặc định; FluentValidation sẽ từ chối.
    // Rule bắt buộc, >= 1900-01-01 và trước hôm nay nằm ở CreateStudentRequestValidator.
    public DateTime DateOfBirth { get; set; }

    /// <summary>FPTU student code — optional. If omitted the service will generate one automatically.</summary>
    [StringLength(10, ErrorMessage = "StudentCode must not exceed 10 characters.")]
    // [LAB2-03] Custom attribute do mình viết: SE/CE/... + 5 hoặc 6 chữ số.
    // SE19886 và SE182422 hợp lệ; XX123 không hợp lệ. Xem FptuStudentCodeAttribute.cs.
    // Nullable vì được phép bỏ qua; service tự sinh mã khi tạo sinh viên.
    [FptuStudentCode]
    public string? StudentCode { get; set; }

    // [Phone] kiểm tra kiểu số điện thoại; [RegularExpression] bổ sung quy tắc của bài:
    // ^ = đầu chuỗi, 0 = bắt đầu bằng số 0, \d{9,10} = thêm 9-10 chữ số, $ = cuối chuỗi.
    // Kết quả chấp nhận tổng 10-11 chữ số, ví dụ 0987654321. Thuộc tính vẫn là tùy chọn.
    [Phone(ErrorMessage = "PhoneNumber is not a valid phone number.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "PhoneNumber must start with 0 and contain 10-11 digits.")]
    [StringLength(15, ErrorMessage = "PhoneNumber must not exceed 15 characters.")]
    public string? PhoneNumber { get; set; }
}

/// <summary>Payload for updating a student.</summary>
public class UpdateStudentRequest
{
    // [LAB2-03] PUT nhận đầy đủ các trường bắt buộc tên/email/ngày sinh giống create.
    // StudentCode là tùy chọn: bỏ qua thì service giữ mã cũ, không sinh lại.
    // Các attribute chạy ở MVC; UpdateStudentRequestValidator kiểm tra thêm trong service.
    [Required(ErrorMessage = "FullName is required.")]
    [StringLength(100, ErrorMessage = "FullName must not exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    /// <summary>FPTU student code — optional. If omitted the service will keep the existing value.</summary>
    [StringLength(10, ErrorMessage = "StudentCode must not exceed 10 characters.")]
    [FptuStudentCode]
    public string? StudentCode { get; set; }

    [Phone(ErrorMessage = "PhoneNumber is not a valid phone number.")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "PhoneNumber must start with 0 and contain 10-11 digits.")]
    [StringLength(15, ErrorMessage = "PhoneNumber must not exceed 15 characters.")]
    public string? PhoneNumber { get; set; }
}

/// <summary>Query parameters for GET /api/students.</summary>
public class StudentQueryRequest : QueryParameters
{
    // [LAB2-02] Kế thừa page/size/search/... từ QueryParameters và thêm bộ lọc studentCode.
    // GET /api/v1/students?studentCode=SE182422 -> StudentCode = "SE182422".
    /// <summary>Optional filter: student code (v1 also supports it as a condition filter).</summary>
    public string? StudentCode { get; set; }
}
