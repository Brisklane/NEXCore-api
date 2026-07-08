using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Persistence;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class OfferLetterService : IOfferLetterService
{
    private readonly IOfferLetterRepository _repository;
    private readonly HrDbContext _context;
    private readonly ILogger<OfferLetterService> _logger;

    public OfferLetterService(
        IOfferLetterRepository repository,
        HrDbContext context,
        ILogger<OfferLetterService> logger)
    {
        _repository = repository;
        _context    = context;
        _logger     = logger;
    }

    public async Task<IEnumerable<OfferLetterDto>> GetAllAsync()
    {
        try
        {
            var offers = await _repository.GetAllByTenantAsync();
            return offers.Where(o => !o.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letters"); throw; }
    }

    public async Task<OfferLetterDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var offer = await _repository.GetByIdAsync(id);
            return offer == null ? null : MapToDto(offer);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letter: {OfferId}", id); throw; }
    }

    public async Task<IEnumerable<OfferLetterDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        try
        {
            var offers = await _repository.GetByApplicationIdAsync(applicationId);
            return offers.Where(o => !o.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letters by application"); throw; }
    }

    public async Task<OfferLetterDto> CreateAsync(CreateOfferLetterDto request, Guid userId)
    {
        try
        {
            // Derive currency, title, employment type, and reporting manager from the job
            var job = await _context.Jobs
                .Include(j => j.Detail)
                .FirstOrDefaultAsync(j => j.Id == request.JobId && !j.IsDeleted)
                ?? throw new InvalidOperationException($"Job '{request.JobId}' not found.");

            // Resolve default candidate response (No Response) — looked up by code
            var candRespNone = await _context.LookupValues
                .FirstOrDefaultAsync(v => v.Code == "CAND_RESP_NONE" && !v.IsDeleted);

            var offerCode = !string.IsNullOrWhiteSpace(request.OfferCode)
                ? request.OfferCode
                : $"OFR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

            var reportingManagerId = job.Detail?.ReportingManagerEmployeeId
                ?? job.HiringManagerEmployeeId
                ?? request.GeneratedByEmployeeId
                ?? userId;

            var offer = new OfferLetter
            {
                OfferCode                      = offerCode,
                ApplicationId                  = request.ApplicationId,
                CandidateId                    = request.CandidateId,
                JobId                          = request.JobId,
                JobTitle                       = job.JobTitle,
                ReportingManagerEmployeeId     = reportingManagerId,
                BaseSalary                     = request.BaseSalary,
                TotalPackage                   = request.TotalPackage > 0 ? request.TotalPackage : request.BaseSalary,
                CurrencyId                     = job.CurrencyId,
                EmploymentType                 = job.EmploymentType,
                StartDate                      = request.IssuedDate ?? DateTime.UtcNow,
                ExpiryDate                     = request.ExpiryDate ?? DateTime.UtcNow.AddDays(14),
                ProbationPeriodMonths          = request.ProbationPeriodMonths,
                NoticePeriodDays               = request.NoticePeriodDays,
                StatusLookupValueId            = request.StatusLookupValueId,
                CandidateResponseLookupValueId = candRespNone?.Id ?? Guid.Empty,
                VersionNumber                  = 1,
                SentByEmployeeId               = request.GeneratedByEmployeeId,
                CreatedAt                      = DateTime.UtcNow,
                CreatedByUserId                = userId
            };

            await _repository.AddAsync(offer);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Offer letter created: {OfferCode} (ID: {OfferId})", offer.OfferCode, offer.Id);
            return MapToDto(offer);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating offer letter"); throw; }
    }

    public async Task<OfferLetterDto> UpdateAsync(Guid id, UpdateOfferLetterDto request, Guid userId)
    {
        try
        {
            var offer = await _repository.GetByIdAsync(id);
            if (offer == null || offer.IsDeleted)
                throw new InvalidOperationException("Offer letter not found");

            if (request.StatusLookupValueId.HasValue)  offer.StatusLookupValueId  = request.StatusLookupValueId.Value;
            if (request.BaseSalary.HasValue)            offer.BaseSalary            = request.BaseSalary.Value;
            if (request.TotalPackage.HasValue)          offer.TotalPackage          = request.TotalPackage.Value;
            if (request.IssuedDate.HasValue)            offer.StartDate             = request.IssuedDate.Value;
            if (request.ExpiryDate.HasValue)            offer.ExpiryDate            = request.ExpiryDate.Value;
            if (request.GeneratedByEmployeeId.HasValue) offer.SentByEmployeeId      = request.GeneratedByEmployeeId;
            if (request.ProbationPeriodMonths.HasValue) offer.ProbationPeriodMonths = request.ProbationPeriodMonths.Value;
            if (request.NoticePeriodDays.HasValue)      offer.NoticePeriodDays      = request.NoticePeriodDays.Value;
            if (request.RevokeReason != null)           offer.RevokeReason          = request.RevokeReason;

            offer.UpdatedAt       = DateTime.UtcNow;
            offer.UpdatedByUserId = userId;

            _repository.Update(offer);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Offer letter updated: {OfferId}", id);
            return MapToDto(offer);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating offer letter: {OfferId}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var offer = await _repository.GetByIdAsync(id)
                ?? throw new InvalidOperationException("Offer letter not found");

            _context.OfferLetters.Remove(offer);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Offer letter hard-deleted: {OfferId}", id);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting offer letter: {OfferId}", id); throw; }
    }

    private static OfferLetterDto MapToDto(OfferLetter o) => new()
    {
        Id = o.Id,
        CompanyId = o.CompanyId,
        OfferCode = o.OfferCode,
        ApplicationId = o.ApplicationId,
        CandidateId = o.CandidateId,
        JobId = o.JobId,
        JobTitle = o.JobTitle,
        ReportingManagerEmployeeId = o.ReportingManagerEmployeeId,
        BaseSalary = o.BaseSalary,
        TotalPackage = o.TotalPackage,
        CurrencyId = o.CurrencyId,
        EmploymentType = o.EmploymentType,
        StartDate = o.StartDate,
        ExpiryDate = o.ExpiryDate,
        ProbationPeriodMonths = o.ProbationPeriodMonths,
        NoticePeriodDays = o.NoticePeriodDays,
        StatusLookupValueId = o.StatusLookupValueId,
        CandidateResponseLookupValueId = o.CandidateResponseLookupValueId,
        VersionNumber = o.VersionNumber,
        ApprovalRequestId = o.ApprovalRequestId,
        SentDate = o.SentDate,
        SentByEmployeeId = o.SentByEmployeeId,
        SentVia = o.SentVia,
        ResponseDate = o.ResponseDate,
        RevokedDate = o.RevokedDate,
        RevokedByEmployeeId = o.RevokedByEmployeeId,
        RevokeReason = o.RevokeReason,
        CommunicationTemplateId = o.CommunicationTemplateId,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt
    };
}
