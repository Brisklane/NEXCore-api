using Fitness.Application.Services.Interfaces;
using Fitness.Infrastructure.Events;
using Fitness.Infrastructure.Persistence;
using Fitness.Infrastructure.Repositories;
using Fitness.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Persistence;

namespace Fitness.Infrastructure;

/// <summary>Composition root for the Fitness module.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFitnessInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<FitnessDbContext>(options =>
            options.UseNexcorePostgres(connectionString, typeof(FitnessDbContext).Assembly));

        // ── Tenant scope ──────────────────────────────────────────────────────
        // Scoped: it caches the claims for the life of one request.
        services.AddScoped<IFitnessTenant, HttpFitnessTenant>();

        // ── Repositories ──────────────────────────────────────────────────────
        // One open generic covers every plain-CRUD entity in the module; the screens that need
        // real behaviour use the services below instead.
        services.AddScoped(typeof(IFitnessRepository<>), typeof(FitnessRepository<>));

        // ── Payments ──────────────────────────────────────────────────────────
        // The default provider records what a human took at the desk. A gateway integration
        // replaces this one registration and nothing else — the module never sees card data
        // either way.
        services.AddScoped<IPaymentProvider, ManualPaymentProvider>();

        // ── Services ──────────────────────────────────────────────────────────
        services.AddScoped<FitnessNumbering>();

        services.AddScoped<IClubService, ClubService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<ICatalogueService, CatalogueService>();
        services.AddScoped<IAgreementService, AgreementService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IDunningService, DunningService>();
        services.AddScoped<IAccessService, AccessService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ITrainingService, TrainingService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IRetentionService, RetentionService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IFacilityService, FacilityService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ICommerceService, CommerceService>();
        services.AddScoped<IFitnessReportService, FitnessReportService>();

        // ── Event handlers ────────────────────────────────────────────────────
        services.AddScoped<FitnessInitializationService>();
        services.AddScoped<IEventHandler<CompanyCreatedEvent>, FitnessCompanyCreatedEventHandler>();

        return services;
    }
}
