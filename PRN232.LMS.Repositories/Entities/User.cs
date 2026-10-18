using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// User entity (Lab 2) — maps to the "User" table used for JWT authentication/authorization.
/// Entity Model — data-access layer only. Never returned to clients (PasswordHash is secret).
/// </summary>
[Table("User")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required]
    [StringLength(50)]
    [Column(TypeName = "varchar(50)")]
    public string Username { get; set; } = string.Empty;

    /// <summary>BCrypt hash — the plain password is never stored.</summary>
    [Required]
    [StringLength(255)]
    [Column(TypeName = "varchar(255)")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string Role { get; set; } = "Student";

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
