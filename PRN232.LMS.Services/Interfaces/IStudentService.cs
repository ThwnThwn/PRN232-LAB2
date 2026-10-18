using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Business logic for students. Implemented in the Service layer.</summary>
public interface IStudentService
{
    Task<PagedResult<object>> GetStudentsAsync(StudentQueryRequest query, CancellationToken ct = default);

    /// <summary>API v2: same query capabilities, richer response contract.</summary>
    Task<PagedResult<object>> GetStudentsV2Async(StudentQueryRequest query, CancellationToken ct = default);

    Task<StudentResponse> GetStudentByIdAsync(int id, CancellationToken ct = default);

    /// <summary>API v2 detail.</summary>
    Task<StudentResponseV2> GetStudentByIdV2Async(int id, CancellationToken ct = default);

    Task<StudentResponse> CreateStudentAsync(CreateStudentRequest request, CancellationToken ct = default);

    Task<StudentResponse> UpdateStudentAsync(int id, UpdateStudentRequest request, CancellationToken ct = default);

    Task DeleteStudentAsync(int id, CancellationToken ct = default);
}
