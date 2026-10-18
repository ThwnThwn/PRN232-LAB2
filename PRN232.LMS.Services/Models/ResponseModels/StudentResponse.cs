namespace PRN232.LMS.Services.Models.ResponseModels;

// ---------------------------------------------------------------
// Student-related response models.
// ---------------------------------------------------------------

public class StudentResponse
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    /// <summary>Number of enrollments of this student (always returned on list APIs).</summary>
    public int EnrollmentsCount { get; set; }

    /// <summary>Filled for GET by id or when ?expand=enrollments is used on the list API.</summary>
    public IReadOnlyList<EnrollmentBriefResponse>? Enrollments { get; set; }
}

public class StudentBriefResponse
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }
}
