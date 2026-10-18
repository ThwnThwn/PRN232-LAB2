namespace PRN232.LMS.Services.Models.ResponseModels;

// ---------------------------------------------------------------
// Semester-related response models (Response Model — API output).
// Entities are never returned directly to the client.
// ---------------------------------------------------------------

public class SemesterResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>Filled for GET by id or when ?expand=courses is used on the list API.</summary>
    public IReadOnlyList<CourseBriefResponse>? Courses { get; set; }
}

public class CourseBriefResponse
{
    public int CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;
}

public class SemesterBriefResponse
{
    public int SemesterId { get; set; }

    public string SemesterName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
