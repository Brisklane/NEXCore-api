using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Helpers;

namespace Distribution.Infrastructure.Persistence;

/// <summary>
/// The caller's tenant scope, resolved once per request from the JWT.
///
/// Distribution's read paths are join-heavy — a beat list is routes joined to outlets joined to
/// visits joined to credit profiles; a settlement is a day joined to orders, collections, returns
/// and van movements — so the services query the DbContext directly rather than going through one
/// repository per entity. That makes tenant filtering something a developer could forget, so it is
/// centralised here and applied through <see cref="DistributionQueryExtensions.ForTenant{T}"/> on
/// every single query.
/// </summary>
public interface IDistributionTenant
{
    Guid CompanyId { get; }
    Guid BranchId { get; }
    Guid BusinessUnitId { get; }

    /// <summary>Signed-in ERP user, used for audit stamps. <see cref="Guid.Empty"/> when unauthenticated.</summary>
    Guid UserId { get; }
}

/// <inheritdoc />
public class HttpDistributionTenant : IDistributionTenant
{
    private readonly Lazy<(Guid Company, Guid Branch, Guid BusinessUnit, Guid User)> _scope;

    public HttpDistributionTenant(IHttpContextAccessor accessor)
    {
        _scope = new Lazy<(Guid, Guid, Guid, Guid)>(() =>
        {
            var user = accessor.HttpContext?.User
                ?? throw new InvalidOperationException("No authenticated request context.");

            var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(user);
            if (businessUnitId is null)
                throw new InvalidOperationException("BusinessUnitId claim not found or invalid");

            Guid userId;
            try { userId = TenantContextHelper.ExtractUserId(user); }
            catch (InvalidOperationException) { userId = Guid.Empty; }

            return (companyId, branchId, businessUnitId.Value, userId);
        });
    }

    public Guid CompanyId => _scope.Value.Company;
    public Guid BranchId => _scope.Value.Branch;
    public Guid BusinessUnitId => _scope.Value.BusinessUnit;
    public Guid UserId => _scope.Value.User;
}

/// <summary>Fixed scope, used by the seeder — which runs outside any HTTP request.</summary>
public sealed class FixedDistributionTenant(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    : IDistributionTenant
{
    public Guid CompanyId { get; } = companyId;
    public Guid BranchId { get; } = branchId;
    public Guid BusinessUnitId { get; } = businessUnitId;
    public Guid UserId { get; } = userId;
}

public static class DistributionQueryExtensions
{
    /// <summary>
    /// Narrows a query to the caller's company, branch and business unit, and drops soft-deleted
    /// rows. Every read in this module goes through it — no exceptions.
    /// </summary>
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, IDistributionTenant tenant)
        where T : BaseEntity
        => query.Where(e =>
            e.CompanyId == tenant.CompanyId &&
            e.BranchId == tenant.BranchId &&
            e.BusinessUnitId == tenant.BusinessUnitId &&
            !e.IsDeleted);

    /// <summary>
    /// Narrows to the company only, ignoring branch and business unit.
    ///
    /// Needed by the genuinely company-wide masters — the item-agnostic reason-code registry,
    /// trade schemes that run across every branch, and the geography tree. Scoping those to a
    /// branch would force a national scheme to be created once per depot.
    /// </summary>
    public static IQueryable<T> ForCompany<T>(this IQueryable<T> query, IDistributionTenant tenant)
        where T : BaseEntity
        => query.Where(e => e.CompanyId == tenant.CompanyId && !e.IsDeleted);

    /// <summary>Same as <see cref="ForTenant{T}"/>, but keeps soft-deleted rows for audit views.</summary>
    public static IQueryable<T> ForTenantIncludingDeleted<T>(this IQueryable<T> query, IDistributionTenant tenant)
        where T : BaseEntity
        => query.Where(e =>
            e.CompanyId == tenant.CompanyId &&
            e.BranchId == tenant.BranchId &&
            e.BusinessUnitId == tenant.BusinessUnitId);

    /// <summary>Stamps tenant scope and creation audit onto a new row.</summary>
    public static T StampNew<T>(this T entity, IDistributionTenant tenant, Guid? userId = null) where T : BaseEntity
    {
        entity.CompanyId = tenant.CompanyId;
        entity.BranchId = tenant.BranchId;
        entity.BusinessUnitId = tenant.BusinessUnitId;
        entity.CreatedAt = DateTime.UtcNow;
        entity.CreatedByUserId = userId ?? tenant.UserId;
        return entity;
    }

    /// <summary>Stamps the update audit. Never touches tenant scope — a row cannot change owner.</summary>
    public static T StampUpdated<T>(this T entity, Guid userId) where T : BaseEntity
    {
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        return entity;
    }

    /// <summary>Marks a row deleted rather than removing it, preserving history and references.</summary>
    public static T StampDeleted<T>(this T entity, Guid userId) where T : BaseEntity
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        return entity;
    }

    /// <summary>Applies a filter only when the value is present, keeping query builders flat.</summary>
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
