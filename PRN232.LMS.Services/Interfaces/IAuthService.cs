using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;

namespace PRN232.LMS.Services.Interfaces;

/// <summary>Authentication/authorization business logic (Lab 2).</summary>
public interface IAuthService
{
    /// <summary>Validates credentials and issues an access token + refresh token.</summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Exchanges a valid refresh token for a new token pair (rotation).
    /// Reusing an already-revoked token is treated as theft: every session of that user is revoked.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Lists the API users (admin-only endpoint).</summary>
    Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken ct = default);
}
