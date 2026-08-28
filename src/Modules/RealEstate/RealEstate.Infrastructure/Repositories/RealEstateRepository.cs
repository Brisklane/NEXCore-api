using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Repository;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Repositories;

/// <summary>
/// Tenant-scoped CRUD for any Real Estate entity, registered open-generically.
///
/// The module has four hundred-odd tables and most of them are reference or master data — reason
/// codes, amenities, tariffs, clause library items, portal mappings, document templates. Hand-
/// writing an interface and an implementation for each would be four hundred pairs of files that
/// all say the same thing. The surfaces with real behaviour — inventory, bookings, the payment
/// plan, receipts, transfers, possession, tenancies, the rent roll, construction certificates —
/// get a proper service instead; everything else uses this.
/// </summary>
public interface IRealEstateRepository<T> : IRepository<T> where T : BaseEntity;

/// <inheritdoc cref="IRealEstateRepository{T}"/>
public class RealEstateRepository<T> : TenantAwareRepository<T>, IRealEstateRepository<T> where T : BaseEntity
{
    public RealEstateRepository(RealEstateDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
}
