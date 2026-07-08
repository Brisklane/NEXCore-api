using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class ApplicationDetailService : IApplicationDetailService
{
    private readonly IApplicationDetailRepository _repo;
    private readonly ILogger<ApplicationDetailService> _logger;

    public ApplicationDetailService(IApplicationDetailRepository repo, ILogger<ApplicationDetailService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<ApplicationDetailDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application details"); throw; }
    }

    public async Task<ApplicationDetailDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application detail {Id}", id); throw; }
    }

    public async Task<ApplicationDetailDto?> GetByApplicationIdAsync(Guid applicationId)
    {
        try { var e = await _repo.GetByApplicationIdAsync(applicationId); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application detail for application {Id}", applicationId); throw; }
    }

    public async Task<ApplicationDetailDto> CreateAsync(CreateApplicationDetailDto request, Guid userId)
    {
        try
        {
            var entity = new ApplicationDetail { ApplicationId = request.ApplicationId, CoverLetterUrl = request.CoverLetterUrl, MinQualificationsMet = request.MinQualificationsMet, OverallRating = request.OverallRating, ScreeningQuestionnaireId = request.ScreeningQuestionnaireId, AssignedHiringManagerEmployeeId = request.AssignedHiringManagerEmployeeId, RecruiterNotes = request.RecruiterNotes, NextFollowUpDate = request.NextFollowUpDate, ExpectedJoinDate = request.ExpectedJoinDate, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application detail"); throw; }
    }

    public async Task<ApplicationDetailDto> UpdateAsync(Guid id, UpdateApplicationDetailDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Application detail not found");
            if (request.CoverLetterUrl != null) entity.CoverLetterUrl = request.CoverLetterUrl;
            if (request.MinQualificationsMet != null) entity.MinQualificationsMet = request.MinQualificationsMet;
            if (request.OverallRating != null) entity.OverallRating = request.OverallRating;
            if (request.ResumeParseScore.HasValue) entity.ResumeParseScore = request.ResumeParseScore;
            if (request.ScreeningQuestionnaireScore.HasValue) entity.ScreeningQuestionnaireScore = request.ScreeningQuestionnaireScore;
            if (request.ScreeningQuestionnaireId.HasValue) entity.ScreeningQuestionnaireId = request.ScreeningQuestionnaireId;
            if (request.ScreeningStatusLookupValueId.HasValue) entity.ScreeningStatusLookupValueId = request.ScreeningStatusLookupValueId;
            if (request.ScreeningCompletedDate.HasValue) entity.ScreeningCompletedDate = request.ScreeningCompletedDate;
            if (request.AssignedHiringManagerEmployeeId.HasValue) entity.AssignedHiringManagerEmployeeId = request.AssignedHiringManagerEmployeeId;
            if (request.ReviewedByEmployeeId.HasValue) entity.ReviewedByEmployeeId = request.ReviewedByEmployeeId;
            if (request.ReviewedAt.HasValue) entity.ReviewedAt = request.ReviewedAt;
            if (request.RecruiterNotes != null) entity.RecruiterNotes = request.RecruiterNotes;
            if (request.NextFollowUpDate.HasValue) entity.NextFollowUpDate = request.NextFollowUpDate;
            if (request.ExpectedJoinDate.HasValue) entity.ExpectedJoinDate = request.ExpectedJoinDate;
            if (request.ActualJoinDate.HasValue) entity.ActualJoinDate = request.ActualJoinDate;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application detail {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Application detail not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application detail {Id}", id); throw; }
    }

    private static ApplicationDetailDto Map(ApplicationDetail e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, ApplicationId = e.ApplicationId,
        CoverLetterUrl = e.CoverLetterUrl, MinQualificationsMet = e.MinQualificationsMet,
        OverallRating = e.OverallRating, ResumeParseScore = e.ResumeParseScore,
        ScreeningQuestionnaireScore = e.ScreeningQuestionnaireScore,
        ScreeningQuestionnaireId = e.ScreeningQuestionnaireId,
        ScreeningStatusLookupValueId = e.ScreeningStatusLookupValueId,
        ScreeningCompletedDate = e.ScreeningCompletedDate,
        AssignedHiringManagerEmployeeId = e.AssignedHiringManagerEmployeeId,
        ReviewedByEmployeeId = e.ReviewedByEmployeeId, ReviewedAt = e.ReviewedAt,
        RecruiterNotes = e.RecruiterNotes, NextFollowUpDate = e.NextFollowUpDate,
        ExpectedJoinDate = e.ExpectedJoinDate, ActualJoinDate = e.ActualJoinDate,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class ApplicationComplianceService : IApplicationComplianceService
{
    private readonly IApplicationComplianceRepository _repo;
    private readonly ILogger<ApplicationComplianceService> _logger;

    public ApplicationComplianceService(IApplicationComplianceRepository repo, ILogger<ApplicationComplianceService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<ApplicationComplianceDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application compliances"); throw; }
    }

    public async Task<ApplicationComplianceDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application compliance {Id}", id); throw; }
    }

    public async Task<ApplicationComplianceDto?> GetByApplicationIdAsync(Guid applicationId)
    {
        try { var e = await _repo.GetByApplicationIdAsync(applicationId); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving compliance for application {Id}", applicationId); throw; }
    }

    public async Task<ApplicationComplianceDto> CreateAsync(CreateApplicationComplianceDto request, Guid userId)
    {
        try
        {
            var entity = new ApplicationCompliance { ApplicationId = request.ApplicationId, EEODataCaptured = request.EEODataCaptured, BackgroundCheckStatusLookupValueId = request.BackgroundCheckStatusLookupValueId, BackgroundCheckDate = request.BackgroundCheckDate, DrugTestStatusLookupValueId = request.DrugTestStatusLookupValueId, DrugTestDate = request.DrugTestDate, RightToWorkVerified = request.RightToWorkVerified, RightToWorkDocumentUrl = request.RightToWorkDocumentUrl, VisaSponsorshipRequired = request.VisaSponsorshipRequired, DataConsentGiven = request.DataConsentGiven, DataConsentDate = request.DataConsentDate, DataRetentionExpiryDate = request.DataRetentionExpiryDate, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application compliance"); throw; }
    }

    public async Task<ApplicationComplianceDto> UpdateAsync(Guid id, UpdateApplicationComplianceDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Application compliance not found");
            if (request.EEODataCaptured.HasValue) entity.EEODataCaptured = request.EEODataCaptured.Value;
            if (request.BackgroundCheckStatusLookupValueId.HasValue) entity.BackgroundCheckStatusLookupValueId = request.BackgroundCheckStatusLookupValueId;
            if (request.BackgroundCheckDate.HasValue) entity.BackgroundCheckDate = request.BackgroundCheckDate;
            if (request.DrugTestStatusLookupValueId.HasValue) entity.DrugTestStatusLookupValueId = request.DrugTestStatusLookupValueId;
            if (request.DrugTestDate.HasValue) entity.DrugTestDate = request.DrugTestDate;
            if (request.RightToWorkVerified.HasValue) entity.RightToWorkVerified = request.RightToWorkVerified.Value;
            if (request.RightToWorkDocumentUrl != null) entity.RightToWorkDocumentUrl = request.RightToWorkDocumentUrl;
            if (request.VisaSponsorshipRequired.HasValue) entity.VisaSponsorshipRequired = request.VisaSponsorshipRequired.Value;
            if (request.DataConsentGiven.HasValue) entity.DataConsentGiven = request.DataConsentGiven.Value;
            if (request.DataConsentDate.HasValue) entity.DataConsentDate = request.DataConsentDate;
            if (request.DataRetentionExpiryDate.HasValue) entity.DataRetentionExpiryDate = request.DataRetentionExpiryDate;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application compliance {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Application compliance not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application compliance {Id}", id); throw; }
    }

    private static ApplicationComplianceDto Map(ApplicationCompliance e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, ApplicationId = e.ApplicationId,
        EEODataCaptured = e.EEODataCaptured,
        BackgroundCheckStatusLookupValueId = e.BackgroundCheckStatusLookupValueId,
        BackgroundCheckDate = e.BackgroundCheckDate,
        DrugTestStatusLookupValueId = e.DrugTestStatusLookupValueId, DrugTestDate = e.DrugTestDate,
        RightToWorkVerified = e.RightToWorkVerified, RightToWorkDocumentUrl = e.RightToWorkDocumentUrl,
        VisaSponsorshipRequired = e.VisaSponsorshipRequired, DataConsentGiven = e.DataConsentGiven,
        DataConsentDate = e.DataConsentDate, DataRetentionExpiryDate = e.DataRetentionExpiryDate,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
