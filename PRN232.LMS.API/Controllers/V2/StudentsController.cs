using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V2;

// [LAB2-08] URL /api/v2/students chọn controller này nhờ [ApiVersion(2.0)].
// Controller V1 dùng cùng route template nhưng metadata version là 1.0.
// Tên thư mục/namespace V2 giúp tổ chức code; riêng tên V2 không tự tạo versioning.
[ApiVersion(2.0)]
[Authorize]
[Route("api/v{version:apiVersion}/students")]
public class StudentsController : ApiControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
        => _studentService = studentService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents([FromQuery] StudentQueryRequest request)
        // Cùng dữ liệu gốc và quy tắc query, nhưng service chọn MapV2 -> thêm mã SV, điện thoại, tuổi.
        => Success(await _studentService.GetStudentsV2Async(request));

    // Named route có tên riêng để không trùng GetStudentById của V1.
    // V2 trong bài chỉ mở API đọc; CRUD tiếp tục qua /api/v1/students.
    [HttpGet("{id:int}", Name = "GetStudentByIdV2")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponseV2>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentById([FromRoute] int id)
        => Success(await _studentService.GetStudentByIdV2Async(id));
}
