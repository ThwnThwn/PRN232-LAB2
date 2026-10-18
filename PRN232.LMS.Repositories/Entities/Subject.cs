using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRN232.LMS.Repositories.Entities;

/// <summary>
/// Subject entity (maps to the "Subject" table).
/// Entity Model — used only for database mapping inside the data-access layer.
/// </summary>
[Table("Subject")]
public class Subject
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SubjectId { get; set; }

    [Required]
    [StringLength(20)]
    [Column(TypeName = "varchar(20)")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SubjectName { get; set; } = string.Empty;

    [Required]
    public int Credit { get; set; }
}
