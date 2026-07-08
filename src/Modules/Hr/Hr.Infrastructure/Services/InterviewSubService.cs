using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class InterviewPanelMemberService : IInterviewPanelMemberService
{
    private readonly IInterviewPanelMemberRepository _repo;
    private readonly ILogger<InterviewPanelMemberService> _logger;

    public InterviewPanelMemberService(IInterviewPanelMemberRepository repo, ILogger<InterviewPanelMemberService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<InterviewPanelMemberDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview panel members"); throw; }
    }

    public async Task<InterviewPanelMemberDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview panel member {Id}", id); throw; }
    }

    public async Task<IEnumerable<InterviewPanelMemberDto>> GetByInterviewIdAsync(Guid interviewId)
    {
        try { return (await _repo.GetByInterviewIdAsync(interviewId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving panel members for interview {Id}", interviewId); throw; }
    }

    public async Task<InterviewPanelMemberDto> CreateAsync(CreateInterviewPanelMemberDto request, Guid userId)
    {
        try
        {
            var entity = new InterviewPanelMember { InterviewId = request.InterviewId, InterviewerEmployeeId = request.InterviewerEmployeeId, AlternateInterviewerEmployeeId = request.AlternateInterviewerEmployeeId, Role = request.Role, ScoreWeight = request.ScoreWeight, IsLead = request.IsLead, IsMandatory = request.IsMandatory, InviteStatusLookupValueId = request.InviteStatusLookupValueId, FeedbackDeadline = request.FeedbackDeadline, PanelSequence = request.PanelSequence, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview panel member"); throw; }
    }

    public async Task<InterviewPanelMemberDto> UpdateAsync(Guid id, UpdateInterviewPanelMemberDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interview panel member not found");
            if (request.AlternateInterviewerEmployeeId.HasValue) entity.AlternateInterviewerEmployeeId = request.AlternateInterviewerEmployeeId;
            if (request.Role != null) entity.Role = request.Role;
            if (request.ScoreWeight.HasValue) entity.ScoreWeight = request.ScoreWeight.Value;
            if (request.IsLead.HasValue) entity.IsLead = request.IsLead.Value;
            if (request.IsMandatory.HasValue) entity.IsMandatory = request.IsMandatory.Value;
            if (request.InviteStatusLookupValueId.HasValue) entity.InviteStatusLookupValueId = request.InviteStatusLookupValueId.Value;
            if (request.HasConflictOfInterest.HasValue) entity.HasConflictOfInterest = request.HasConflictOfInterest.Value;
            if (request.ConflictOfInterestNotes != null) entity.ConflictOfInterestNotes = request.ConflictOfInterestNotes;
            if (request.IsAvailabilityConfirmed.HasValue) entity.IsAvailabilityConfirmed = request.IsAvailabilityConfirmed.Value;
            if (request.FeedbackDeadline.HasValue) entity.FeedbackDeadline = request.FeedbackDeadline;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview panel member {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interview panel member not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview panel member {Id}", id); throw; }
    }

    private static InterviewPanelMemberDto Map(InterviewPanelMember e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, InterviewId = e.InterviewId,
        InterviewerEmployeeId = e.InterviewerEmployeeId, AlternateInterviewerEmployeeId = e.AlternateInterviewerEmployeeId,
        Role = e.Role, ScoreWeight = e.ScoreWeight, IsLead = e.IsLead, IsMandatory = e.IsMandatory,
        InviteStatusLookupValueId = e.InviteStatusLookupValueId, InviteSentAt = e.InviteSentAt,
        HasConflictOfInterest = e.HasConflictOfInterest, IsAvailabilityConfirmed = e.IsAvailabilityConfirmed,
        FeedbackDeadline = e.FeedbackDeadline, PanelSequence = e.PanelSequence,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class InterviewerAvailabilityService : IInterviewerAvailabilityService
{
    private readonly IInterviewerAvailabilityRepository _repo;
    private readonly ILogger<InterviewerAvailabilityService> _logger;

    public InterviewerAvailabilityService(IInterviewerAvailabilityRepository repo, ILogger<InterviewerAvailabilityService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<InterviewerAvailabilityDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviewer availabilities"); throw; }
    }

    public async Task<InterviewerAvailabilityDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviewer availability {Id}", id); throw; }
    }

    public async Task<IEnumerable<InterviewerAvailabilityDto>> GetByInterviewerIdAsync(Guid interviewerEmployeeId)
    {
        try { return (await _repo.GetByInterviewerIdAsync(interviewerEmployeeId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving availability for interviewer {Id}", interviewerEmployeeId); throw; }
    }

    public async Task<InterviewerAvailabilityDto> CreateAsync(CreateInterviewerAvailabilityDto request, Guid userId)
    {
        try
        {
            var entity = new InterviewerAvailability { InterviewerEmployeeId = request.InterviewerEmployeeId, AvailableDate = request.AvailableDate, StartTime = request.StartTime, EndTime = request.EndTime, SlotStatusLookupValueId = request.SlotStatusLookupValueId, TimeZone = request.TimeZone, Notes = request.Notes, IsRecurring = request.IsRecurring, RecurrencePattern = request.RecurrencePattern, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interviewer availability"); throw; }
    }

    public async Task<InterviewerAvailabilityDto> UpdateAsync(Guid id, UpdateInterviewerAvailabilityDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interviewer availability not found");
            if (request.AvailableDate.HasValue) entity.AvailableDate = request.AvailableDate.Value;
            if (request.StartTime.HasValue) entity.StartTime = request.StartTime.Value;
            if (request.EndTime.HasValue) entity.EndTime = request.EndTime.Value;
            if (request.SlotStatusLookupValueId.HasValue) entity.SlotStatusLookupValueId = request.SlotStatusLookupValueId.Value;
            if (request.InterviewId.HasValue) entity.InterviewId = request.InterviewId;
            if (request.TimeZone != null) entity.TimeZone = request.TimeZone;
            if (request.Notes != null) entity.Notes = request.Notes;
            if (request.IsBlocked.HasValue) entity.IsBlocked = request.IsBlocked.Value;
            if (request.ReasonCode != null) entity.ReasonCode = request.ReasonCode;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interviewer availability {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interviewer availability not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interviewer availability {Id}", id); throw; }
    }

    private static InterviewerAvailabilityDto Map(InterviewerAvailability e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, InterviewerEmployeeId = e.InterviewerEmployeeId,
        AvailableDate = e.AvailableDate, StartTime = e.StartTime, EndTime = e.EndTime,
        SlotStatusLookupValueId = e.SlotStatusLookupValueId, InterviewId = e.InterviewId,
        TimeZone = e.TimeZone, Notes = e.Notes, IsRecurring = e.IsRecurring,
        RecurrencePattern = e.RecurrencePattern, IsBlocked = e.IsBlocked, ReasonCode = e.ReasonCode,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class InterviewFeedbackService : IInterviewFeedbackService
{
    private readonly IInterviewFeedbackRepository _repo;
    private readonly ILogger<InterviewFeedbackService> _logger;

    public InterviewFeedbackService(IInterviewFeedbackRepository repo, ILogger<InterviewFeedbackService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<InterviewFeedbackDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview feedbacks"); throw; }
    }

    public async Task<InterviewFeedbackDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview feedback {Id}", id); throw; }
    }

    public async Task<IEnumerable<InterviewFeedbackDto>> GetByInterviewIdAsync(Guid interviewId)
    {
        try { return (await _repo.GetByInterviewIdAsync(interviewId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving feedback for interview {Id}", interviewId); throw; }
    }

    public async Task<InterviewFeedbackDto> CreateAsync(CreateInterviewFeedbackDto request, Guid userId)
    {
        try
        {
            var entity = new InterviewFeedback { InterviewId = request.InterviewId, PanelMemberId = request.PanelMemberId, CandidateId = request.CandidateId, ApplicationId = request.ApplicationId, SubmittedByEmployeeId = request.SubmittedByEmployeeId, OverallScore = request.OverallScore, RecommendationLookupValueId = request.RecommendationLookupValueId, TechnicalScore = request.TechnicalScore, BehavioralScore = request.BehavioralScore, CommunicationScore = request.CommunicationScore, CultureFitScore = request.CultureFitScore, StrengthNotes = request.StrengthNotes, ConcernNotes = request.ConcernNotes, Comments = request.Comments, CompetencyScoresJson = request.CompetencyScoresJson, HireReadiness = request.HireReadiness, WouldRehire = request.WouldRehire, IsSubmitted = false, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview feedback"); throw; }
    }

    public async Task<InterviewFeedbackDto> UpdateAsync(Guid id, UpdateInterviewFeedbackDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interview feedback not found");
            if (request.OverallScore.HasValue) entity.OverallScore = request.OverallScore;
            if (request.RecommendationLookupValueId.HasValue) entity.RecommendationLookupValueId = request.RecommendationLookupValueId;
            if (request.DecisionLookupValueId.HasValue) entity.DecisionLookupValueId = request.DecisionLookupValueId;
            if (request.TechnicalScore.HasValue) entity.TechnicalScore = request.TechnicalScore;
            if (request.BehavioralScore.HasValue) entity.BehavioralScore = request.BehavioralScore;
            if (request.CommunicationScore.HasValue) entity.CommunicationScore = request.CommunicationScore;
            if (request.CultureFitScore.HasValue) entity.CultureFitScore = request.CultureFitScore;
            if (request.StrengthNotes != null) entity.StrengthNotes = request.StrengthNotes;
            if (request.ConcernNotes != null) entity.ConcernNotes = request.ConcernNotes;
            if (request.Comments != null) entity.Comments = request.Comments;
            if (request.CompetencyScoresJson != null) entity.CompetencyScoresJson = request.CompetencyScoresJson;
            if (request.HireReadiness != null) entity.HireReadiness = request.HireReadiness;
            if (request.WouldRehire.HasValue) entity.WouldRehire = request.WouldRehire.Value;
            if (request.IsSubmitted.HasValue) entity.IsSubmitted = request.IsSubmitted.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview feedback {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Interview feedback not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview feedback {Id}", id); throw; }
    }

    private static InterviewFeedbackDto Map(InterviewFeedback e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, InterviewId = e.InterviewId, PanelMemberId = e.PanelMemberId,
        CandidateId = e.CandidateId, ApplicationId = e.ApplicationId,
        SubmittedByEmployeeId = e.SubmittedByEmployeeId, SubmittedAt = e.SubmittedAt,
        IsSubmitted = e.IsSubmitted, OverallScore = e.OverallScore,
        RecommendationLookupValueId = e.RecommendationLookupValueId,
        DecisionLookupValueId = e.DecisionLookupValueId, TechnicalScore = e.TechnicalScore,
        BehavioralScore = e.BehavioralScore, CommunicationScore = e.CommunicationScore,
        CultureFitScore = e.CultureFitScore, WeightedFinalScore = e.WeightedFinalScore,
        StrengthNotes = e.StrengthNotes, ConcernNotes = e.ConcernNotes, Comments = e.Comments,
        CompetencyScoresJson = e.CompetencyScoresJson, HireReadiness = e.HireReadiness,
        WouldRehire = e.WouldRehire, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
