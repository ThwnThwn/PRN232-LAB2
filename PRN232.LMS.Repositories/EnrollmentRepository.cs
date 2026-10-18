using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="Enrollment"/>.</summary>
public class EnrollmentRepository : GenericRepository<Enrollment>, IRepository<Enrollment>
{
    public EnrollmentRepository(AppDbContext db) : base(db)
    {
    }
}
