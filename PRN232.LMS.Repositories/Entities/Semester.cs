using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// Semester entity (maps to the "Semester" table).
/// Entity Model — used only for database mapping inside the data-access layer.
/// </summary>
[Table("Semester")]
public class Semester
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SemesterId { get; set; }

    [Required]
    [StringLength(100)]
    public string SemesterName { get; set; } = string.Empty;

    [Column(TypeName = "datetime")]
    public DateTime StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime EndDate { get; set; }

    // Navigation (one-to-many): a semester can contain many courses.
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
