namespace PRN232.LMS.Services.Security;

/// <summary>
/// BCrypt implementation of <see cref="IPasswordHasher"/> (Lab 2 requirement: use BCrypt
/// or ASP.NET Core PasswordHasher).
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // [LAB2-06/07] BCrypt là hàm băm mật khẩu có salt và chi phí tính toán, không phải mã hóa
    // hai chiều. HashPassword tự tạo salt; cùng password có thể cho các chuỗi hash khác nhau.
    // Lưu hash trong User.PasswordHash; không lưu mật khẩu thô và không "giải mã" để đăng nhập.
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        try
        {
            // Verify sử dụng salt/thông số nằm trong hash để kiểm tra password người dùng gửi.
            // Không HashPassword(password) rồi so chuỗi, vì hash mới có salt ngẫu nhiên khác.
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Stored value is not a valid BCrypt hash.
            return false;
        }
    }
}
