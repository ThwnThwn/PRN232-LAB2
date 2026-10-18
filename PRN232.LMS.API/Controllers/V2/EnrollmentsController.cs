using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V2;

[ApiVersion(2.0)]
[Authorize]
[Route("api/v{version:apiVersion}/enrollments")]
public class EnrollmentsController : ApiControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
        => _enrollmentService = enrollmentService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollments([FromQuery] EnrollmentQueryRequest request)
        => Success(await _enrollmentService.GetEnrollmentsV2Async(request));

    [HttpGet("{id:int}", Name = "GetEnrollmentByIdV2")]
    [ProducesResponseType(typeof(ApiResponse<EnrollmentResponseV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEnrollmentById([FromRoute] int id)
        => Success(await _enrollmentService.GetEnrollmentByIdV2Async(id));
}
