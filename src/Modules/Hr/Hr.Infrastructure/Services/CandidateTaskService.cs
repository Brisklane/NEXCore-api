using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CandidateTaskService : ICandidateTaskService
{
    private readonly ICandidateTaskRepository _repo;
    private readonly ILogger<CandidateTaskService> _logger;

    public CandidateTaskService(ICandidateTaskRepository repo, ILogger<CandidateTaskService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateTaskDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate tasks"); throw; }
    }

    public async Task<CandidateTaskDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate task {Id}", id); throw; }
    }

    public async Task<IEnumerable<CandidateTaskDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        try { return (await _repo.GetByApplicationIdAsync(applicationId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tasks for application {Id}", applicationId); throw; }
    }

    public async Task<IEnumerable<CandidateTaskDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tasks for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateTaskDto> CreateAsync(CreateCandidateTaskDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateTask { TaskCode = request.TaskCode, ApplicationId = request.ApplicationId, CandidateId = request.CandidateId, JobId = request.JobId, TaskTypeLookupValueId = request.TaskTypeLookupValueId, SourceType = request.SourceType, ExternalProvider = request.ExternalProvider, DueDate = request.DueDate, StatusLookupValueId = request.StatusLookupValueId, AssignedByEmployeeId = request.AssignedByEmployeeId, Description = request.Description, MaxScore = request.MaxScore, PassingScore = request.PassingScore, AttemptAllowed = request.AttemptAllowed, IsMandatory = request.IsMandatory, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("CandidateTask created: {Code} ({Id})", entity.TaskCode, entity.Id);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate task"); throw; }
    }

    public async Task<CandidateTaskDto> UpdateAsync(Guid id, UpdateCandidateTaskDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate task not found");
            if (request.DueDate.HasValue) entity.DueDate = request.DueDate;
            if (request.StatusLookupValueId.HasValue) entity.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.Description != null) entity.Description = request.Description;
            if (request.MaxScore.HasValue) entity.MaxScore = request.MaxScore;
            if (request.PassingScore.HasValue) entity.PassingScore = request.PassingScore;
            if (request.AttemptAllowed.HasValue) entity.AttemptAllowed = request.AttemptAllowed.Value;
            if (request.IsMandatory.HasValue) entity.IsMandatory = request.IsMandatory.Value;
            if (request.CancelReason != null) entity.CancelReason = request.CancelReason;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate task {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate task not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate task {Id}", id); throw; }
    }

    private static CandidateTaskDto Map(CandidateTask e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, TaskCode = e.TaskCode, ApplicationId = e.ApplicationId,
        CandidateId = e.CandidateId, JobId = e.JobId, TaskTypeLookupValueId = e.TaskTypeLookupValueId,
        SourceType = e.SourceType, ExternalProvider = e.ExternalProvider,
        ProviderReferenceId = e.ProviderReferenceId, AssignedDate = e.AssignedDate, DueDate = e.DueDate,
        StatusLookupValueId = e.StatusLookupValueId, AssignedByEmployeeId = e.AssignedByEmployeeId,
        Description = e.Description, MaxScore = e.MaxScore, PassingScore = e.PassingScore,
        AttemptAllowed = e.AttemptAllowed, IsMandatory = e.IsMandatory,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
