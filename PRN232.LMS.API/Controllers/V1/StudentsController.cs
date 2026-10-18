using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers.V1;

/// <summary>RESTful endpoints for the "students" resource — API version 1.</summary>
[ApiVersion(1.0)]
// [LAB2-06] Mọi action ở controller này yêu cầu người gọi có danh tính được xác thực.
// GET chấp nhận Admin và Student; action ghi có thêm yêu cầu Roles = "Admin" ở bên dưới.
[Authorize]
// [LAB2-04/08] Attribute routing tạo URL gốc; {version:apiVersion} do thư viện đọc/kiểm tra.
// Cộng route gốc với template của action: GET + gốc + /{id:int} -> lấy chi tiết.
[Route("api/v{version:apiVersion}/students")]
public class StudentsController : ApiControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    /// <summary>
    /// Gets a paged list of students. Supports ?search, ?sort, ?page, ?size, ?fields, ?expand, ?studentCode.
    /// Demonstrates HEADER binding: X-Request-Id is echoed in the response header.
    /// </summary>
    // [LAB2-02] Ví dụ request:
    // GET /api/v1/students?search=nguyen&page=2&size=5, header X-Request-Id: demo-001.
    // [FromQuery] tạo StudentQueryRequest { Search = "nguyen", Page = 2, Size = 5 }.
    // [FromHeader] lấy riêng header thành requestId = "demo-001"; thiếu thì có thể null.
    // Nếu page=abc, chuyển sang int thất bại -> ModelState lỗi -> [ApiController] trả 400.
    // Binding chỉ lấy/chuyển dữ liệu. Việc tên/email hợp lệ hay không thuộc validation.
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] StudentQueryRequest request,
        [FromHeader(Name = "X-Request-Id")] string? requestId)
    {
        if (!string.IsNullOrWhiteSpace(requestId))
        {
            // Echo header để client có thể đối chiếu request/response khi kiểm tra.
            Response.Headers["X-Request-Id"] = requestId;
        }
        // [LAB2-01] Service trả dữ liệu C#; Success bọc envelope. Accept quyết định JSON/XML sau return.
        // [LAB2-02] Controller truyền request đã bind xuống service; không tự truy vấn SQL ở đây.
        return Success(await _studentService.GetStudentsAsync(request));
    }

    /// <summary>
    /// Gets one student with its complete related data (enrollments). 404 if it does not exist.
    /// Demonstrates ROUTE binding: [FromRoute] id with the {id:int} route constraint.
    /// </summary>
    // [LAB2-04] Constraint :int làm /students/abc không khớp action này (thường trả 404).
    // /students/5 khớp route, [FromRoute] gán id = 5 rồi service kiểm tra sinh viên có tồn tại.
    // Tên GetStudentById được CreateStudent dùng để tạo Location; tên không phải URL.
    [HttpGet("{id:int}", Name = "GetStudentById")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStudentById([FromRoute] int id)
        => Success(await _studentService.GetStudentByIdAsync(id));

    /// <summary>Creates a student. Returns 201 when created.</summary>
    // [LAB2-02/03] [FromBody] đọc body qua input formatter theo Content-Type.
    // Content-Type: application/json + { "fullName": "An", "email": "an@test.local", ... }
    // -> CreateStudentRequest. Data annotations chạy trước action; lỗi thì response 400.
    // [LAB2-06] Roles lấy từ JWT đã được server xác thực. Token Student hợp lệ vẫn bị 403.
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
    {
        var created = await _studentService.CreateStudentAsync(request);
        // [LAB2-04] Trả 201 + Location đến GET chi tiết; không phải redirect trình duyệt.
        return CreatedAtRouteResponse("GetStudentById", new { id = created.StudentId }, created, "Student created successfully.");
    }

    /// <summary>Updates a student. 404 if it does not exist.</summary>
    // [LAB2-02] Một request có thể bind nhiều nguồn: id từ URL, request từ JSON body.
    // Ví dụ PUT /api/v1/students/5: id=5; thông tin muốn cập nhật nằm trong UpdateStudentRequest.
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateStudent([FromRoute] int id, [FromBody] UpdateStudentRequest request)
        => Success(await _studentService.UpdateStudentAsync(id, request), "Student updated successfully.");

    /// <summary>Deletes a student (and its enrollments). Admin-only endpoint. 404 if it does not exist.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteStudent([FromRoute] int id)
    {
        await _studentService.DeleteStudentAsync(id);
        return Success<object?>(null, $"Student with id {id} was deleted successfully.");
    }
}
