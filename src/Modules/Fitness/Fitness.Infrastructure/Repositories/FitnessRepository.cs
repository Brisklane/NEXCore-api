using Fitness.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Repository;

namespace Fitness.Infrastructure.Repositories;

/// <summary>
/// Tenant-scoped CRUD for any Fitness entity, registered open-generically.
///
/// The module has a hundred and fifty tables, most of which are plain reference data — lead
/// sources, loss reasons, badges, loyalty tiers, printer-style settings. Hand-writing an
/// interface and an implementation for each would be a hundred and fifty pairs of files that all
/// say the same thing. The screens that need real behaviour — the door, the billing run, the
/// booking engine, the retention board — get a proper service instead; everything else uses this.
/// </summary>
public interface IFitnessRepository<T> : IRepository<T> where T : BaseEntity;

/// <inheritdoc cref="IFitnessRepository{T}"/>
public class FitnessRepository<T> : TenantAwareRepository<T>, IFitnessRepository<T> where T : BaseEntity
{
    public FitnessRepository(FitnessDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
}
