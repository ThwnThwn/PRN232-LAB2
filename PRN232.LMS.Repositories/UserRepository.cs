using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Entities;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>Data access for <see cref="User"/> (Lab 2).</summary>
public class UserRepository : GenericRepository<User>, IRepository<User>
{
    public UserRepository(AppDbContext db) : base(db)
    {
    }
}
