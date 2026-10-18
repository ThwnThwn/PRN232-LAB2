namespace PRN232.LMS.Services.Models.ResponseModels;

/// <summary>
/// Response model for authentication APIs:
/// { "success": true, "data": { "accessToken", "refreshToken", "expiresIn", "tokenType" } }
/// In cookie mode (Jwt:UseRefreshTokenCookie = true) RefreshToken is null because the
/// refresh token travels in an HttpOnly cookie instead of the JSON body.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh token (null when the HttpOnly-cookie transport is enabled).</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Access-token lifetime in seconds (e.g. 3600).</summary>
    public int ExpiresIn { get; set; }

    public string TokenType { get; set; } = "Bearer";
}

/// <summary>User information exposed to clients (never exposes PasswordHash).</summary>
public class UserResponse
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
