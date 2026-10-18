using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// RefreshToken entity (Lab 2, optional table from the requirement) — enables the refresh-token flow.
/// Only the SHA-256 hash of the token is stored, never the raw token.
/// </summary>
[Table("RefreshToken")]
public class RefreshToken
{
    // [LAB2-07] Bản ghi trạng thái refresh token phía server:
    // TokenHash để tra cứu; ExpiresAt giới hạn tuổi thọ; RevokedAt khác null là đã thu hồi/đã dùng.
    // Token raw chỉ gửi cho client lúc phát hành, không có property để lưu ở entity này.
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RefreshTokenId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [StringLength(200)]
    [Column(TypeName = "varchar(200)")]
    public string TokenHash { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? RevokedAt { get; set; }

    public User? User { get; set; }

    // Các property tính toán để kiểm tra trong service, không phải credential client tự khai.
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsActive => RevokedAt is null && !IsExpired;
}
