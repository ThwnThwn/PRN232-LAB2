using Microsoft.EntityFrameworkCore;
using PRN232.LMS.Repositories.Data;
using PRN232.LMS.Repositories.Interfaces;

namespace PRN232.LMS.Repositories;

/// <summary>
/// Generic EF Core based repository implementing <see cref="IRepository{TEntity}"/>.
/// </summary>
public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<TEntity> Set;

    public GenericRepository(AppDbContext db)
    {
        Db = db;
        Set = db.Set<TEntity>();
    }

    public IQueryable<TEntity> Query() => Set.AsNoTracking();

    public IQueryable<TEntity> QueryTracking() => Set;

    public async Task<TEntity?> GetByIdAsync(int id, bool tracking = false, CancellationToken ct = default)
    {
        var entity = await Set.FindAsync(new object[] { id }, ct);
        if (entity == null)
        {
            return null;
        }

        // Detach the returned entity unless the caller explicitly asked for a tracked one.
        if (!tracking)
        {
            Db.Entry(entity).State = EntityState.Detached;
        }

        return entity;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        var entity = await Set.FindAsync(new object[] { id }, ct);
        if (entity != null)
        {
            Db.Entry(entity).State = EntityState.Detached;
            return true;
        }

        return false;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await Set.AddAsync(entity, ct);
        await Db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        Set.Update(entity);
        await Db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        Set.Remove(entity);
        await Db.SaveChangesAsync(ct);
    }
}
