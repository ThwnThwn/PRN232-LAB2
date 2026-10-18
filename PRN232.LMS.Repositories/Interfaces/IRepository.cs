namespace PRN232.LMS.Repositories.Interfaces;

/// <summary>
/// Generic data-access contract for an entity. Implementations live in the Repository layer.
/// No business logic is allowed here — only raw CRUD / query access.
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>Read-only, no-tracking queryable used by the Service layer to build queries.</summary>
    IQueryable<TEntity> Query();

    /// <summary>Tracking queryable (for loading an entity that will be modified).</summary>
    IQueryable<TEntity> QueryTracking();

    /// <summary>Finds one entity by its integer primary key.</summary>
    Task<TEntity?> GetByIdAsync(int id, bool tracking = false, CancellationToken ct = default);

    /// <summary>Checks whether an entity with the given id exists.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>Adds a new entity and saves the change.</summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Updates (or attaches) an entity and saves the change.</summary>
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Removes an entity and saves the change.</summary>
    Task DeleteAsync(TEntity entity, CancellationToken ct = default);
}
