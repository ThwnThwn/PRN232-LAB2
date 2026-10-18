using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="Course"/>.</summary>
public class CourseRepository : GenericRepository<Course>, IRepository<Course>
{
    public CourseRepository(AppDbContext db) : base(db)
    {
    }
}
