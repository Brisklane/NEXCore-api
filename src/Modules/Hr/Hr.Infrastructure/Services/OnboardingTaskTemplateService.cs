using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class OnboardingTaskTemplateService : IOnboardingTaskTemplateService
{
    private readonly IOnboardingTaskTemplateRepository _repo;
    private readonly ILogger<OnboardingTaskTemplateService> _logger;

    public OnboardingTaskTemplateService(IOnboardingTaskTemplateRepository repo, ILogger<OnboardingTaskTemplateService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<OnboardingTaskTemplateDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving onboarding task templates"); throw; }
    }

    public async Task<OnboardingTaskTemplateDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving onboarding task template {Id}", id); throw; }
    }

    public async Task<OnboardingTaskTemplateDto> CreateAsync(CreateOnboardingTaskTemplateDto request, Guid userId)
    {
        try
        {
            var entity = new OnboardingTaskTemplate
            {
                TemplateCode = request.TemplateCode, TaskName = request.TaskName,
                TaskCategory = request.TaskCategory, Description = request.Description,
                DefaultAssigneeRole = request.DefaultAssigneeRole,
                DefaultDueDaysFromStart = request.DefaultDueDaysFromStart,
                IsRequired = request.IsRequired, SortOrder = request.SortOrder,
                ApplicableDepartmentId = request.ApplicableDepartmentId,
                ApplicableDesignationId = request.ApplicableDesignationId,
                ApplicableEmploymentType = request.ApplicableEmploymentType,
                ApplicableLocationId = request.ApplicableLocationId,
                DependsOnTemplateId = request.DependsOnTemplateId,
                EstimatedHours = request.EstimatedHours,
                RequiresDocumentUpload = request.RequiresDocumentUpload,
                RequiresManagerSignoff = request.RequiresManagerSignoff,
                NotifyEmployeeOnAssign = request.NotifyEmployeeOnAssign,
                NotifyAssigneeOnCreate = request.NotifyAssigneeOnCreate,
                EscalateAfterDays = request.EscalateAfterDays,
                EscalateToRole = request.EscalateToRole,
                IsActive = true, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("OnboardingTaskTemplate created: {Name} ({Id})", entity.TaskName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating onboarding task template"); throw; }
    }

    public async Task<OnboardingTaskTemplateDto> UpdateAsync(Guid id, UpdateOnboardingTaskTemplateDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Onboarding task template not found");
            if (!string.IsNullOrWhiteSpace(request.TaskName)) entity.TaskName = request.TaskName;
            if (request.TaskCategory != null) entity.TaskCategory = request.TaskCategory;
            if (request.Description != null) entity.Description = request.Description;
            if (request.DefaultAssigneeRole != null) entity.DefaultAssigneeRole = request.DefaultAssigneeRole;
            if (request.DefaultDueDaysFromStart.HasValue) entity.DefaultDueDaysFromStart = request.DefaultDueDaysFromStart.Value;
            if (request.IsRequired.HasValue) entity.IsRequired = request.IsRequired.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
            if (request.ApplicableDepartmentId.HasValue) entity.ApplicableDepartmentId = request.ApplicableDepartmentId;
            if (request.ApplicableDesignationId.HasValue) entity.ApplicableDesignationId = request.ApplicableDesignationId;
            if (request.ApplicableEmploymentType != null) entity.ApplicableEmploymentType = request.ApplicableEmploymentType;
            if (request.ApplicableLocationId.HasValue) entity.ApplicableLocationId = request.ApplicableLocationId;
            if (request.EstimatedHours.HasValue) entity.EstimatedHours = request.EstimatedHours;
            if (request.RequiresDocumentUpload.HasValue) entity.RequiresDocumentUpload = request.RequiresDocumentUpload.Value;
            if (request.RequiresManagerSignoff.HasValue) entity.RequiresManagerSignoff = request.RequiresManagerSignoff.Value;
            if (request.EscalateAfterDays.HasValue) entity.EscalateAfterDays = request.EscalateAfterDays;
            if (request.EscalateToRole != null) entity.EscalateToRole = request.EscalateToRole;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating onboarding task template {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Onboarding task template not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting onboarding task template {Id}", id); throw; }
    }

    private static OnboardingTaskTemplateDto Map(OnboardingTaskTemplate e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, TemplateCode = e.TemplateCode,
        TaskName = e.TaskName, TaskCategory = e.TaskCategory, Description = e.Description,
        DefaultAssigneeRole = e.DefaultAssigneeRole, DefaultDueDaysFromStart = e.DefaultDueDaysFromStart,
        IsRequired = e.IsRequired, IsActive = e.IsActive, SortOrder = e.SortOrder,
        ApplicableDepartmentId = e.ApplicableDepartmentId, ApplicableDesignationId = e.ApplicableDesignationId,
        ApplicableEmploymentType = e.ApplicableEmploymentType, ApplicableLocationId = e.ApplicableLocationId,
        DependsOnTemplateId = e.DependsOnTemplateId, EstimatedHours = e.EstimatedHours,
        RequiresDocumentUpload = e.RequiresDocumentUpload, RequiresManagerSignoff = e.RequiresManagerSignoff,
        NotifyEmployeeOnAssign = e.NotifyEmployeeOnAssign, NotifyAssigneeOnCreate = e.NotifyAssigneeOnCreate,
        EscalateAfterDays = e.EscalateAfterDays, EscalateToRole = e.EscalateToRole,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
