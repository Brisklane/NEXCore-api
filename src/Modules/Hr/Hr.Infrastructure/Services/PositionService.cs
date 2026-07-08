using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _repo;
    private readonly ILogger<PositionService> _logger;

    public PositionService(IPositionRepository repo, ILogger<PositionService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<PositionDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving positions"); throw; }
    }

    public async Task<PositionDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving position {Id}", id); throw; }
    }

    public async Task<IEnumerable<PositionDto>> GetVacantAsync()
    {
        try { return (await _repo.GetVacantAsync()).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vacant positions"); throw; }
    }

    public async Task<PositionDto> CreateAsync(CreatePositionDto request, Guid userId)
    {
        try
        {
            var entity = new Position
            {
                PositionCode = request.PositionCode, PositionName = request.PositionName,
                DepartmentId = request.DepartmentId, DesignationId = request.DesignationId,
                JobFamilyId = request.JobFamilyId, JobFunctionId = request.JobFunctionId,
                GradeId = request.GradeId, PayScaleId = request.PayScaleId,
                ReportsToPositionId = request.ReportsToPositionId,
                IsVacant = true, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("Position created: {Name} ({Id})", entity.PositionName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating position"); throw; }
    }

    public async Task<PositionDto> UpdateAsync(Guid id, UpdatePositionDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Position not found");
            if (!string.IsNullOrWhiteSpace(request.PositionName)) entity.PositionName = request.PositionName;
            if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId.Value;
            if (request.DesignationId.HasValue) entity.DesignationId = request.DesignationId.Value;
            if (request.JobFamilyId.HasValue) entity.JobFamilyId = request.JobFamilyId;
            if (request.JobFunctionId.HasValue) entity.JobFunctionId = request.JobFunctionId;
            if (request.GradeId.HasValue) entity.GradeId = request.GradeId;
            if (request.PayScaleId.HasValue) entity.PayScaleId = request.PayScaleId;
            if (request.ReportsToPositionId.HasValue) entity.ReportsToPositionId = request.ReportsToPositionId;
            if (request.IsVacant.HasValue) entity.IsVacant = request.IsVacant.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating position {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Position not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting position {Id}", id); throw; }
    }

    private static PositionDto Map(Position e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, PositionCode = e.PositionCode,
        PositionName = e.PositionName, DepartmentId = e.DepartmentId, DesignationId = e.DesignationId,
        JobFamilyId = e.JobFamilyId, JobFunctionId = e.JobFunctionId, GradeId = e.GradeId,
        PayScaleId = e.PayScaleId, ReportsToPositionId = e.ReportsToPositionId,
        IsVacant = e.IsVacant, IsActive = e.IsActive, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
