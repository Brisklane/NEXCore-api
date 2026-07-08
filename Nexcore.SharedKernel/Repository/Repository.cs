using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nexcore.SharedKernel.Repository;

/// <summary>
/// EF Core implementation of <see cref="IRepository{T}"/>. Reads are <c>AsNoTracking</c> and
/// transparently exclude soft-deleted rows; deletes default to a soft delete. Members are
/// <c>virtual</c> so a module can subclass to add entity-specific behaviour.
/// </summary>
/// <typeparam name="T">Aggregate root, rooted at <see cref="BaseEntity"/>.</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().Where(e => !e.IsDeleted).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AsNoTracking()
            .Where(e => !e.IsDeleted)
            .FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<(IEnumerable<T> Items, int Total)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        var query = DbSet.AsNoTracking().Where(e => !e.IsDeleted);

        if (predicate != null)
            query = query.Where(predicate);

        var total = await query.CountAsync();

        // Apply eager-loads only to the page rows — the count above stays a cheap join-free query.
        if (include != null)
            query = include(query);

        IQueryable<T> orderedQuery = orderBy != null ? orderBy(query) : query;
        var items = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public virtual async Task AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await DbSet.AddRangeAsync(entities);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        DbSet.UpdateRange(entities);
    }

    public virtual void Delete(T entity)
    {
        DbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        DbSet.RemoveRange(entities);
    }

    public virtual void SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        DbSet.Update(entity);
    }

    public virtual async Task<bool> ExistsAsync(Guid id)
    {
        return await DbSet.AnyAsync(e => e.Id == id && !e.IsDeleted);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var query = DbSet.AsNoTracking().Where(e => !e.IsDeleted);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    public virtual async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
