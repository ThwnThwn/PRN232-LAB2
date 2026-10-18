namespace PRN232.LMS.Services.Models.ResponseModels;

// ---------------------------------------------------------------
// Enrollment-related response models.
// ---------------------------------------------------------------

public class EnrollmentResponse
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public DateTime EnrollDate { get; set; }

    public string Status { get; set; } = string.Empty;

    /// <summary>Filled when ?expand=student is used on the list API, or on GET by id.</summary>
    public StudentBriefResponse? Student { get; set; }

    /// <summary>Filled when ?expand=course is used on the list API, or on GET by id.</summary>
    public CourseBriefResponse? Course { get; set; }
}

/// <summary>
/// Compact enrollment used inside Student / Course detail responses.
/// (No nested Student/Course here → no circular reference / infinite recursion.)
/// </summary>
public class EnrollmentBriefResponse
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    public string StudentName { get; set; } = string.Empty;

    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public DateTime EnrollDate { get; set; }

    public string Status { get; set; } = string.Empty;
}
