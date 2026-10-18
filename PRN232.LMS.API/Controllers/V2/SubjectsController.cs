using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V2;

[ApiVersion(2.0)]
[Authorize]
[Route("api/v{version:apiVersion}/subjects")]
public class SubjectsController : ApiControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService)
        => _subjectService = subjectService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjects([FromQuery] SubjectQueryRequest request)
        => Success(await _subjectService.GetSubjectsV2Async(request));

    [HttpGet("{id:int}", Name = "GetSubjectByIdV2")]
    [ProducesResponseType(typeof(ApiResponse<SubjectResponseV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjectById([FromRoute] int id)
        => Success(await _subjectService.GetSubjectByIdV2Async(id));
}
