using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class BenefitsPlanService : IBenefitsPlanService
{
    private readonly IBenefitsPlanRepository _repo;
    private readonly ILogger<BenefitsPlanService> _logger;

    public BenefitsPlanService(IBenefitsPlanRepository repo, ILogger<BenefitsPlanService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<BenefitsPlanDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving benefits plans"); throw; }
    }

    public async Task<BenefitsPlanDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving benefits plan {Id}", id); throw; }
    }

    public async Task<BenefitsPlanDto> CreateAsync(CreateBenefitsPlanDto request, Guid userId)
    {
        try
        {
            var entity = new BenefitsPlan
            {
                BenefitsPlanCode = request.BenefitsPlanCode, BenefitsPlanName = request.BenefitsPlanName,
                Description = request.Description, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Benefits plan created: {Name} ({Id})", entity.BenefitsPlanName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating benefits plan"); throw; }
    }

    public async Task<BenefitsPlanDto> UpdateAsync(Guid id, UpdateBenefitsPlanDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Benefits plan not found");
            if (!string.IsNullOrWhiteSpace(request.BenefitsPlanName)) entity.BenefitsPlanName = request.BenefitsPlanName;
            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating benefits plan {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Benefits plan not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting benefits plan {Id}", id); throw; }
    }

    private static BenefitsPlanDto Map(BenefitsPlan e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId,
        BenefitsPlanCode = e.BenefitsPlanCode, BenefitsPlanName = e.BenefitsPlanName,
        Description = e.Description, IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
