using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Business logic for courses. Implemented in the Service layer.</summary>
public interface ICourseService
{
    Task<PagedResult<object>> GetCoursesAsync(CourseQueryRequest query, CancellationToken ct = default);

    /// <summary>API v2: adds the number of enrollments per course.</summary>
    Task<PagedResult<object>> GetCoursesV2Async(CourseQueryRequest query, CancellationToken ct = default);

    Task<CourseResponse> GetCourseByIdAsync(int id, CancellationToken ct = default);

    /// <summary>API v2 detail.</summary>
    Task<CourseResponseV2> GetCourseByIdV2Async(int id, CancellationToken ct = default);

    /// <summary>Nested resource (Lab 2): the students enrolled in the given course.</summary>
    Task<PagedResult<object>> GetCourseStudentsAsync(int courseId, StudentQueryRequest query, CancellationToken ct = default);

    Task<CourseResponse> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default);

    Task<CourseResponse> UpdateCourseAsync(int id, UpdateCourseRequest request, CancellationToken ct = default);

    Task DeleteCourseAsync(int id, CancellationToken ct = default);
}
