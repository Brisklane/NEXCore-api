using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class AllowancesProfileService : IAllowancesProfileService
{
    private readonly IAllowancesProfileRepository _repo;
    private readonly ILogger<AllowancesProfileService> _logger;

    public AllowancesProfileService(IAllowancesProfileRepository repo, ILogger<AllowancesProfileService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<AllowancesProfileDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving allowances profiles"); throw; }
    }

    public async Task<AllowancesProfileDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving allowances profile {Id}", id); throw; }
    }

    public async Task<AllowancesProfileDto> CreateAsync(CreateAllowancesProfileDto request, Guid userId)
    {
        try
        {
            var entity = new AllowancesProfile
            {
                AllowancesProfileCode = request.AllowancesProfileCode,
                AllowancesProfileName = request.AllowancesProfileName,
                Description = request.Description, IsActive = true,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Allowances profile created: {Name} ({Id})", entity.AllowancesProfileName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating allowances profile"); throw; }
    }

    public async Task<AllowancesProfileDto> UpdateAsync(Guid id, UpdateAllowancesProfileDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Allowances profile not found");
            if (!string.IsNullOrWhiteSpace(request.AllowancesProfileName)) entity.AllowancesProfileName = request.AllowancesProfileName;
            if (request.Description != null) entity.Description = request.Description;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating allowances profile {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Allowances profile not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting allowances profile {Id}", id); throw; }
    }

    private static AllowancesProfileDto Map(AllowancesProfile e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId,
        AllowancesProfileCode = e.AllowancesProfileCode, AllowancesProfileName = e.AllowancesProfileName,
        Description = e.Description, IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
