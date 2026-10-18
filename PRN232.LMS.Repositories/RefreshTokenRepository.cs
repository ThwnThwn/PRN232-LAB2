using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="RefreshToken"/> (Lab 2).</summary>
public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext db) : base(db)
    {
    }

    // [LAB2-07] Một UPDATE có điều kiện (id đúng, chưa thu hồi, còn hạn) đánh dấu token đã dùng.
    // SQL thực thi nguyên tử: hai request cùng token không thể cùng cập nhật thành công bản ghi này.
    // Số hàng cập nhật = 1 -> service được cấp token mới; = 0 -> từ chối request đó.
    // Repository chỉ thực hiện thao tác dữ liệu; service quyết định khi nào consume/revoke.
    public Task<int> TryConsumeAsync(int refreshTokenId, DateTime now, CancellationToken ct = default)
        => Set.Where(t => t.RefreshTokenId == refreshTokenId && t.RevokedAt == null && t.ExpiresAt > now)
              .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, now), ct);

    // Chỉ đánh dấu các REFRESH token chưa bị thu hồi; không thay đổi JWT access token đã phát hành.
    public Task<int> RevokeAllActiveForUserAsync(int userId, DateTime revokedAt, CancellationToken ct = default)
        => Set.Where(t => t.UserId == userId && t.RevokedAt == null)
              .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAt, revokedAt), ct);

    public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken ct = default)
        => Set.Where(t => t.ExpiresAt < now)
              .ExecuteDeleteAsync(ct);
}
