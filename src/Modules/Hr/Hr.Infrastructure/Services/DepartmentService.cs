using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly ILogger<DepartmentService> _logger;

    public DepartmentService(IDepartmentRepository repo, ILogger<DepartmentService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving departments"); throw; }
    }

    public async Task<DepartmentDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving department {Id}", id); throw; }
    }

    public async Task<IEnumerable<DepartmentDto>> GetByParentAsync(Guid parentDepartmentId)
    {
        try { return (await _repo.GetByParentAsync(parentDepartmentId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving child departments for {ParentId}", parentDepartmentId); throw; }
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto request, Guid userId)
    {
        try
        {
            var entity = new Department
            {
                DepartmentCode = request.DepartmentCode,
                DepartmentName = request.DepartmentName,
                ParentDepartmentId = request.ParentDepartmentId,
                DepartmentHeadEmployeeId = request.DepartmentHeadEmployeeId,
                CostCenterId = request.CostCenterId,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Department created: {Name} ({Id})", entity.DepartmentName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating department"); throw; }
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Department not found");
            if (!string.IsNullOrWhiteSpace(request.DepartmentName)) entity.DepartmentName = request.DepartmentName;
            if (request.ParentDepartmentId.HasValue) entity.ParentDepartmentId = request.ParentDepartmentId;
            if (request.DepartmentHeadEmployeeId.HasValue) entity.DepartmentHeadEmployeeId = request.DepartmentHeadEmployeeId;
            if (request.CostCenterId.HasValue) entity.CostCenterId = request.CostCenterId;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating department {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Department not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting department {Id}", id); throw; }
    }

    public async Task<IEnumerable<HrLookupItemDto>> GetLookupListAsync()
    {
        try
        {
            return (await _repo.GetAllByTenantAsync())
                .Where(e => !e.IsDeleted && e.IsActive)
                .OrderBy(e => e.DepartmentName)
                .Select(e => new HrLookupItemDto
                {
                    Value = e.Id.ToString(),
                    Label = e.DepartmentName
                });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving department lookup list"); throw; }
    }

    private static DepartmentDto Map(Department e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, DepartmentCode = e.DepartmentCode,
        DepartmentName = e.DepartmentName, ParentDepartmentId = e.ParentDepartmentId,
        DepartmentHeadEmployeeId = e.DepartmentHeadEmployeeId, CostCenterId = e.CostCenterId,
        IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
