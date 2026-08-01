using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class PayScaleService : IPayScaleService
{
    private readonly IPayScaleRepository _repo;
    private readonly ILogger<PayScaleService> _logger;

    public PayScaleService(IPayScaleRepository repo, ILogger<PayScaleService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<PayScaleDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pay scales"); throw; }
    }

    public async Task<PayScaleDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pay scale {Id}", id); throw; }
    }

    public async Task<PayScaleDto> CreateAsync(CreatePayScaleDto request, Guid userId)
    {
        try
        {
            var entity = new PayScale
            {
                PayScaleCode = request.PayScaleCode, PayScaleName = request.PayScaleName,
                CurrencyCode = request.CurrencyCode, MinAmount = request.MinAmount, MaxAmount = request.MaxAmount,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Pay scale created: {Name} ({Id})", entity.PayScaleName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating pay scale"); throw; }
    }

    public async Task<PayScaleDto> UpdateAsync(Guid id, UpdatePayScaleDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Pay scale not found");
            if (!string.IsNullOrWhiteSpace(request.PayScaleName)) entity.PayScaleName = request.PayScaleName;
            if (!string.IsNullOrWhiteSpace(request.CurrencyCode)) entity.CurrencyCode = request.CurrencyCode;
            if (request.MinAmount.HasValue) entity.MinAmount = request.MinAmount;
            if (request.MaxAmount.HasValue) entity.MaxAmount = request.MaxAmount;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating pay scale {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Pay scale not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting pay scale {Id}", id); throw; }
    }

    private static PayScaleDto Map(PayScale e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, PayScaleCode = e.PayScaleCode,
        PayScaleName = e.PayScaleName, CurrencyCode = e.CurrencyCode,
        MinAmount = e.MinAmount, MaxAmount = e.MaxAmount,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
