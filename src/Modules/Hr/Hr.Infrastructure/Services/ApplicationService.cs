using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using AppEntity = Hr.Domain.Entities.Application;

namespace Hr.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly IApplicationRepository _repository;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(IApplicationRepository repository, ILogger<ApplicationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ApplicationDto>> GetAllAsync()
    {
        try
        {
            var apps = await _repository.GetAllByTenantAsync();
            return apps.Where(a => !a.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications"); throw; }
    }

    public async Task<ApplicationDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var app = await _repository.GetByIdAsync(id);
            return app == null ? null : MapToDto(app);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application: {ApplicationId}", id); throw; }
    }

    public async Task<IEnumerable<ApplicationDto>> GetByJobIdAsync(Guid jobId)
    {
        try
        {
            var apps = await _repository.GetByJobIdAsync(jobId);
            return apps.Where(a => !a.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications by job"); throw; }
    }

    public async Task<IEnumerable<ApplicationDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try
        {
            var apps = await _repository.GetByCandidateIdAsync(candidateId);
            return apps.Where(a => !a.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications by candidate"); throw; }
    }

    public async Task<ApplicationDto> CreateAsync(CreateApplicationDto request, Guid userId)
    {
        try
        {
            var app = new AppEntity
            {
                ApplicationCode = request.ApplicationCode,
                JobId = request.JobId,
                CandidateId = request.CandidateId,
                JobPostingChannelId = request.JobPostingChannelId,
                CurrentStageLookupValueId = request.CurrentStageLookupValueId,
                StatusLookupValueId = request.StatusLookupValueId,
                PriorityLookupValueId = request.PriorityLookupValueId,
                AssignedRecruiterEmployeeId = request.AssignedRecruiterEmployeeId,
                AppliedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(app);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Application created: {ApplicationCode} (ID: {AppId})", app.ApplicationCode, app.Id);
            return MapToDto(app);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application"); throw; }
    }

    public async Task<ApplicationDto> UpdateAsync(Guid id, UpdateApplicationDto request, Guid userId)
    {
        try
        {
            var app = await _repository.GetByIdAsync(id);
            if (app == null || app.IsDeleted)
                throw new InvalidOperationException("Application not found");

            if (request.CurrentStageLookupValueId.HasValue) app.CurrentStageLookupValueId = request.CurrentStageLookupValueId.Value;
            if (request.StatusLookupValueId.HasValue) app.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.PriorityLookupValueId.HasValue) app.PriorityLookupValueId = request.PriorityLookupValueId;
            if (request.IsShortlisted.HasValue) app.IsShortlisted = request.IsShortlisted.Value;
            if (request.ScreeningScore.HasValue) app.ScreeningScore = request.ScreeningScore;
            if (request.InternalScore.HasValue) app.InternalScore = request.InternalScore;
            if (request.AssignedRecruiterEmployeeId.HasValue) app.AssignedRecruiterEmployeeId = request.AssignedRecruiterEmployeeId;

            app.UpdatedAt = DateTime.UtcNow;
            app.UpdatedByUserId = userId;

            _repository.Update(app);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Application updated: {ApplicationId}", id);
            return MapToDto(app);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application: {ApplicationId}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var app = await _repository.GetByIdAsync(id);
            if (app == null || app.IsDeleted)
                throw new InvalidOperationException("Application not found");

            app.IsDeleted = true;
            app.DeletedAt = DateTime.UtcNow;
            app.DeletedByUserId = userId;

            _repository.Update(app);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application: {ApplicationId}", id); throw; }
    }

    private static ApplicationDto MapToDto(AppEntity a) => new()
    {
        Id = a.Id,
        CompanyId = a.CompanyId,
        ApplicationCode = a.ApplicationCode,
        JobId = a.JobId,
        CandidateId = a.CandidateId,
        JobPostingChannelId = a.JobPostingChannelId,
        AppliedDate = a.AppliedDate,
        CurrentStageLookupValueId = a.CurrentStageLookupValueId,
        StatusLookupValueId = a.StatusLookupValueId,
        PriorityLookupValueId = a.PriorityLookupValueId,
        IsShortlisted = a.IsShortlisted,
        ScreeningScore = a.ScreeningScore,
        InternalScore = a.InternalScore,
        AssignedRecruiterEmployeeId = a.AssignedRecruiterEmployeeId,
        HiredDate = a.HiredDate,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
