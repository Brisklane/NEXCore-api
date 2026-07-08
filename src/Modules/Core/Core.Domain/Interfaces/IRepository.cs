namespace Core.Domain.Interfaces;

/// <summary>
/// Minimal persistence contract for Core entities. Writes stage changes; nothing is persisted until
/// <see cref="SaveChangesAsync"/> runs, so a caller can batch several operations into one commit.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}
