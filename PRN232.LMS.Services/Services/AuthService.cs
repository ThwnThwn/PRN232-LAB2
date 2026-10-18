using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services.Exceptions;
using PRN232.LMS.Services.Interfaces;
using PRN232.LMS.Services.Models.RequestModels;
using PRN232.LMS.Services.Models.ResponseModels;
using PRN232.LMS.Services.Options;
using PRN232.LMS.Services.Security;

namespace PRN232.LMS.Services.Services;

/// <summary>
/// JWT authentication business logic: login, refresh-token rotation (with reuse detection)
/// and user listing. Passwords are compared against BCrypt hashes; refresh tokens are only
/// stored as SHA-256 hashes.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IRepository<User> _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IRepository<User> users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // [LAB2-06] LUỒNG AUTHENTICATION KHI LOGIN:
        // 1. Nhận username/password đã được bind và kiểm tra data annotations ở API.
        // 2. Tìm User qua repository. 3. BCrypt.Verify(password gửi lên, hash trong DB).
        // 4. Đúng -> IssueTokensAsync; sai/không có User -> UnauthorizedException -> HTTP 401.
        // Cùng một thông điệp cho username lạ và mật khẩu sai để không tiết lộ tài khoản tồn tại.
        var username = request.Username.Trim();
        var user = await _users.Query().FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // [LAB2-07] Refresh token ở bài này là chuỗi ngẫu nhiên, KHÔNG phải JWT.
        // Server giữ hash + hạn dùng + trạng thái thu hồi trong DB; client giữ bản raw.
        // Flow: nhận raw -> hash -> tìm bản ghi -> kiểm tra thu hồi/hạn -> consume -> cấp cặp mới.
        // Đây là kiểm tra riêng, không dùng access token cũ làm bằng chứng để refresh.
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("A refresh token is required.");
        }

        // Hash cùng thuật toán lúc phát hành để so sánh; không cần/không thể giải mã hash.
        var hash = _tokenGenerator.HashRefreshToken(refreshToken.Trim());
        var stored = await _refreshTokens.Query().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (stored is null)
        {
            throw new UnauthorizedException("Refresh token is invalid.");
        }

        // ---- Reuse detection -------------------------------------------------
        // Rotation revokes a refresh token as soon as it is used. If a revoked token shows up
        // again, the token was probably stolen and replayed -> revoke EVERY active session
        // of that user and force a new login.
        if (stored.RevokedAt is not null)
        {
            // Token cũ bị dùng lại có thể là retry hoặc bị đánh cắp; chính sách của bài thu hồi
            // các refresh token chưa thu hồi của user và yêu cầu đăng nhập lại.
            // Việc này KHÔNG vô hiệu ngay JWT access token đã cấp: chúng còn dùng đến hết hạn.
            var revoked = await _refreshTokens.RevokeAllActiveForUserAsync(stored.UserId, DateTime.UtcNow, ct);

            _logger.LogWarning(
                "Refresh-token reuse detected for user {UserId} (token {RefreshTokenId} was revoked at {RevokedAt}). " +
                "Revoked {RevokedCount} additional active token(s) of this user.",
                stored.UserId, stored.RefreshTokenId, stored.RevokedAt, revoked);

            throw new UnauthorizedException(
                "This refresh token has already been used. For security reasons every session of this account was revoked — please sign in again.");
        }

        if (stored.IsExpired)
        {
            throw new UnauthorizedException("Refresh token is expired.");
        }

        // ---- Rotation --------------------------------------------------------
        var user = await _users.GetByIdAsync(stored.UserId, ct: ct)
            ?? throw new UnauthorizedException("The user of this refresh token no longer exists.");

        if (await _refreshTokens.TryConsumeAsync(stored.RefreshTokenId, DateTime.UtcNow, ct) != 1)
        {
            throw new UnauthorizedException("Refresh token has already been used or has expired.");
        }

        // Refresh token đã dùng được đánh dấu, lần tiếp theo không được tái sử dụng.
        // Cặp token mới lấy role từ User trong DB, không nhận role do client tự khai.
        return await IssueTokensAsync(user, ct);
    }

    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.Query()
            .OrderBy(u => u.UserId)
            .ToListAsync(ct);

        return users.Select(u => new UserResponse
        {
            UserId = u.UserId,
            Username = u.Username,
            Role = u.Role
        }).ToList();
    }

    // ---------------------------------------------------------------

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        // [LAB2-07] Hai token có mục đích khác nhau:
        // accessToken: JWT ký, mặc định 3600 giây, gửi Bearer cho resource endpoints.
        // refreshToken: chuỗi ngẫu nhiên, mặc định 7 ngày, chỉ dùng để xin cặp token mới.
        var (accessToken, expiresIn) = _tokenGenerator.GenerateAccessToken(user);
        var refreshToken = _tokenGenerator.GenerateRefreshToken();

        var days = _jwtOptions.RefreshTokenDays <= 0 ? 7 : _jwtOptions.RefreshTokenDays;

        // Chỉ lưu TokenHash. Nếu database bị đọc, bản hash không dùng trực tiếp để refresh được.
        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = _tokenGenerator.HashRefreshToken(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        }, ct);

        // Client cần bản raw refreshToken để sử dụng; chỉ trả ở response (hoặc cookie tại API).
        // expiresIn tính bằng GIÂY của access token, không phải thời gian sống refresh token.
        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            TokenType = "Bearer"
        };
    }
}
