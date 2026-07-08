using System.Linq.Expressions;

namespace Nexcore.SharedKernel.Repository;

/// <summary>
/// Persistence contract shared by every aggregate. Read methods return materialized results;
/// the write methods (<see cref="Update"/>, <see cref="Delete"/>, …) only stage changes —
/// nothing is persisted until <see cref="SaveChangesAsync"/> runs, so a unit of work can batch
/// several operations into one transaction.
/// </summary>
/// <typeparam name="T">Aggregate root, rooted at <see cref="BaseEntity"/>.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Returns a single page plus the unpaged total, with optional filter, ordering and eager-load shaping.
    /// </summary>
    Task<(IEnumerable<T> Items, int Total)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null);

    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);

    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>Physically removes the row. Prefer <see cref="SoftDelete"/> for auditable records.</summary>
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>Flags the row as deleted (sets <c>IsDeleted</c>) instead of removing it.</summary>
    void SoftDelete(T entity);

    Task<bool> ExistsAsync(Guid id);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>Commits all staged changes as one unit of work.</summary>
    Task SaveChangesAsync();
}
