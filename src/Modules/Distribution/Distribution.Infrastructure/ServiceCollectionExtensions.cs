using Distribution.Application.Services.Interfaces;
using Distribution.Infrastructure.Events;
using Distribution.Infrastructure.Persistence;
using Distribution.Infrastructure.Repositories;
using Distribution.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Persistence;

namespace Distribution.Infrastructure;

/// <summary>Composition root for the Distribution module.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDistributionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<DistributionDbContext>(options =>
            options.UseNexcorePostgres(connectionString, typeof(DistributionDbContext).Assembly));

        // ── Tenant scope ──────────────────────────────────────────────────────
        // Scoped: it caches the claims for the life of one request.
        services.AddScoped<IDistributionTenant, HttpDistributionTenant>();

        // ── Repositories ──────────────────────────────────────────────────────
        // One open generic covers the reference and master data — vehicles, drivers, geography,
        // stock norms. The surfaces with real behaviour use the services below instead.
        services.AddScoped(typeof(IDistributionRepository<>), typeof(DistributionRepository<>));

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<DistributionNumbering>();

        services.AddScoped<INetworkService, NetworkService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IFieldService, FieldService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<ISchemeService, SchemeService>();
        services.AddScoped<ICreditService, CreditService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IVanService, VanService>();
        services.AddScoped<IFulfilmentService, FulfilmentService>();
        services.AddScoped<ILogisticsService, LogisticsService>();
        services.AddScoped<IReturnService, ReturnService>();
        services.AddScoped<IClaimService, ClaimService>();
        services.AddScoped<ISettlementService, SettlementService>();
        services.AddScoped<ISecondarySalesService, SecondarySalesService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<ITraceabilityService, TraceabilityService>();
        services.AddScoped<IDistributionReportService, DistributionReportService>();
        services.AddScoped<IDistributionAdminService, DistributionAdminService>();

        // ── Event handlers ────────────────────────────────────────────────────
        services.AddScoped<DistributionInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, DistributionCompanyCreatedEventHandler>();

        return services;
    }
}
