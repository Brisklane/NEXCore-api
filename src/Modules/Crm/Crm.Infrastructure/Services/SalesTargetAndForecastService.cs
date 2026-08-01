using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class ForecastService : IForecastService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<ForecastService> _logger;

    public ForecastService(CrmDbContext db, ILogger<ForecastService> logger) { _db = db; _logger = logger; }

    public async Task<ForecastDto> CreateAsync(CreateForecastDto dto, Guid userId)
    {
        var entity = new Forecast
        {
            UserId = dto.UserId, FiscalYear = dto.FiscalYear, FiscalQuarter = dto.FiscalQuarter,
            FiscalMonth = dto.FiscalMonth, PeriodStartDate = dto.PeriodStartDate, PeriodEndDate = dto.PeriodEndDate,
            PipelineAmount = dto.PipelineAmount, BestCaseAmount = dto.BestCaseAmount,
            CommitAmount = dto.CommitAmount, ClosedAmount = dto.ClosedAmount,
            AdjustedAmount = dto.AdjustedAmount, QuotaAmount = dto.QuotaAmount, CurrencyCode = dto.CurrencyCode,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Forecasts.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ForecastDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Forecasts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<ForecastDto>> GetByUserAsync(Guid userId, int fiscalYear, int fiscalQuarter)
    {
        var items = await _db.Forecasts.AsNoTracking()
            .Where(x => x.UserId == userId && x.FiscalYear == fiscalYear && x.FiscalQuarter == fiscalQuarter && !x.IsDeleted)
            .ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<ForecastDto> UpdateAsync(Guid id, UpdateForecastDto dto, Guid userId)
    {
        var entity = await _db.Forecasts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Forecast not found");
        entity.PipelineAmount = dto.PipelineAmount; entity.BestCaseAmount = dto.BestCaseAmount;
        entity.CommitAmount = dto.CommitAmount; entity.ClosedAmount = dto.ClosedAmount;
        entity.AdjustedAmount = dto.AdjustedAmount; entity.QuotaAmount = dto.QuotaAmount;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ForecastDto> SubmitAsync(Guid id, Guid userId)
    {
        var entity = await _db.Forecasts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Forecast not found");
        entity.IsSubmitted = true; entity.SubmittedDate = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Forecasts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Forecast not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static ForecastDto MapToDto(Forecast e) => new()
    {
        Id = e.Id, UserId = e.UserId, FiscalYear = e.FiscalYear, FiscalQuarter = e.FiscalQuarter,
        FiscalMonth = e.FiscalMonth, PeriodStartDate = e.PeriodStartDate, PeriodEndDate = e.PeriodEndDate,
        PipelineAmount = e.PipelineAmount, BestCaseAmount = e.BestCaseAmount,
        CommitAmount = e.CommitAmount, ClosedAmount = e.ClosedAmount,
        AdjustedAmount = e.AdjustedAmount, QuotaAmount = e.QuotaAmount, CurrencyCode = e.CurrencyCode,
        IsManagerAdjusted = e.IsManagerAdjusted, IsSubmitted = e.IsSubmitted,
        SubmittedDate = e.SubmittedDate, CreatedAt = e.CreatedAt
    };
}
