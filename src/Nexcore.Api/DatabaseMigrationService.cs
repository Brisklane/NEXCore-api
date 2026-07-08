using Auth.Infrastructure.Persistence;
using Accounting.Infrastructure.Persistence;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Crm.Application.Services.Interfaces;
using Crm.Infrastructure.Persistence;
using Hr.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using Manufacturing.Infrastructure.Persistence;
using Procurement.Infrastructure.Persistence;
using Sales.Infrastructure.Persistence;

namespace Nexcore.Api;

public class DatabaseMigrationService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        try
        {
            var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var accounting = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
            var hr = scope.ServiceProvider.GetRequiredService<HrDbContext>();
            var inventory = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var manufacturing = scope.ServiceProvider.GetRequiredService<ManufacturingDbContext>();
            var crm = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var sales = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
            var procurement = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

            // Migrations run automatically on localhost only.
            // Guarded by RUN_MIGRATIONS=true so Azure App Service never auto-migrates
            // even if ASPNETCORE_ENVIRONMENT is accidentally set to Development.
            var runMigrations = Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true";
            if (runMigrations)
            {
                logger.LogInformation("Starting database migrations (localhost)...");

                await core.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Core migrations applied.");

                await auth.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Auth migrations applied.");

                await accounting.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Accounting migrations applied.");

                await hr.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("HR migrations applied.");

                await inventory.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Inventory migrations applied.");

                await manufacturing.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Manufacturing migrations applied.");

                await crm.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("CRM migrations applied.");

                await sales.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Sales migrations applied.");

                await procurement.Database.MigrateAsync(stoppingToken);
                logger.LogInformation("Procurement migrations applied.");

                logger.LogInformation("All migrations completed.");
            }

            // Seeding runs in all environments — operations are idempotent.
            logger.LogInformation("Seeding geographic reference data...");
            await GeoReferenceSeed.SeedAsync(core);

            logger.LogInformation("Ensuring Walk-in Customer exists for all companies...");
            var crmInit = scope.ServiceProvider.GetRequiredService<ICrmInitializationService>();
            await crmInit.EnsureWalkInCustomersAsync();

            logger.LogInformation("Startup data tasks completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup data task failed.");
            throw;
        }
    }
}
