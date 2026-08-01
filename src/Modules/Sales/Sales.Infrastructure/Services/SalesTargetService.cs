using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Quota CRUD. Ported from the CRM module when SalesTarget was consolidated here so that
/// commission and forecasting read the same record.
/// </summary>
public class SalesTargetService : ISalesTargetService
{
    private readonly SalesDbContext _db;
    private readonly ILogger<SalesTargetService> _logger;

    public SalesTargetService(SalesDbContext db, ILogger<SalesTargetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SalesTargetDto> CreateAsync(CreateSalesTargetDto dto, Guid userId)
    {
        var entity = new SalesTarget
        {
            SalesRepId       = dto.SalesRepId,
            SalesTeamId      = dto.SalesTeamId,
            SalesTerritoryId = dto.SalesTerritoryId,
            Period           = dto.Period,
            PeriodStart      = dto.PeriodStart,
            PeriodEnd        = dto.PeriodEnd,
            FiscalYear       = dto.FiscalYear,
            FiscalQuarter    = dto.FiscalQuarter,
            FiscalMonth      = dto.FiscalMonth,
            TargetAmount     = dto.TargetAmount,
            TargetQuantity   = dto.TargetQuantity,
            TargetDealsCount = dto.TargetDealsCount,
            CurrencyCode     = dto.CurrencyCode,
            Notes            = dto.Notes,
            Description      = dto.Description,
            CreatedByUserId  = userId,
            CreatedAt        = DateTime.UtcNow,
        };

        _db.SalesTargets.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created sales target {TargetId} for fiscal year {FiscalYear}", entity.Id, entity.FiscalYear);
        return MapToDto(entity);
    }

    public async Task<SalesTargetDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.SalesTargets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<SalesTargetDto>> GetBySalesRepAsync(Guid salesRepId, int? fiscalYear = null)
        => await QueryAsync(x => x.SalesRepId == salesRepId, fiscalYear);

    public async Task<IEnumerable<SalesTargetDto>> GetByTeamAsync(Guid salesTeamId, int? fiscalYear = null)
        => await QueryAsync(x => x.SalesTeamId == salesTeamId, fiscalYear);

    public async Task<IEnumerable<SalesTargetDto>> GetByTerritoryAsync(Guid salesTerritoryId, int? fiscalYear = null)
        => await QueryAsync(x => x.SalesTerritoryId == salesTerritoryId, fiscalYear);

    public async Task<SalesTargetDto> UpdateAsync(Guid id, UpdateSalesTargetDto dto, Guid userId)
    {
        var entity = await _db.SalesTargets.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Sales target not found");

        entity.SalesRepId       = dto.SalesRepId;
        entity.SalesTeamId      = dto.SalesTeamId;
        entity.SalesTerritoryId = dto.SalesTerritoryId;
        entity.Period           = dto.Period;
        entity.PeriodStart      = dto.PeriodStart;
        entity.PeriodEnd        = dto.PeriodEnd;
        entity.FiscalYear       = dto.FiscalYear;
        entity.FiscalQuarter    = dto.FiscalQuarter;
        entity.FiscalMonth      = dto.FiscalMonth;
        entity.TargetAmount     = dto.TargetAmount;
        entity.TargetQuantity   = dto.TargetQuantity;
        entity.TargetDealsCount = dto.TargetDealsCount;
        entity.CurrencyCode     = dto.CurrencyCode;
        entity.Notes            = dto.Notes;
        entity.Description      = dto.Description;
        entity.UpdatedAt        = DateTime.UtcNow;
        entity.UpdatedByUserId  = userId;

        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.SalesTargets.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Sales target not found");

        entity.IsDeleted       = true;
        entity.DeletedAt       = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private async Task<IEnumerable<SalesTargetDto>> QueryAsync(
        System.Linq.Expressions.Expression<Func<SalesTarget, bool>> predicate, int? fiscalYear)
    {
        var query = _db.SalesTargets.AsNoTracking().Where(x => !x.IsDeleted).Where(predicate);
        if (fiscalYear.HasValue) query = query.Where(x => x.FiscalYear == fiscalYear.Value);
        return (await query.ToListAsync()).Select(MapToDto);
    }

    private static SalesTargetDto MapToDto(SalesTarget e) => new()
    {
        Id                   = e.Id,
        SalesRepId           = e.SalesRepId,
        SalesTeamId          = e.SalesTeamId,
        SalesTerritoryId     = e.SalesTerritoryId,
        Period               = e.Period,
        PeriodStart          = e.PeriodStart,
        PeriodEnd            = e.PeriodEnd,
        FiscalYear           = e.FiscalYear,
        FiscalQuarter        = e.FiscalQuarter,
        FiscalMonth          = e.FiscalMonth,
        TargetAmount         = e.TargetAmount,
        TargetQuantity       = e.TargetQuantity,
        TargetDealsCount     = e.TargetDealsCount,
        CurrencyCode         = e.CurrencyCode,
        ActualAmount         = e.ActualAmount,
        ActualQuantity       = e.ActualQuantity,
        ActualDealsCount     = e.ActualDealsCount,
        AttainmentPercentage = e.AttainmentPercentage,
        Notes                = e.Notes,
        Description          = e.Description,
        CreatedAt            = e.CreatedAt,
    };
}
