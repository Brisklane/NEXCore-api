using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Persistence;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Infrastructure.Events;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Repositories;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Infrastructure;

/// <summary>Composition root for the Restaurant module.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRestaurantInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<RestaurantDbContext>(options =>
            options.UseNexcorePostgres(connectionString, typeof(RestaurantDbContext).Assembly));

        // ── Tenant scope ──────────────────────────────────────────────────────
        // Scoped: it caches the claims for the life of one request.
        services.AddScoped<IRestaurantTenant, HttpRestaurantTenant>();

        // ── Repositories ──────────────────────────────────────────────────────
        // One open generic covers every plain-CRUD entity in the module; the screens that need
        // real behaviour use the services below instead.
        services.AddScoped(typeof(IRestaurantRepository<>), typeof(RestaurantRepository<>));

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<RestaurantNumbering>();
        services.AddScoped<MenuService>();
        services.AddScoped<IMenuService>(sp => sp.GetRequiredService<MenuService>());

        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IRestaurantOrderService, RestaurantOrderService>();
        services.AddScoped<IKitchenService, KitchenService>();
        services.AddScoped<ICheckService, CheckService>();
        services.AddScoped<IRestaurantStaffService, RestaurantStaffService>();
        services.AddScoped<IRestaurantSessionService, RestaurantSessionService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IRecipeCostingService, RecipeCostingService>();
        services.AddScoped<IRestaurantReportService, RestaurantReportService>();
        services.AddScoped<ComplianceService>();

        // ── Event handlers ────────────────────────────────────────────────────
        services.AddScoped<RestaurantInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, RestaurantCompanyCreatedEventHandler>();

        return services;
    }
}
