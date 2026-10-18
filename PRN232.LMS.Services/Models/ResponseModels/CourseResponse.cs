namespace PRN232.LMS.Services.Models.ResponseModels;

// ---------------------------------------------------------------
// Course-related response models.
// ---------------------------------------------------------------

public class CourseResponse
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;

    /// <summary>Filled when ?expand=semester is used on the list API.</summary>
    public SemesterBriefResponse? Semester { get; set; }

    /// <summary>Filled for GET by id or when ?expand=enrollments is used on the list API.</summary>
    public IReadOnlyList<EnrollmentBriefResponse>? Enrollments { get; set; }
}
