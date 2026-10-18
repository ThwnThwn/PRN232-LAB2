using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="Subject"/>.</summary>
public class SubjectRepository : GenericRepository<Subject>, IRepository<Subject>
{
    public SubjectRepository(AppDbContext db) : base(db)
    {
    }
}
