using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V2;

[ApiVersion(2.0)]
[Authorize]
[Route("api/v{version:apiVersion}/semesters")]
public class SemestersController : ApiControllerBase
{
    private readonly ISemesterService _semesterService;

    public SemestersController(ISemesterService semesterService)
        => _semesterService = semesterService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSemesters([FromQuery] SemesterQueryRequest request)
        => Success(await _semesterService.GetSemestersV2Async(request));

    [HttpGet("{id:int}", Name = "GetSemesterByIdV2")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResponseV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSemesterById([FromRoute] int id)
        => Success(await _semesterService.GetSemesterByIdV2Async(id));
}
