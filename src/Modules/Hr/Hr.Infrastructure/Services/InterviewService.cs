using Hr.Application.DTOs;
using Hr.Application.Enums;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly IInterviewRepository _repository;
    private readonly ILogger<InterviewService> _logger;

    public InterviewService(IInterviewRepository repository, ILogger<InterviewService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<InterviewDto>> GetAllAsync()
    {
        try
        {
            var interviews = await _repository.GetAllByTenantAsync();
            return interviews.Where(i => !i.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviews"); throw; }
    }

    public async Task<InterviewDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var interview = await _repository.GetByIdAsync(id);
            return interview == null ? null : MapToDto(interview);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview: {InterviewId}", id); throw; }
    }

    public async Task<IEnumerable<InterviewDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        try
        {
            var interviews = await _repository.GetByApplicationIdAsync(applicationId);
            return interviews.Where(i => !i.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviews by application"); throw; }
    }

    public async Task<InterviewDto> CreateAsync(CreateInterviewDto request, Guid userId)
    {
        try
        {
            var interview = new Interview
            {
                InterviewCode = request.InterviewCode,
                ApplicationId = request.ApplicationId,
                CandidateId = request.CandidateId,
                JobId = request.JobId,
                InterviewTitle = request.InterviewTitle,
                InterviewTypeLookupValueId = request.InterviewTypeLookupValueId,
                InterviewSequenceNo = request.InterviewSequenceNo,
                StatusLookupValueId = request.StatusLookupValueId,
                ScheduledStart = request.ScheduledStart,
                ScheduledEnd = request.ScheduledEnd,
                DurationMinutes = request.DurationMinutes,
                Format = request.Format,
                Location = request.Location,
                VideoLink = request.VideoLink,
                IsMandatoryRound = request.IsMandatoryRound,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(interview);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Interview created: {InterviewCode} (ID: {InterviewId})", interview.InterviewCode, interview.Id);
            return MapToDto(interview);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview"); throw; }
    }

    public async Task<InterviewDto> UpdateAsync(Guid id, UpdateInterviewDto request, Guid userId)
    {
        try
        {
            var interview = await _repository.GetByIdAsync(id);
            if (interview == null || interview.IsDeleted)
                throw new InvalidOperationException("Interview not found");

            if (request.StatusLookupValueId.HasValue) interview.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.ScheduledStart.HasValue) interview.ScheduledStart = request.ScheduledStart;
            if (request.ScheduledEnd.HasValue) interview.ScheduledEnd = request.ScheduledEnd;
            if (request.Location != null) interview.Location = request.Location;
            if (request.VideoLink != null) interview.VideoLink = request.VideoLink;
            if (request.CandidateConfirmed.HasValue) interview.CandidateConfirmed = request.CandidateConfirmed.Value;
            if (request.CancelReason != null) interview.CancelReason = request.CancelReason;

            interview.UpdatedAt = DateTime.UtcNow;
            interview.UpdatedByUserId = userId;

            _repository.Update(interview);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Interview updated: {InterviewId}", id);
            return MapToDto(interview);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview: {InterviewId}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var interview = await _repository.GetByIdAsync(id);
            if (interview == null || interview.IsDeleted)
                throw new InvalidOperationException("Interview not found");

            interview.IsDeleted = true;
            interview.DeletedAt = DateTime.UtcNow;
            interview.DeletedByUserId = userId;

            _repository.Update(interview);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview: {InterviewId}", id); throw; }
    }

    private static InterviewDto MapToDto(Interview i) => new()
    {
        Id = i.Id,
        CompanyId = i.CompanyId,
        InterviewCode = i.InterviewCode,
        ApplicationId = i.ApplicationId,
        CandidateId = i.CandidateId,
        JobId = i.JobId,
        InterviewTitle = i.InterviewTitle,
        InterviewTypeLookupValueId = i.InterviewTypeLookupValueId,
        InterviewSequenceNo = i.InterviewSequenceNo,
        StatusLookupValueId = i.StatusLookupValueId,
        ScheduledStart = i.ScheduledStart,
        ScheduledEnd = i.ScheduledEnd,
        DurationMinutes = i.DurationMinutes,
        Format = i.Format,
        Location = i.Location,
        VideoLink = i.VideoLink,
        CandidateConfirmed = i.CandidateConfirmed,
        IsMandatoryRound = i.IsMandatoryRound,
        CreatedAt = i.CreatedAt,
        UpdatedAt = i.UpdatedAt
    };
}
