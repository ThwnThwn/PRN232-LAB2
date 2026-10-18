using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>Payload for creating a semester.</summary>
public class CreateSemesterRequest
{
    [Required(ErrorMessage = "SemesterName is required.")]
    [StringLength(100, ErrorMessage = "SemesterName must not exceed 100 characters.")]
    public string SemesterName { get; set; } = string.Empty;

    [Range(typeof(DateTime), "1900-01-01", "9999-12-31", ErrorMessage = "StartDate must be on or after 1900-01-01.")]
    public DateTime StartDate { get; set; }

    [Range(typeof(DateTime), "1900-01-01", "9999-12-31", ErrorMessage = "EndDate must be on or after 1900-01-01.")]
    public DateTime EndDate { get; set; }
}

/// <summary>Payload for updating a semester.</summary>
public class UpdateSemesterRequest
{
    [Required(ErrorMessage = "SemesterName is required.")]
    [StringLength(100, ErrorMessage = "SemesterName must not exceed 100 characters.")]
    public string SemesterName { get; set; } = string.Empty;

    [Range(typeof(DateTime), "1900-01-01", "9999-12-31", ErrorMessage = "StartDate must be on or after 1900-01-01.")]
    public DateTime StartDate { get; set; }

    [Range(typeof(DateTime), "1900-01-01", "9999-12-31", ErrorMessage = "EndDate must be on or after 1900-01-01.")]
    public DateTime EndDate { get; set; }
}

/// <summary>Query parameters for GET /api/semesters.</summary>
public class SemesterQueryRequest : QueryParameters
{
}
