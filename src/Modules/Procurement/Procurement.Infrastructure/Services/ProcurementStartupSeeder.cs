using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Procurement.Application.Services.Interfaces;
using Procurement.Infrastructure.Persistence;

namespace Procurement.Infrastructure.Services;

/// <summary>
/// One-time backfill that runs on application startup (Development only). Seeds procurement
/// master data + demo transactions for any EXISTING company that has none yet — companies
/// created before procurement seeding was wired (or before the sample-data seeder existed)
/// never received it, so this populates them with zero manual action.
///
/// Gated to Development so demo data is never force-seeded into production. Idempotent
/// (InitializeAsync skips when master data exists; SeedAsync skips when vendors exist) and
/// non-blocking — any error is logged per-company and never prevents the app from starting.
/// </summary>
public class ProcurementStartupSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ProcurementStartupSeeder> _logger;

    public ProcurementStartupSeeder(
        IServiceScopeFactory scopeFactory,
        IHostEnvironment env,
        ILogger<ProcurementStartupSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _env          = env;
        _logger       = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
            return; // Never auto-backfill demo data outside Development.

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var ctx  = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();
            var init = scope.ServiceProvider.GetRequiredService<IProcurementInitializationService>();
            var seed = scope.ServiceProvider.GetRequiredService<ProcurementSeedDataService>();

            // This hosted service is registered before DatabaseMigrationService, so apply any
            // pending procurement migrations here first — otherwise seeding would run against a
            // stale schema. Idempotent: a no-op once migrations are already applied.
            await ctx.Database.MigrateAsync(cancellationToken);

            // Companies that exist but have no procurement vendors yet (single shared database).
            var companyIds = await ctx.Database
                .SqlQueryRaw<Guid>(
                    "SELECT c.Id AS Value FROM core.Companies c " +
                    "WHERE c.IsDeleted = 0 " +
                    "AND NOT EXISTS (SELECT 1 FROM procurement.Vendors v WHERE v.CompanyId = c.Id)")
                .ToListAsync(cancellationToken);

            if (companyIds.Count == 0)
            {
                _logger.LogInformation("Procurement startup backfill: all companies already have data.");
                return;
            }

            foreach (var companyId in companyIds)
            {
                try
                {
                    var branchId       = await ScalarAsync(ctx, "SELECT TOP 1 b.Id AS Value FROM core.Branches b WHERE b.CompanyId = {0} AND b.IsDeleted = 0", companyId, cancellationToken);
                    var userId         = await ScalarAsync(ctx, "SELECT TOP 1 b.CreatedByUserId AS Value FROM core.Branches b WHERE b.CompanyId = {0} AND b.IsDeleted = 0", companyId, cancellationToken);
                    var businessUnitId = await ScalarAsync(ctx, "SELECT TOP 1 bu.Id AS Value FROM core.BusinessUnits bu WHERE bu.CompanyId = {0} AND bu.IsDeleted = 0", companyId, cancellationToken);

                    // 1. Master data (idempotent — skips if already present).
                    await init.InitializeAsync(companyId);

                    // 2. Sample transactional data (idempotent — skips if vendors already exist).
                    var seeded = await seed.SeedAsync(companyId, branchId, businessUnitId, userId);

                    if (seeded)
                        _logger.LogInformation("Procurement startup backfill: seeded Company:{CompanyId}", companyId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Procurement startup backfill FAILED for Company:{CompanyId}", companyId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Procurement startup backfill encountered an error");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<Guid> ScalarAsync(ProcurementDbContext ctx, string sql, Guid companyId, CancellationToken ct)
    {
        var rows = await ctx.Database.SqlQueryRaw<Guid>(sql, companyId).ToListAsync(ct);
        return rows.FirstOrDefault();
    }
}
