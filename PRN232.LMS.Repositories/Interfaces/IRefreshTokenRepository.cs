using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories.Interfaces;

/// <summary>
/// Data access for <see cref="RefreshToken"/> with the operations required by the
/// Lab 2 security hardening: bulk revoke (reuse detection) and expired-token cleanup.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Atomically consumes an unexpired token. Only one concurrent refresh can succeed.</summary>
    Task<int> TryConsumeAsync(int refreshTokenId, DateTime now, CancellationToken ct = default);

    /// <summary>Revokes every still-active refresh token of a user (used when a stolen token is replayed).</summary>
    Task<int> RevokeAllActiveForUserAsync(int userId, DateTime revokedAt, CancellationToken ct = default);

    /// <summary>Deletes refresh tokens whose expiry date has passed. Returns the number of deleted rows.</summary>
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default);
}
