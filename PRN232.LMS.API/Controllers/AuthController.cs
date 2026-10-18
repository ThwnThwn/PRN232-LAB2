using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;
using PRN232.LMS.Services.Options;

namespace PRN232.LMS.API.Controllers;

// [LAB2-06/08] Đăng nhập/refresh được dùng ở v1 và v2, có thêm alias /api/auth theo handout.
// Ví dụ POST /api/v1/auth/login và POST /api/auth/login đều gọi Login.
[ApiVersion(1.0)]
[ApiVersion(2.0)]
[Route("api/v{version:apiVersion}/auth")]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
    }

    // [LAB2-06] Người dùng chưa có JWT vẫn phải đăng nhập được, nên action là AllowAnonymous.
    // [LAB2-02] Content-Type: application/json chọn input formatter đọc body thành LoginRequest.
    // Controller không so mật khẩu: AuthService kiểm tra rồi cấp token; Success bọc response.
    // Request ví dụ: { "username": "admin", "password": "Admin@123" }.
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => Success(TransportRefreshToken(await _authService.LoginAsync(request, ct)), "Login successful");

    // [LAB2-07] Access token có thể hết hạn khi client muốn refresh, nên action không bắt JWT.
    // AllowAnonymous không có nghĩa ai cũng đổi được token: service phải kiểm tra refresh token.
    // [LAB2-02] EmptyBodyBehavior.Allow hỗ trợ trình duyệt gửi token bằng cookie và body trống.
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? request,
        CancellationToken ct)
    {
        var token = request?.RefreshToken;
        // Mặc định API/mobile gửi { "refreshToken": "..." }. Cookie mode bật thì có thể lấy cookie.
        if (string.IsNullOrWhiteSpace(token) && _jwtOptions.UseRefreshTokenCookie)
        {
            token = Request.Cookies[_jwtOptions.RefreshTokenCookieName];
        }

        return Success(TransportRefreshToken(await _authService.RefreshTokenAsync(token ?? string.Empty, ct)),
            "Token refreshed successfully");
    }

    private AuthResponse TransportRefreshToken(AuthResponse response)
    {
        // [LAB2-07] no-store yêu cầu cache không lưu response chứa token.
        // Mặc định trả refresh token trong data; cookie mode chuyển token sang cookie HttpOnly.
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        if (_jwtOptions.UseRefreshTokenCookie)
        {
            Response.Cookies.Append(_jwtOptions.RefreshTokenCookieName, response.RefreshToken!, new CookieOptions
            {
                // HttpOnly: JavaScript không đọc cookie qua document.cookie; browser vẫn gửi nó.
                // Secure: chỉ gửi qua HTTPS nếu bật; SameSite.Strict hạn chế gửi từ site khác.
                // Path=/api giới hạn đường dẫn browser gửi cookie; không phải giới hạn quyền JWT.
                HttpOnly = true,
                Secure = _jwtOptions.CookieSecure,
                SameSite = SameSiteMode.Strict,
                Path = "/api",
                MaxAge = TimeSpan.FromDays(_jwtOptions.RefreshTokenDays)
            });
            response.RefreshToken = null;
            // Tránh trả lại bản raw trong body khi đã chọn vận chuyển qua cookie.
        }

        return response;
    }
}
