using Distribution.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Repository;

namespace Distribution.Infrastructure.Repositories;

/// <summary>
/// Tenant-scoped CRUD for any Distribution entity, registered open-generically.
///
/// The module has a hundred-odd tables, and most of them are reference or master data — vehicles,
/// drivers, reason codes, survey forms, geography nodes, stock norms. Hand-writing an interface
/// and an implementation for each would be a hundred pairs of files that all say the same thing.
/// The surfaces with real behaviour — visits, orders, van stock, schemes, claims, settlement —
/// get a proper service instead; everything else uses this.
/// </summary>
public interface IDistributionRepository<T> : IRepository<T> where T : BaseEntity;

/// <inheritdoc cref="IDistributionRepository{T}"/>
public class DistributionRepository<T> : TenantAwareRepository<T>, IDistributionRepository<T> where T : BaseEntity
{
    public DistributionRepository(DistributionDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
}
