using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V1;

/// <summary>
/// RESTful endpoints for the "courses" resource — API version 1.
/// Also demonstrates a nested resource: /api/v1/courses/{courseId}/students.
/// </summary>
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/courses")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    /// <summary>Gets a paged list of courses. Supports ?search, ?sort, ?page, ?size, ?fields, ?expand, ?semesterId.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourses([FromQuery] CourseQueryRequest request)
        => Success(await _courseService.GetCoursesAsync(request));

    /// <summary>Gets one course with its complete related data (semester + enrollments). 404 if it does not exist.</summary>
    [HttpGet("{id:int}", Name = "GetCourseById")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourseById([FromRoute] int id)
        => Success(await _courseService.GetCourseByIdAsync(id));

    /// <summary>Nested resource: the students enrolled in the given course (supports search/sort/page/fields).</summary>
    // [LAB2-04] Nested resource: GET /api/v1/courses/1/students lấy sinh viên CỦA course 1.
    // Route gốc /courses + template /{courseId:int}/students tạo URL đầy đủ.
    // [LAB2-02] courseId từ đường dẫn, các tùy chọn page/size/search từ query string.
    // Controller chỉ truyền courseId và request; lọc enrollment/student thực hiện trong service.
    // :int chỉ kiểm tra kiểu URL; courseId=999 vẫn có thể khớp và cần service kiểm tra tồn tại.
    [HttpGet("{courseId:int}/students", Name = "GetCourseStudents")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCourseStudents([FromRoute] int courseId, [FromQuery] StudentQueryRequest request)
        => Success(await _courseService.GetCourseStudentsAsync(courseId, request));

    /// <summary>Creates a course. Returns 201 when created.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
    {
        var created = await _courseService.CreateCourseAsync(request);
        return CreatedAtRouteResponse("GetCourseById", new { id = created.CourseId }, created, "Course created successfully.");
    }

    /// <summary>Updates a course. 404 if it does not exist.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateCourse([FromRoute] int id, [FromBody] UpdateCourseRequest request)
        => Success(await _courseService.UpdateCourseAsync(id, request), "Course updated successfully.");

    /// <summary>Deletes a course (and its enrollments). Admin-only endpoint. 404 if it does not exist.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteCourse([FromRoute] int id)
    {
        await _courseService.DeleteCourseAsync(id);
        return Success<object?>(null, $"Course with id {id} was deleted successfully.");
    }
}
