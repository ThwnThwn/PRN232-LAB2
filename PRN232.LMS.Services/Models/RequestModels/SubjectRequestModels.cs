using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>Payload for creating a subject.</summary>
public class CreateSubjectRequest
{
    [Required(ErrorMessage = "SubjectCode is required.")]
    [StringLength(20, ErrorMessage = "SubjectCode must not exceed 20 characters.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubjectName is required.")]
    [StringLength(100, ErrorMessage = "SubjectName must not exceed 100 characters.")]
    public string SubjectName { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Credit must be between 1 and 20.")]
    public int Credit { get; set; }
}

/// <summary>Payload for updating a subject.</summary>
public class UpdateSubjectRequest
{
    [Required(ErrorMessage = "SubjectCode is required.")]
    [StringLength(20, ErrorMessage = "SubjectCode must not exceed 20 characters.")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubjectName is required.")]
    [StringLength(100, ErrorMessage = "SubjectName must not exceed 100 characters.")]
    public string SubjectName { get; set; } = string.Empty;

    [Range(1, 20, ErrorMessage = "Credit must be between 1 and 20.")]
    public int Credit { get; set; }
}

/// <summary>Query parameters for GET /api/subjects.</summary>
public class SubjectQueryRequest : QueryParameters
{
    /// <summary>Optional filter: subjects with exactly this number of credits.</summary>
    public int? Credit { get; set; }
}
