using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>Payload for creating an enrollment.</summary>
public class CreateEnrollmentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "StudentId must be a positive integer.")]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "CourseId must be a positive integer.")]
    public int CourseId { get; set; }

    /// <summary>When omitted, the current date/time is used.</summary>
    public DateTime? EnrollDate { get; set; }

    /// <summary>When omitted, "Active" is used.</summary>
    [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
    public string? Status { get; set; }
}

/// <summary>Payload for updating an enrollment.</summary>
public class UpdateEnrollmentRequest
{
    [Required(ErrorMessage = "Status is required.")]
    [StringLength(20, ErrorMessage = "Status must not exceed 20 characters.")]
    public string Status { get; set; } = string.Empty;

    public DateTime? EnrollDate { get; set; }
}

/// <summary>Query parameters for GET /api/enrollments.</summary>
public class EnrollmentQueryRequest : QueryParameters
{
    /// <summary>Optional filter: enrollments of a student.</summary>
    public int? StudentId { get; set; }

    /// <summary>Optional filter: enrollments into a course.</summary>
    public int? CourseId { get; set; }

    /// <summary>Optional filter: enrollments having exactly this status (e.g. Active).</summary>
    public string? Status { get; set; }
}
