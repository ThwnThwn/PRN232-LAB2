using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Business logic for enrollments. Implemented in the Service layer.</summary>
public interface IEnrollmentService
{
    Task<PagedResult<object>> GetEnrollmentsAsync(EnrollmentQueryRequest query, CancellationToken ct = default);

    /// <summary>API v2: adds the semester of the course as flat fields.</summary>
    Task<PagedResult<object>> GetEnrollmentsV2Async(EnrollmentQueryRequest query, CancellationToken ct = default);

    Task<EnrollmentResponse> GetEnrollmentByIdAsync(int id, CancellationToken ct = default);

    /// <summary>API v2 detail.</summary>
    Task<EnrollmentResponseV2> GetEnrollmentByIdV2Async(int id, CancellationToken ct = default);

    Task<EnrollmentResponse> CreateEnrollmentAsync(CreateEnrollmentRequest request, CancellationToken ct = default);

    Task<EnrollmentResponse> UpdateEnrollmentAsync(int id, UpdateEnrollmentRequest request, CancellationToken ct = default);

    Task DeleteEnrollmentAsync(int id, CancellationToken ct = default);
}
