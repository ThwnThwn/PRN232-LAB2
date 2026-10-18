using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;
using PRN232.LMS.Services.Security;

namespace PRN232.LMS.Services.Data;

/// <summary>
/// Seeds the authentication accounts (Lab 2). Passwords are hashed with BCrypt,
/// Required demo accounts: admin / Admin@123 and student / Student@123.
/// </summary>
public static class UserSeeder
{
    public static async Task EnsureSeededAsync(
        IRepository<User> users,
        IPasswordHasher passwordHasher,
        CancellationToken ct = default)
    {
        // [LAB2-06/07] Hai tài khoản demo đúng yêu cầu chấm: Admin và Student.
        // Password thô dưới đây chỉ là dữ liệu seed demo, được Hash trước khi lưu PasswordHash.
        // Khi đăng nhập, role từ bản ghi User được đưa vào JWT để authorization sử dụng.
        // Tìm theo username giúp chạy lại startup không thêm trùng; seed bảo đảm lại role/password demo.
        foreach (var (username, password, role) in new[]
        {
            ("admin", "Admin@123", "Admin"),
            ("student", "Student@123", "Student")
        })
        {
            var existing = await users.QueryTracking()
                .SingleOrDefaultAsync(u => u.Username == username, ct);
            if (existing is null)
            {
                await users.AddAsync(new User
                {
                    Username = username,
                    PasswordHash = passwordHasher.Hash(password),
                    Role = role
                }, ct);
            }
            else if (existing.Role != role || !passwordHasher.Verify(password, existing.PasswordHash))
            {
                existing.Role = role;
                existing.PasswordHash = passwordHasher.Hash(password);
                await users.UpdateAsync(existing, ct);
            }
        }
    }
}
