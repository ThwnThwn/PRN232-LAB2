using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V1;

/// <summary>RESTful endpoints for the "enrollments" resource — API version 1.</summary>
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/enrollments")]
public class EnrollmentsController : ApiControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    /// <summary>
    /// Gets a paged list of enrollments. Supports ?search, ?sort, ?page, ?size, ?fields, ?expand,
    /// plus ?studentId, ?courseId, ?status filters.
    /// Example: /api/v1/enrollments?search=active&amp;sort=-enrollDate&amp;page=1&amp;size=20&amp;fields=enrollmentId,status&amp;expand=student,course
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnrollments([FromQuery] EnrollmentQueryRequest request)
        => Success(await _enrollmentService.GetEnrollmentsAsync(request));

    /// <summary>Gets one enrollment with its complete related data (student + course + semester). 404 if it does not exist.</summary>
    [HttpGet("{id:int}", Name = "GetEnrollmentById")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEnrollmentById([FromRoute] int id)
        => Success(await _enrollmentService.GetEnrollmentByIdAsync(id));

    /// <summary>Creates an enrollment. Returns 201 when created.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateEnrollment([FromBody] CreateEnrollmentRequest request)
    {
        var created = await _enrollmentService.CreateEnrollmentAsync(request);
        return CreatedAtRouteResponse("GetEnrollmentById", new { id = created.EnrollmentId }, created, "Enrollment created successfully.");
    }

    /// <summary>Updates an enrollment (status / enroll date). 404 if it does not exist.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateEnrollment([FromRoute] int id, [FromBody] UpdateEnrollmentRequest request)
        => Success(await _enrollmentService.UpdateEnrollmentAsync(id, request), "Enrollment updated successfully.");

    /// <summary>Deletes an enrollment. Admin-only endpoint. 404 if it does not exist.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteEnrollment([FromRoute] int id)
    {
        await _enrollmentService.DeleteEnrollmentAsync(id);
        return Success<object?>(null, $"Enrollment with id {id} was deleted successfully.");
    }
}
