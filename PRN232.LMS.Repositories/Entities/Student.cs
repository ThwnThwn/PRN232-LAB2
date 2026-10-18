using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// Student entity (maps to the "Student" table).
/// Entity Model — used only for database mapping inside the data-access layer.
/// </summary>
[Table("Student")]
public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StudentId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string Email { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// FPTU-style student code (Lab 2) — custom validation rule: SE19886, CE18793, ...
    /// </summary>
    [Required]
    [StringLength(10)]
    [Column(TypeName = "varchar(10)")]
    public string StudentCode { get; set; } = string.Empty;

    /// <summary>Phone number (Lab 2) — validated with the [Phone] attribute on request models.</summary>
    [StringLength(15)]
    [Column(TypeName = "varchar(15)")]
    public string? PhoneNumber { get; set; }

    // Navigation (one-to-many): a student can have many enrollments.
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
