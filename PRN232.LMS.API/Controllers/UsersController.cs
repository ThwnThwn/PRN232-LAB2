using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.API.Controllers;

[ApiVersion(1.0)]
[ApiVersion(2.0)]
// [LAB2-06] Authorization: controller này chỉ cho danh tính có role "Admin" truy cập.
// Không có JWT/ JWT sai -> 401; JWT Student hợp lệ -> 403; JWT Admin hợp lệ -> vào action.
// Framework kiểm tra trước action. Đưa "role":"Admin" vào body không tạo quyền Admin.
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/users")]
public class UsersController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public UsersController(IAuthService authService) => _authService = authService;

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(CancellationToken ct)
        // Service chỉ trả UserResponse (id/username/role), không gửi User entity chứa PasswordHash.
        => Success(await _authService.GetUsersAsync(ct));
}
