using PRN232.LMS.Repositories.Entities;

namespace PRN232.LMS.Services.Security;

/// <summary>Creates signed JWT access tokens and random refresh tokens.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Creates a signed access token for the user and returns it with its lifetime in seconds.</summary>
    (string Token, int ExpiresInSeconds) GenerateAccessToken(User user);

    /// <summary>Creates a cryptographically random refresh token (raw value; only its hash is stored).</summary>
    string GenerateRefreshToken();

    /// <summary>Computes the SHA-256 hash used to store/lookup a refresh token.</summary>
    string HashRefreshToken(string refreshToken);
}
