using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>Payload for creating a course.</summary>
public class CreateCourseRequest
{
    [Required(ErrorMessage = "CourseName is required.")]
    [StringLength(100, ErrorMessage = "CourseName must not exceed 100 characters.")]
    public string CourseName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "SemesterId must be a positive integer.")]
    // [LAB2-03] [Range] chỉ biết SemesterId > 0, không biết semester đó có trong database.
    // SemesterId=0 -> MVC trả 400. SemesterId=999 -> cần service kiểm tra tồn tại.
    // Ví dụ này phân biệt validation hình dạng/giá trị với kiểm tra nghiệp vụ có truy vấn dữ liệu.
    public int SemesterId { get; set; }
}

/// <summary>Payload for updating a course.</summary>
public class UpdateCourseRequest
{
    [Required(ErrorMessage = "CourseName is required.")]
    [StringLength(100, ErrorMessage = "CourseName must not exceed 100 characters.")]
    public string CourseName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "SemesterId must be a positive integer.")]
    public int SemesterId { get; set; }
}

/// <summary>Query parameters for GET /api/courses.</summary>
public class CourseQueryRequest : QueryParameters
{
    /// <summary>Optional filter: only courses of the given semester.</summary>
    public int? SemesterId { get; set; }
}
