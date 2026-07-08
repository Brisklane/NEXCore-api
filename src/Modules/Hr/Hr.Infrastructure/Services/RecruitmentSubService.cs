using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class OnboardingTaskService : IOnboardingTaskService
{
    private readonly IOnboardingTaskRepository _repo;
    private readonly ILogger<OnboardingTaskService> _logger;

    public OnboardingTaskService(IOnboardingTaskRepository repo, ILogger<OnboardingTaskService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<OnboardingTaskDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving onboarding tasks"); throw; }
    }

    public async Task<OnboardingTaskDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving onboarding task {Id}", id); throw; }
    }

    public async Task<IEnumerable<OnboardingTaskDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        try { return (await _repo.GetByApplicationIdAsync(applicationId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving onboarding tasks for application {Id}", applicationId); throw; }
    }

    public async Task<OnboardingTaskDto> CreateAsync(CreateOnboardingTaskDto request, Guid userId)
    {
        try
        {
            var entity = new OnboardingTask { TaskCode = request.TaskCode, ApplicationId = request.ApplicationId, EmployeeId = request.EmployeeId, CandidateId = request.CandidateId, OnboardingTaskTemplateId = request.OnboardingTaskTemplateId, TaskName = request.TaskName, TaskCategory = request.TaskCategory, TaskDescription = request.TaskDescription, AssignedToEmployeeId = request.AssignedToEmployeeId, DueDate = request.DueDate, StatusLookupValueId = request.StatusLookupValueId, IsRequired = request.IsRequired, SortOrder = request.SortOrder, DependsOnTaskId = request.DependsOnTaskId, RequiresDocumentUpload = request.RequiresDocumentUpload, RequiresManagerSignOff = request.RequiresManagerSignOff, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("OnboardingTask created: {Name} ({Id})", entity.TaskName, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating onboarding task"); throw; }
    }

    public async Task<OnboardingTaskDto> UpdateAsync(Guid id, UpdateOnboardingTaskDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Onboarding task not found");
            if (request.TaskName != null) entity.TaskName = request.TaskName;
            if (request.TaskDescription != null) entity.TaskDescription = request.TaskDescription;
            if (request.AssignedToEmployeeId.HasValue) entity.AssignedToEmployeeId = request.AssignedToEmployeeId.Value;
            if (request.DueDate.HasValue) entity.DueDate = request.DueDate.Value;
            if (request.CompletionDate.HasValue) entity.CompletionDate = request.CompletionDate;
            if (request.StatusLookupValueId.HasValue) entity.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.CompletedByEmployeeId.HasValue) entity.CompletedByEmployeeId = request.CompletedByEmployeeId;
            if (request.VerifiedByEmployeeId.HasValue) entity.VerifiedByEmployeeId = request.VerifiedByEmployeeId;
            if (request.VerifiedAt.HasValue) entity.VerifiedAt = request.VerifiedAt;
            if (request.DocumentUrl != null) entity.DocumentUrl = request.DocumentUrl;
            if (request.EscalatedFlag.HasValue) entity.EscalatedFlag = request.EscalatedFlag.Value;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating onboarding task {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Onboarding task not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting onboarding task {Id}", id); throw; }
    }

    private static OnboardingTaskDto Map(OnboardingTask e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, TaskCode = e.TaskCode, ApplicationId = e.ApplicationId,
        EmployeeId = e.EmployeeId, CandidateId = e.CandidateId,
        OnboardingTaskTemplateId = e.OnboardingTaskTemplateId, TaskName = e.TaskName,
        TaskCategory = e.TaskCategory, TaskDescription = e.TaskDescription,
        AssignedToEmployeeId = e.AssignedToEmployeeId, DueDate = e.DueDate,
        CompletionDate = e.CompletionDate, StatusLookupValueId = e.StatusLookupValueId,
        IsAutoGenerated = e.IsAutoGenerated, IsRequired = e.IsRequired, SortOrder = e.SortOrder,
        DependsOnTaskId = e.DependsOnTaskId, RequiresDocumentUpload = e.RequiresDocumentUpload,
        RequiresManagerSignOff = e.RequiresManagerSignOff, EscalatedFlag = e.EscalatedFlag,
        SLABreached = e.SLABreached, Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CallLogService : ICallLogService
{
    private readonly ICallLogRepository _repo;
    private readonly ILogger<CallLogService> _logger;

    public CallLogService(ICallLogRepository repo, ILogger<CallLogService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CallLogDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving call logs"); throw; }
    }

    public async Task<CallLogDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving call log {Id}", id); throw; }
    }

    public async Task<IEnumerable<CallLogDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving call logs for candidate {Id}", candidateId); throw; }
    }

    public async Task<CallLogDto> CreateAsync(CreateCallLogDto request, Guid userId)
    {
        try
        {
            var entity = new CallLog { CandidateId = request.CandidateId, ApplicationId = request.ApplicationId, CalledByEmployeeId = request.CalledByEmployeeId, CallType = request.CallType, CallDate = request.CallDate, DurationMinutes = request.DurationMinutes, Notes = request.Notes, Outcome = request.Outcome, NextActionDate = request.NextActionDate, NextActionType = request.NextActionType, ContactMethod = request.ContactMethod, CallDirection = request.CallDirection, CommunicationTemplateId = request.CommunicationTemplateId, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating call log"); throw; }
    }

    public async Task<CallLogDto> UpdateAsync(Guid id, UpdateCallLogDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Call log not found");
            if (request.Notes != null) entity.Notes = request.Notes;
            if (request.Outcome != null) entity.Outcome = request.Outcome;
            if (request.DurationMinutes.HasValue) entity.DurationMinutes = request.DurationMinutes;
            if (request.NextActionDate.HasValue) entity.NextActionDate = request.NextActionDate;
            if (request.NextActionType != null) entity.NextActionType = request.NextActionType;
            if (request.IsFollowUpDone.HasValue) entity.IsFollowUpDone = request.IsFollowUpDone.Value;
            if (request.FollowUpCompletedAt.HasValue) entity.FollowUpCompletedAt = request.FollowUpCompletedAt;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating call log {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Call log not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting call log {Id}", id); throw; }
    }

    private static CallLogDto Map(CallLog e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId, ApplicationId = e.ApplicationId,
        CalledByEmployeeId = e.CalledByEmployeeId, CallType = e.CallType, CallDate = e.CallDate,
        DurationMinutes = e.DurationMinutes, Notes = e.Notes, Outcome = e.Outcome,
        NextActionDate = e.NextActionDate, NextActionType = e.NextActionType,
        ContactMethod = e.ContactMethod, CallDirection = e.CallDirection,
        IsFollowUpDone = e.IsFollowUpDone, CommunicationTemplateId = e.CommunicationTemplateId,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
