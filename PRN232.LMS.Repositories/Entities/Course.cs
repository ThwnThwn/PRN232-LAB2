using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// Course entity (maps to the "Course" table).
/// Entity Model — used only for database mapping inside the data-access layer.
/// </summary>
[Table("Course")]
public class Course
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int CourseId { get; set; }

    [Required]
    [StringLength(100)]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    public int SemesterId { get; set; }

    // Navigation: course belongs to one semester.
    public Semester? Semester { get; set; }

    // Navigation (one-to-many): a course can contain many enrollments.
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
