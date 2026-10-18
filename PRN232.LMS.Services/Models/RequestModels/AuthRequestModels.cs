using System.ComponentModel.DataAnnotations;

namespace PRN232.LMS.Services.Models.RequestModels;

/// <summary>Payload for POST /api/auth/login.</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username must not exceed 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, ErrorMessage = "Password must not exceed 100 characters.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Payload for POST /api/auth/refresh-token.
/// The property is optional: when cookie mode is enabled the refresh token can be sent
/// by the browser through the HttpOnly cookie instead of the request body.
/// </summary>
public class RefreshTokenRequest
{
    [StringLength(500, ErrorMessage = "RefreshToken must not exceed 500 characters.")]
    public string? RefreshToken { get; set; }
}
