using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="Student"/>.</summary>
public class StudentRepository : GenericRepository<Student>, IRepository<Student>
{
    public StudentRepository(AppDbContext db) : base(db)
    {
    }
}
