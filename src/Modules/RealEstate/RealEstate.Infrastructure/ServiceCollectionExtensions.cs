using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Persistence;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Infrastructure.Events;
using RealEstate.Infrastructure.Persistence;
using RealEstate.Infrastructure.Repositories;
using RealEstate.Infrastructure.Services;

namespace RealEstate.Infrastructure;

/// <summary>Composition root for the Real Estate module.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRealEstateInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<RealEstateDbContext>(options =>
            options.UseNexcorePostgres(connectionString, typeof(RealEstateDbContext).Assembly));

        // ── Tenant scope ──────────────────────────────────────────────────────
        // Scoped: it caches the claims for the life of one request.
        services.AddScoped<IRealEstateTenant, HttpRealEstateTenant>();

        // ── Repositories ──────────────────────────────────────────────────────
        // One open generic covers every plain-CRUD entity in the module. With four hundred
        // entities, writing an interface and an implementation for each would be eight hundred
        // files nobody would ever read; the screens that need real behaviour use the services.
        services.AddScoped(typeof(IRealEstateRepository<>), typeof(RealEstateRepository<>));

        // ── Shared helpers ────────────────────────────────────────────────────
        services.AddScoped<RealEstateNumbering>();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<IRealEstateAdminService, RealEstateAdminService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<ICrmService, CrmService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IMoneyService, MoneyService>();
        services.AddScoped<IExitService, ExitService>();
        services.AddScoped<IBrokerageService, BrokerageService>();
        services.AddScoped<ILeasingService, LeasingService>();
        services.AddScoped<ISocietyService, SocietyService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IConstructionService, ConstructionService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<IRealEstateReportService, RealEstateReportService>();
        services.AddScoped<ICommunicationService, CommunicationService>();

        // ── Event handlers ────────────────────────────────────────────────────
        services.AddScoped<RealEstateInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, RealEstateCompanyCreatedEventHandler>();

        return services;
    }
}
