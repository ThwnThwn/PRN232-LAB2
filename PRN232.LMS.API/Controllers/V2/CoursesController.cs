using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V2;

[ApiVersion(2.0)]
[Authorize]
[Route("api/v{version:apiVersion}/courses")]
public class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
        => _courseService = courseService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourses([FromQuery] CourseQueryRequest request)
        => Success(await _courseService.GetCoursesV2Async(request));

    [HttpGet("{id:int}", Name = "GetCourseByIdV2")]
    [ProducesResponseType(typeof(ApiResponse<CourseResponseV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseById([FromRoute] int id)
        => Success(await _courseService.GetCourseByIdV2Async(id));

    [HttpGet("{courseId:int}/students")]
    public async Task<IActionResult> GetCourseStudents([FromRoute] int courseId, [FromQuery] StudentQueryRequest request)
        => Success(await _courseService.GetCourseStudentsAsync(courseId, request));
}
