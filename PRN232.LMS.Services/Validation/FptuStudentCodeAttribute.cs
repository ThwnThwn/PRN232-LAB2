using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PRN232.LMS.Services.Validation;

/// <summary>
/// Custom validation rule (Lab 2 requirement: "at least ONE custom validation rule").
/// Validates the FPTU student-code style: SE19886, CE18793, IA19003, ...
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class FptuStudentCodeAttribute : ValidationAttribute
{
    // [LAB2-03] Kế thừa ValidationAttribute để dùng [FptuStudentCode] trên request property.
    // ^...$ buộc khớp toàn chuỗi; (SE|CE|...) chọn tiền tố; [0-9]{5,6} là 5-6 chữ số.
    // IgnoreCase nhận cả se182422; service chuẩn hóa thành chữ hoa trước khi lưu.
    private static readonly Regex Pattern = new(
        @"^(SE|CE|IA|AI|MC|GD)[0-9]{5,6}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public FptuStudentCodeAttribute()
        : base("StudentCode must follow the FPTU format, e.g. SE19886 or CE18793.")
    {
    }

    // MVC gọi IsValid khi kiểm tra attribute. true = hợp lệ, false = thêm lỗi vào ModelState.
    // null được chấp nhận vì StudentCode tùy chọn. Attribute này không truy vấn database,
    // nên tính duy nhất của mã phải được kiểm tra ở StudentService.
    public override bool IsValid(object? value)
        => value switch
        {
            null => true, // let [Required] handle missing values
            string s => Pattern.IsMatch(s.Trim()),
            _ => false
        };
}
