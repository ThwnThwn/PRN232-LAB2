using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Business logic for subjects. Implemented in the Service layer.</summary>
public interface ISubjectService
{
    Task<PagedResult<object>> GetSubjectsAsync(SubjectQueryRequest query, CancellationToken ct = default);

    /// <summary>API v2: adds a human-friendly display name.</summary>
    Task<PagedResult<object>> GetSubjectsV2Async(SubjectQueryRequest query, CancellationToken ct = default);

    Task<SubjectResponse> GetSubjectByIdAsync(int id, CancellationToken ct = default);

    /// <summary>API v2 detail.</summary>
    Task<SubjectResponseV2> GetSubjectByIdV2Async(int id, CancellationToken ct = default);

    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default);

    Task<SubjectResponse> UpdateSubjectAsync(int id, UpdateSubjectRequest request, CancellationToken ct = default);

    Task DeleteSubjectAsync(int id, CancellationToken ct = default);
}
