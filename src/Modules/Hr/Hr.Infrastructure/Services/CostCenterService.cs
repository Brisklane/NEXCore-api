using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CostCenterService : ICostCenterService
{
    private readonly ICostCenterRepository _repo;
    private readonly ILogger<CostCenterService> _logger;

    public CostCenterService(ICostCenterRepository repo, ILogger<CostCenterService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CostCenterDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving cost centers"); throw; }
    }

    public async Task<CostCenterDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving cost center {Id}", id); throw; }
    }

    public async Task<CostCenterDto> CreateAsync(CreateCostCenterDto request, Guid userId)
    {
        try
        {
            var entity = new CostCenter
            {
                CostCenterCode = request.CostCenterCode, CostCenterName = request.CostCenterName,
                DepartmentId = request.DepartmentId, BudgetOwnerEmployeeId = request.BudgetOwnerEmployeeId,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Cost center created: {Name} ({Id})", entity.CostCenterName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating cost center"); throw; }
    }

    public async Task<CostCenterDto> UpdateAsync(Guid id, UpdateCostCenterDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Cost center not found");
            if (!string.IsNullOrWhiteSpace(request.CostCenterName)) entity.CostCenterName = request.CostCenterName;
            if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId;
            if (request.BudgetOwnerEmployeeId.HasValue) entity.BudgetOwnerEmployeeId = request.BudgetOwnerEmployeeId;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating cost center {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Cost center not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting cost center {Id}", id); throw; }
    }

    private static CostCenterDto Map(CostCenter e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CostCenterCode = e.CostCenterCode,
        CostCenterName = e.CostCenterName, DepartmentId = e.DepartmentId,
        BudgetOwnerEmployeeId = e.BudgetOwnerEmployeeId,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
