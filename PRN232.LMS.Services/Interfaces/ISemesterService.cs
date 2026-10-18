using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Business logic for semesters. Implemented in the Service layer.</summary>
public interface ISemesterService
{
    Task<PagedResult<object>> GetSemestersAsync(SemesterQueryRequest query, CancellationToken ct = default);

    /// <summary>API v2: adds the number of courses per semester.</summary>
    Task<PagedResult<object>> GetSemestersV2Async(SemesterQueryRequest query, CancellationToken ct = default);

    Task<SemesterResponse> GetSemesterByIdAsync(int id, CancellationToken ct = default);

    /// <summary>API v2 detail.</summary>
    Task<SemesterResponseV2> GetSemesterByIdV2Async(int id, CancellationToken ct = default);

    Task<SemesterResponse> CreateSemesterAsync(CreateSemesterRequest request, CancellationToken ct = default);

    Task<SemesterResponse> UpdateSemesterAsync(int id, UpdateSemesterRequest request, CancellationToken ct = default);

    Task DeleteSemesterAsync(int id, CancellationToken ct = default);
}
