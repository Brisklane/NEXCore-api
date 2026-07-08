using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _repo;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(ICurrencyRepository repo, ILogger<CurrencyService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CurrencyDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving currencies"); throw; }
    }

    public async Task<CurrencyDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving currency {Id}", id); throw; }
    }

    public async Task<CurrencyDto> CreateAsync(CreateCurrencyDto request, Guid userId)
    {
        try
        {
            var entity = new Currency
            {
                CurrencyCode = request.CurrencyCode, CurrencyName = request.CurrencyName,
                Symbol = request.Symbol, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Currency created: {Code} ({Id})", entity.CurrencyCode, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating currency"); throw; }
    }

    public async Task<CurrencyDto> UpdateAsync(Guid id, UpdateCurrencyDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Currency not found");
            if (!string.IsNullOrWhiteSpace(request.CurrencyName)) entity.CurrencyName = request.CurrencyName;
            if (request.Symbol != null) entity.Symbol = request.Symbol;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating currency {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Currency not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting currency {Id}", id); throw; }
    }

    private static CurrencyDto Map(Currency e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CurrencyCode = e.CurrencyCode,
        CurrencyName = e.CurrencyName, Symbol = e.Symbol,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
