namespace PRN232.LMS.Services.Models.BusinessModels;

/// <summary>
/// Business Model — the shape the Service layer uses for business processing
/// of an enrollment request (distinct from the Request / Entity models).
/// The business rules are evaluated against this model inside the Service layer.
/// </summary>
public class EnrollmentBusinessModel
{
    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public DateTime EnrollDate { get; set; }

    public string Status { get; set; } = "Active";

    public string StudentName { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public bool IsValid => StudentId > 0 && CourseId > 0;
}
