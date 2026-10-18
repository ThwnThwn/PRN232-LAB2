using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="Semester"/>.</summary>
public class SemesterRepository : GenericRepository<Semester>, IRepository<Semester>
{
    public SemesterRepository(AppDbContext db) : base(db)
    {
    }
}
