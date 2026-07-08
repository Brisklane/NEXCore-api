using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Helpers;

namespace Nexcore.SharedKernel.Repository;

/// <summary>
/// <see cref="Repository{T}"/> that pins every operation to the caller's tenant. It reads the
/// company/branch/business-unit from the JWT once per request (cached), then transparently adds
/// those three predicates to reads, stamps them onto inserts, and rejects deletes of rows that
/// belong to a different tenant — so callers can't accidentally cross tenant boundaries.
/// All three scope fields are mandatory.
/// </summary>
/// <typeparam name="T">Aggregate root, rooted at <see cref="BaseEntity"/>.</typeparam>
public abstract class TenantAwareRepository<T> : Repository<T> where T : BaseEntity
{
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private (Guid CompanyId, Guid BranchId, Guid BusinessUnitId)? _cachedTenantContext;

    protected TenantAwareRepository(DbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>Resolves the scope from the current request's claims, caching it for the rest of the request.</summary>
    protected (Guid CompanyId, Guid BranchId, Guid BusinessUnitId) GetTenantContext()
    {
        if (_cachedTenantContext.HasValue)
            return _cachedTenantContext.Value;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            throw new InvalidOperationException("HTTP context or user not available");

        var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(httpContext.User);

        if (businessUnitId is null)
            throw new InvalidOperationException("BusinessUnitId claim not found or invalid");

        _cachedTenantContext = (companyId, branchId, businessUnitId.Value);
        return _cachedTenantContext.Value;
    }

    public override async Task<T?> GetByIdAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await DbSet.AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.Id == id &&
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted);
    }

    public override async Task<IEnumerable<T>> GetAllAsync()
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await DbSet.AsNoTracking()
            .Where(e =>
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted)
            .ToListAsync();
    }

    public override async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await DbSet.AsNoTracking()
            .Where(e =>
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted)
            .Where(predicate)
            .ToListAsync();
    }

    public override async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await DbSet.AsNoTracking()
            .Where(e =>
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted)
            .FirstOrDefaultAsync(predicate);
    }

    public override async Task<(IEnumerable<T> Items, int Total)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet.AsNoTracking()
            .Where(e =>
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted);

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

    /// <summary>Stamps the tenant scope onto the new row before it is tracked.</summary>
    public override async Task AddAsync(T entity)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        entity.CompanyId = companyId;
        entity.BranchId = branchId;
        entity.BusinessUnitId = businessUnitId;

        await base.AddAsync(entity);
    }

    /// <summary>Stamps the tenant scope onto every new row in the batch.</summary>
    public override async Task AddRangeAsync(IEnumerable<T> entities)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        foreach (var entity in entities)
        {
            entity.CompanyId = companyId;
            entity.BranchId = branchId;
            entity.BusinessUnitId = businessUnitId;
        }

        await base.AddRangeAsync(entities);
    }

    public override async Task<bool> ExistsAsync(Guid id)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        return await DbSet.AnyAsync(e =>
            e.Id == id &&
            e.CompanyId == companyId &&
            e.BranchId == branchId &&
            e.BusinessUnitId == businessUnitId &&
            !e.IsDeleted);
    }

    public override async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        var query = DbSet.AsNoTracking()
            .Where(e =>
                e.CompanyId == companyId &&
                e.BranchId == branchId &&
                e.BusinessUnitId == businessUnitId &&
                !e.IsDeleted);

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync();
    }

    /// <summary>Refuses to delete a row that belongs to a different tenant, then soft-deletes it.</summary>
    public override void SoftDelete(T entity)
    {
        var (companyId, branchId, businessUnitId) = GetTenantContext();

        if (entity.CompanyId != companyId || entity.BranchId != branchId || entity.BusinessUnitId != businessUnitId)
            throw new UnauthorizedAccessException("Entity does not belong to current tenant");

        base.SoftDelete(entity);
    }
}
