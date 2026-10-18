using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// Enrollment entity (maps to the "Enrollment" table).
/// Entity Model — used only for database mapping inside the data-access layer.
/// </summary>
[Table("Enrollment")]
public class Enrollment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrollmentId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required]
    public int CourseId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EnrollDate { get; set; }

    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string Status { get; set; } = "Active";

    // Navigation: enrollment belongs to one student.
    public Student? Student { get; set; }

    // Navigation: enrollment belongs to one course.
    public Course? Course { get; set; }
}
