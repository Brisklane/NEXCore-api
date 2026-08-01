using Core.Application.Services.Interfaces;
using Core.Infrastructure.Events;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Events;

namespace Core.Infrastructure;

/// <summary>
/// Composition root for the Core module: registers the tenant-aware <see cref="CoreDbContext"/>,
/// the request-scoped tenant context, the domain services/validators, and the cross-module event
/// handlers (context-switch validation, city-code and company-logo lookups).
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ITenantContext — reads TenantId JWT claim per-request
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();

        // Database — ITenantContext is injected into CoreDbContext for global query filter
        services.AddDbContext<CoreDbContext>((sp, options) =>
        {
            options.UseNexcorePostgres(configuration.GetConnectionString("DefaultConnection"), typeof(CoreDbContext).Assembly);
        });

        // NOTE: IEventPublisher is registered in Program.cs (centralized, decoupled)

        // Services
        services.AddScoped<TenantService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IBusinessUnitService, BusinessUnitService>();
        services.AddScoped<ICompanyValidator, CompanyValidator>();
        services.AddMemoryCache();
        services.AddScoped<IGeoReferenceService, GeoReferenceService>();

        // Synchronous city code lookup (TCS request/response pattern)
        services.AddScoped<IEventHandler<CityCodeLookupEvent>, CityCodeLookupHandler>();
        services.AddScoped<IEventHandler<CompanyLogoLookupEvent>, CompanyLogoLookupHandler>();

        // Validates company/branch/business-unit context switches from the Auth module
        services.AddScoped<IEventHandler<ContextValidationRequestedEvent>, ContextValidationHandler>();

        return services;
    }
}
