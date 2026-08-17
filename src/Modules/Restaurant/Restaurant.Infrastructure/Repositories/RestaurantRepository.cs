using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Repository;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Repositories;

/// <summary>
/// Tenant-scoped CRUD for any Restaurant entity, registered open-generically.
///
/// The module has forty-odd tables, most of which are plain reference data — sections, printer
/// profiles, void reasons, guests. Hand-writing an interface and an implementation for each one
/// would be forty pairs of files that all say the same thing. The screens that need real
/// behaviour (orders, checks, kitchen, floor) get a proper service instead; everything else uses
/// this.
/// </summary>
public interface IRestaurantRepository<T> : IRepository<T> where T : BaseEntity;

/// <inheritdoc cref="IRestaurantRepository{T}"/>
public class RestaurantRepository<T> : TenantAwareRepository<T>, IRestaurantRepository<T> where T : BaseEntity
{
    public RestaurantRepository(RestaurantDbContext context, IHttpContextAccessor httpContextAccessor)
        : base(context, httpContextAccessor) { }
}
