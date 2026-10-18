using FluentValidation;
using PRN232.LMS.Services.Models.RequestModels;

namespace PRN232.LMS.Services.Validation.Validators;

/// <summary>
/// FluentValidation rules for CreateStudentRequest (Lab 2 requirement).
/// Contains a custom rule (FPTU student code + Vietnamese phone number).
/// </summary>
public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    // [LAB2-03] FluentValidation viết quy tắc bằng C# theo chuỗi RuleFor(...).
    // AbstractValidator<CreateStudentRequest> chỉ rõ model được kiểm tra.
    // DI đăng ký IValidator<CreateStudentRequest> trong ServiceCollectionExtensions.cs.
    // Bài này KHÔNG bật auto-validation của FluentValidation trong MVC:
    // StudentService gọi ValidateAndThrowAsync trước khi ghi dữ liệu.
    // Sai rule -> ValidationException -> GlobalExceptionHandlingMiddleware trả HTTP 400.
    // Vì kiểm tra ở service, các quy tắc vẫn chạy khi service được gọi từ nơi khác ngoài HTTP.
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(100).WithMessage("FullName must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        // Must nhận hàm kiểm tra tùy chỉnh. Đọc UtcNow tại lúc validate thay vì cố định
        // ngày hôm nay khi khởi tạo validator; date.Date bỏ phần giờ để so sánh ngày sinh.
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth is required.")
            .GreaterThanOrEqualTo(new DateTime(1900, 1, 1)).WithMessage("DateOfBirth must be on or after 1900-01-01.")
            .Must(date => date.Date < DateTime.UtcNow.Date).WithMessage("DateOfBirth must be in the past.");

        // --- Custom rule #1: FPTU student code (SE19886 / CE18793 ...) — optional ---
        RuleFor(x => x.StudentCode)
            // Must kiểm tra định dạng; When chỉ chạy rule nếu client có gửi mã không rỗng.
            // Mã bỏ qua được service tự sinh, nên không dùng NotEmpty cho trường tùy chọn này.
            .Must(StudentRules.BeFptuStudentCode)
            .When(x => !string.IsNullOrWhiteSpace(x.StudentCode))
            .WithMessage("StudentCode must follow the FPTU format, e.g. SE19886 or CE18793.");

        // --- Custom rule #2: Vietnamese phone number, optional ---
        RuleFor(x => x.PhoneNumber)
            // Cùng ý tưởng: WithMessage là lỗi trả cho client khi hàm Must trả false.
            .Must(StudentRules.BeVietnamesePhoneNumber)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("PhoneNumber must be a valid Vietnamese phone number (e.g. 0987654321).");
    }

}
