namespace PRN232.LMS.Services.Options;

/// <summary>
/// JWT / token configuration (Lab 2). Bound from the "Jwt" configuration section.
/// In Docker the values come from environment variables (.env -> docker-compose.yml),
/// e.g. Jwt__Key, Jwt__Issuer, Jwt__Audience, Jwt__ExpirySeconds, Jwt__UseRefreshTokenCookie.
/// </summary>
public class JwtOptions
{
    // [LAB2-07] Options pattern: Program.cs bind section Jwt, IOptions<JwtOptions> đưa vào service.
    // Key/Issuer/Audience phải thống nhất giữa bên phát hành (JwtTokenGenerator) và kiểm tra (API).
    // ExpirySeconds là tuổi thọ access token; RefreshTokenDays là tuổi thọ refresh token riêng.
    // Ví dụ biến môi trường Jwt__ExpirySeconds=3600 ghi đè Jwt:ExpirySeconds trong JSON config.
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing secret (must be at least 32 characters).</summary>
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = "PRN232.LMS";

    public string Audience { get; set; } = "PRN232.LMS.Client";

    /// <summary>Access-token lifetime in seconds (the API returns this as "expiresIn").</summary>
    public int ExpirySeconds { get; set; } = 3600;

    /// <summary>Refresh-token lifetime in days.</summary>
    public int RefreshTokenDays { get; set; } = 7;

    // ------------------------------------------------------------------
    // Cookie mode (for browser clients)
    // ------------------------------------------------------------------

    /// <summary>
    /// When true the refresh token is NOT returned in the JSON body but sent as an
    /// HttpOnly cookie, so client-side JavaScript cannot read it (XSS protection).
    /// API/mobile clients keep using the body (default: false).
    /// </summary>
    public bool UseRefreshTokenCookie { get; set; }

    public string RefreshTokenCookieName { get; set; } = "prn232_refresh_token";

    /// <summary>Set to true when the API is served over HTTPS (production). Keep false for local HTTP demos.</summary>
    public bool CookieSecure { get; set; }

    // ------------------------------------------------------------------
    // Maintenance
    // ------------------------------------------------------------------

    /// <summary>How often the background job removes expired refresh tokens (minutes). Default 60.</summary>
    public int CleanupIntervalMinutes { get; set; } = 60;
}
