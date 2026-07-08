using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Manufacturing.Infrastructure.Services;

/// <summary>
/// Seeds default Manufacturing master data (WorkCenters, OverheadRules) for a new company.
/// Called by ManufacturingCompanyCreatedEventHandler on company creation.
/// </summary>
public class ManufacturingInitializationService
{
    private readonly ManufacturingDbContext _ctx;
    private readonly ILogger<ManufacturingInitializationService> _logger;

    public ManufacturingInitializationService(
        ManufacturingDbContext ctx,
        ILogger<ManufacturingInitializationService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    /// <summary>Returns true if initialization ran, false if data already existed.</summary>
    public async Task<bool> InitializeForNewCompanyAsync(
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId)
    {
        if (await _ctx.WorkCenters.AnyAsync(w => w.CompanyId == companyId))
            return false;

        await using var tx = await _ctx.Database.BeginTransactionAsync();
        try
        {
            var T = (companyId, branchId, businessUnitId, userId);
            var now = DateTime.UtcNow;

            // ?? Default Work Centers ??????????????????????????????????????????
            var workCenters = new[]
            {
                WorkCenter(T, now, "WC-ASSEMBLY",  "Assembly Line",       8, 25),
                WorkCenter(T, now, "WC-MACHINING", "Machining Center",    6, 45),
                WorkCenter(T, now, "WC-PAINTING",  "Painting Station",    4, 20),
                WorkCenter(T, now, "WC-QC",        "Quality Control",     8, 30),
                WorkCenter(T, now, "WC-PACKAGING", "Packaging Line",      8, 15),
                WorkCenter(T, now, "WC-WELDING",   "Welding Station",     6, 50),
                WorkCenter(T, now, "WC-CUTTING",   "Cutting Station",     8, 35),
                WorkCenter(T, now, "WC-SUBCON",    "Sub-Contract",        0, 0),
                // Kitchen stations (fast-food / pizza shop production)
                WorkCenter(T, now, "WC-DOUGH",     "Dough & Prep Station", 40, 8),
                WorkCenter(T, now, "WC-OVEN",      "Pizza Oven",           30, 18),
                WorkCenter(T, now, "WC-GRILL",     "Burger Grill",         60, 12),
                WorkCenter(T, now, "WC-FRYER",     "Fryer Station",        80, 10),
            };
            _ctx.WorkCenters.AddRange(workCenters);
            await _ctx.SaveChangesAsync();

            // ?? Default Overhead Rules ????????????????????????????????????????
            var overheadRules = new[]
            {
                OverheadRule(T, now, "OH-FACTORY", "Factory Overhead",    null,                   "Percentage", 15,   "Labor",       workCenters),
                OverheadRule(T, now, "OH-MACH",    "Machine Overhead",    "WC-MACHINING",         "PerHour",    10,   "Machine",     workCenters),
                OverheadRule(T, now, "OH-ADMIN",   "Admin Overhead",      null,                   "Percentage",  5,   "TotalCost",   workCenters),
            };
            _ctx.OverheadRules.AddRange(overheadRules);
            await _ctx.SaveChangesAsync();

            await tx.CommitAsync();
            _logger.LogInformation("Manufacturing master data seeded for Company: {CompanyId}", companyId);
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ?? Helpers ???????????????????????????????????????????????????????????????

    private static WorkCenter WorkCenter(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        string code, string name, decimal capacityPerHour, decimal? hourlyMachineCost) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        Code = code, Name = name, CapacityPerHour = capacityPerHour,
        HourlyMachineCost = hourlyMachineCost == 0 ? null : hourlyMachineCost, IsActive = true
    };

    private static Domain.Entities.OverheadRule OverheadRule(
        (Guid c, Guid b, Guid bu, Guid u) T, DateTime now,
        string code, string name, string? wcCode, string rateType, decimal value, string appliesTo,
        WorkCenter[] workCenters) => new()
    {
        Id = Guid.NewGuid(), CompanyId = T.c, BranchId = T.b, BusinessUnitId = T.bu,
        CreatedByUserId = T.u, CreatedAt = now,
        Code = code, Name = name, RateType = rateType, Value = value,
        AppliesTo = appliesTo, IsActive = true, EffectiveFrom = now,
        WorkCenterId = wcCode == null ? null : workCenters.FirstOrDefault(w => w.Code == wcCode)?.Id
    };
}
