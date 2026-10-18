using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V1;

/// <summary>RESTful endpoints for the "subjects" resource — API version 1.</summary>
[ApiVersion(1.0)]
[Authorize]
[Route("api/v{version:apiVersion}/subjects")]
public class SubjectsController : ApiControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    /// <summary>Gets a paged list of subjects. Supports ?search, ?sort, ?page, ?size, ?fields, ?credit.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubjects([FromQuery] SubjectQueryRequest request)
        => Success(await _subjectService.GetSubjectsAsync(request));

    /// <summary>Gets one subject. 404 if it does not exist.</summary>
    [HttpGet("{id:int}", Name = "GetSubjectById")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSubjectById([FromRoute] int id)
        => Success(await _subjectService.GetSubjectByIdAsync(id));

    /// <summary>Creates a subject. Returns 201 when created.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        var created = await _subjectService.CreateSubjectAsync(request);
        return CreatedAtRouteResponse("GetSubjectById", new { id = created.SubjectId }, created, "Subject created successfully.");
    }

    /// <summary>Updates a subject. 404 if it does not exist.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateSubject([FromRoute] int id, [FromBody] UpdateSubjectRequest request)
        => Success(await _subjectService.UpdateSubjectAsync(id, request), "Subject updated successfully.");

    /// <summary>Deletes a subject. Admin-only endpoint. 404 if it does not exist.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSubject([FromRoute] int id)
    {
        await _subjectService.DeleteSubjectAsync(id);
        return Success<object?>(null, $"Subject with id {id} was deleted successfully.");
    }
}
