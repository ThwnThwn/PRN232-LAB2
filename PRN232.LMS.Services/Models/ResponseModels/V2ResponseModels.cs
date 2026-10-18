namespace PRN232.LMS.Services.Models.ResponseModels;

// [LAB2-08] Versioning tách HỢP ĐỒNG API, không bắt buộc tách entity/table/database.
// StudentResponseV2 kế thừa dữ liệu V1 rồi thêm StudentCode, PhoneNumber, Age.
// Ví dụ cùng GET sinh viên 1: V1 không có age; V2 có age do service tính từ ngày sinh.
// Các DTO ở đây chỉ chứa dữ liệu được phép trả, không phải entity EF có navigation/hash mật khẩu.
// Đổi version chọn mapper/response; đổi Accept chọn JSON/XML. Hai lựa chọn độc lập:
// /api/v2/students + Accept: application/xml = hợp đồng V2 được biểu diễn dưới dạng XML.

// ---------------------------------------------------------------
// API v2 response models (Lab 2 — API versioning).
// v1 keeps the original Lab 1 contract; v2 adds extra information
// so the versioning is clearly visible to clients.
// ---------------------------------------------------------------

/// <summary>Student (v2): adds studentCode, phoneNumber and the computed age.</summary>
public class StudentResponseV2 : StudentResponse
{
    public string StudentCode { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public int Age { get; set; }
}

/// <summary>Course (v2): adds the number of enrollments of the course.</summary>
public class CourseResponseV2 : CourseResponse
{
    public int EnrollmentsCount { get; set; }
}

/// <summary>Semester (v2): adds the number of courses in the semester.</summary>
public class SemesterResponseV2 : SemesterResponse
{
    public int CoursesCount { get; set; }
}

/// <summary>Subject (v2): adds a human-friendly display name.</summary>
public class SubjectResponseV2 : SubjectResponse
{
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Enrollment (v2): adds the semester of the course (flat fields).</summary>
public class EnrollmentResponseV2 : EnrollmentResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;
}
