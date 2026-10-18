using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V1;

/// <summary>RESTful endpoints for the "semesters" resource — API version 1.</summary>
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/semesters")]
public class SemestersController : ApiControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
    {
        _semesterService = semesterService;
    }

    /// <summary>Gets a paged list of semesters. Supports ?search, ?sort, ?page, ?size, ?fields, ?expand.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSemesters([FromQuery] SemesterQueryRequest request)
        => Success(await _semesterService.GetSemestersAsync(request));

    /// <summary>Gets one semester with its complete related data (courses). 404 if it does not exist.</summary>
    [HttpGet("{id:int}", Name = "GetSemesterById")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSemesterById([FromRoute] int id)
        => Success(await _semesterService.GetSemesterByIdAsync(id));

    /// <summary>Creates a semester. Returns 201 when created.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request)
    {
        var created = await _semesterService.CreateSemesterAsync(request);
        return CreatedAtRouteResponse("GetSemesterById", new { id = created.SemesterId }, created, "Semester created successfully.");
    }

    /// <summary>Updates a semester. 404 if it does not exist.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSemester([FromRoute] int id, [FromBody] UpdateSemesterRequest request)
        => Success(await _semesterService.UpdateSemesterAsync(id, request), "Semester updated successfully.");

    /// <summary>Deletes a semester (and its courses). Admin-only endpoint. 404 if it does not exist.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSemester([FromRoute] int id)
    {
        await _semesterService.DeleteSemesterAsync(id);
        return Success<object?>(null, $"Semester with id {id} was deleted successfully.");
    }
}
