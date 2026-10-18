using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Services.Options;

namespace PRN232.LMS.Services.Security;

/// <summary>JWT / refresh-token generator built on System.IdentityModel.Tokens.Jwt.</summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    // [LAB2-07] JWT access token có dạng header.payload.signature, ba phần phân cách bằng dấu chấm.
    // Header/payload dùng Base64Url nên ĐỌC ĐƯỢC; chữ ký bảo vệ tính toàn vẹn, không giấu dữ liệu.
    // Không đặt password/secret trong claims. Client gửi JWT qua Authorization: Bearer <token>.
    // Hàm dưới phát hành token; AddJwtBearer trong API/Program.cs là phía kiểm tra token.
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, int ExpiresInSeconds) GenerateAccessToken(User user)
    {
        // Với HS256, server phát hành và kiểm tra dùng chung secret đủ dài (ít nhất 32 byte).
        // Secret lấy từ configuration/environment, không phải chuỗi token trả về cho client.
        if (string.IsNullOrWhiteSpace(_options.Key) || Encoding.UTF8.GetByteCount(_options.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters.");
        }

        var now = DateTime.UtcNow;
        var expires = now.AddSeconds(_options.ExpirySeconds <= 0 ? 3600 : _options.ExpirySeconds);

        // Claims là các phát biểu server ký về người dùng/token:
        // sub: UserId; unique_name/Name: username; Role: Admin hoặc Student; jti: ID token duy nhất.
        // [Authorize(Roles = "Admin")] đối chiếu Role sau khi Bearer handler kiểm tra JWT.
        // Dữ liệu lấy từ User trong DB; không lấy role do client điền trong request login.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // SigningCredentials chọn HMAC-SHA256. Sửa payload (ví dụ Student thành Admin)
        // mà không có secret để ký lại -> chữ ký sai -> Bearer handler từ chối với 401.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // issuer -> iss, audience -> aud, notBefore -> nbf, expires -> exp trong JWT payload.
        // Thời gian dùng UTC. Lifetime mặc định 3600 giây; handler có ClockSkew 30 giây.
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        // WriteToken serialize + ký ra chuỗi JWT dùng được. Không lưu access token vào DB ở bài này.
        // Mỗi request kiểm tra chữ ký/hạn/issuer/audience; thu hồi refresh token không tự hủy JWT này.
        return (new JwtSecurityTokenHandler().WriteToken(token), (int)(expires - now).TotalSeconds);
    }

    public string GenerateRefreshToken()
    {
        // Refresh token là 64 byte ngẫu nhiên từ bộ sinh dùng cho mật mã, đổi sang Base64 để truyền.
        // Khác JWT: không có claims/chữ ký có thể tự kiểm tra; server phải tra hash trong database.
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    // Refresh token có độ ngẫu nhiên cao nên dùng SHA-256 để tra cứu. Đây không phải hàm băm password;
    // mật khẩu người dùng ít entropy hơn và được BCrypt xử lý trong BcryptPasswordHasher.
    public string HashRefreshToken(string refreshToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
