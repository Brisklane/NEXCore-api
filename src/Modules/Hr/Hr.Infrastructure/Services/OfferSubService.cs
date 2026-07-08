using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class OfferLetterDetailService : IOfferLetterDetailService
{
    private readonly IOfferLetterDetailRepository _repo;
    private readonly ILogger<OfferLetterDetailService> _logger;

    public OfferLetterDetailService(IOfferLetterDetailRepository repo, ILogger<OfferLetterDetailService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<OfferLetterDetailDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letter details"); throw; }
    }

    public async Task<OfferLetterDetailDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer letter detail {Id}", id); throw; }
    }

    public async Task<OfferLetterDetailDto?> GetByOfferLetterIdAsync(Guid offerLetterId)
    {
        try { var e = await _repo.GetByOfferLetterIdAsync(offerLetterId); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving detail for offer letter {Id}", offerLetterId); throw; }
    }

    public async Task<OfferLetterDetailDto> CreateAsync(CreateOfferLetterDetailDto request, Guid userId)
    {
        try
        {
            var entity = new OfferLetterDetail { OfferLetterId = request.OfferLetterId, AllowancesBreakdown = request.AllowancesBreakdown, HousingAllowance = request.HousingAllowance, TransportAllowance = request.TransportAllowance, MedicalAllowance = request.MedicalAllowance, OtherAllowances = request.OtherAllowances, BonusTarget = request.BonusTarget, BonusPercent = request.BonusPercent, EquityGrant = request.EquityGrant, GradeId = request.GradeId, PayScaleId = request.PayScaleId, BenefitsPlanId = request.BenefitsPlanId, AllowancesProfileId = request.AllowancesProfileId, BenefitsSummary = request.BenefitsSummary, TermsDocumentUrl = request.TermsDocumentUrl, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating offer letter detail"); throw; }
    }

    public async Task<OfferLetterDetailDto> UpdateAsync(Guid id, UpdateOfferLetterDetailDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Offer letter detail not found");
            if (request.AllowancesBreakdown != null) entity.AllowancesBreakdown = request.AllowancesBreakdown;
            if (request.HousingAllowance.HasValue) entity.HousingAllowance = request.HousingAllowance;
            if (request.TransportAllowance.HasValue) entity.TransportAllowance = request.TransportAllowance;
            if (request.MedicalAllowance.HasValue) entity.MedicalAllowance = request.MedicalAllowance;
            if (request.OtherAllowances.HasValue) entity.OtherAllowances = request.OtherAllowances;
            if (request.BonusTarget.HasValue) entity.BonusTarget = request.BonusTarget;
            if (request.BonusPercent.HasValue) entity.BonusPercent = request.BonusPercent;
            if (request.EquityGrant != null) entity.EquityGrant = request.EquityGrant;
            if (request.GradeId.HasValue) entity.GradeId = request.GradeId;
            if (request.PayScaleId.HasValue) entity.PayScaleId = request.PayScaleId;
            if (request.BenefitsPlanId.HasValue) entity.BenefitsPlanId = request.BenefitsPlanId;
            if (request.AllowancesProfileId.HasValue) entity.AllowancesProfileId = request.AllowancesProfileId;
            if (request.BenefitsSummary != null) entity.BenefitsSummary = request.BenefitsSummary;
            if (request.TermsDocumentUrl != null) entity.TermsDocumentUrl = request.TermsDocumentUrl;
            if (request.CounterSignedByEmployeeId.HasValue) entity.CounterSignedByEmployeeId = request.CounterSignedByEmployeeId;
            if (request.CounterSignedAt.HasValue) entity.CounterSignedAt = request.CounterSignedAt;
            if (request.IsDigitallySigned.HasValue) entity.IsDigitallySigned = request.IsDigitallySigned.Value;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating offer letter detail {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Offer letter detail not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting offer letter detail {Id}", id); throw; }
    }

    private static OfferLetterDetailDto Map(OfferLetterDetail e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, OfferLetterId = e.OfferLetterId,
        AllowancesBreakdown = e.AllowancesBreakdown, HousingAllowance = e.HousingAllowance,
        TransportAllowance = e.TransportAllowance, MedicalAllowance = e.MedicalAllowance,
        OtherAllowances = e.OtherAllowances, BonusTarget = e.BonusTarget, BonusPercent = e.BonusPercent,
        EquityGrant = e.EquityGrant, GradeId = e.GradeId, PayScaleId = e.PayScaleId,
        BenefitsPlanId = e.BenefitsPlanId, AllowancesProfileId = e.AllowancesProfileId,
        BenefitsSummary = e.BenefitsSummary, TermsDocumentUrl = e.TermsDocumentUrl,
        CounterSignedByEmployeeId = e.CounterSignedByEmployeeId, CounterSignedAt = e.CounterSignedAt,
        IsDigitallySigned = e.IsDigitallySigned, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class OfferNegotiationService : IOfferNegotiationService
{
    private readonly IOfferNegotiationRepository _repo;
    private readonly ILogger<OfferNegotiationService> _logger;

    public OfferNegotiationService(IOfferNegotiationRepository repo, ILogger<OfferNegotiationService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<OfferNegotiationDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer negotiations"); throw; }
    }

    public async Task<OfferNegotiationDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving offer negotiation {Id}", id); throw; }
    }

    public async Task<IEnumerable<OfferNegotiationDto>> GetByOfferIdAsync(Guid offerId)
    {
        try { return (await _repo.GetByOfferIdAsync(offerId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving negotiations for offer {Id}", offerId); throw; }
    }

    public async Task<OfferNegotiationDto> CreateAsync(CreateOfferNegotiationDto request, Guid userId)
    {
        try
        {
            var entity = new OfferNegotiation { OfferId = request.OfferId, NegotiationRound = request.NegotiationRound, InitiatedBy = request.InitiatedBy, ProposedSalary = request.ProposedSalary, ProposedTerms = request.ProposedTerms, CounterOfferNotes = request.CounterOfferNotes, StatusLookupValueId = request.StatusLookupValueId, PreviousSalary = request.PreviousSalary, HRRecommendation = request.HRRecommendation, RequiresReApproval = request.RequiresReApproval, Deadline = request.Deadline, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating offer negotiation"); throw; }
    }

    public async Task<OfferNegotiationDto> UpdateAsync(Guid id, UpdateOfferNegotiationDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Offer negotiation not found");
            if (request.ProposedTerms != null) entity.ProposedTerms = request.ProposedTerms;
            if (request.CounterOfferNotes != null) entity.CounterOfferNotes = request.CounterOfferNotes;
            if (request.StatusLookupValueId.HasValue) entity.StatusLookupValueId = request.StatusLookupValueId.Value;
            if (request.ResponseByEmployeeId.HasValue) entity.ResponseByEmployeeId = request.ResponseByEmployeeId;
            if (request.HRRecommendation != null) entity.HRRecommendation = request.HRRecommendation;
            if (request.RequiresReApproval.HasValue) entity.RequiresReApproval = request.RequiresReApproval.Value;
            if (request.RespondedAt.HasValue) entity.RespondedAt = request.RespondedAt;
            if (request.Deadline.HasValue) entity.Deadline = request.Deadline;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating offer negotiation {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Offer negotiation not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting offer negotiation {Id}", id); throw; }
    }

    private static OfferNegotiationDto Map(OfferNegotiation e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, OfferId = e.OfferId,
        NegotiationRound = e.NegotiationRound, InitiatedBy = e.InitiatedBy, NegotiationDate = e.NegotiationDate,
        ProposedSalary = e.ProposedSalary, ProposedTerms = e.ProposedTerms,
        CounterOfferNotes = e.CounterOfferNotes, StatusLookupValueId = e.StatusLookupValueId,
        ResponseByEmployeeId = e.ResponseByEmployeeId, PreviousSalary = e.PreviousSalary,
        HRRecommendation = e.HRRecommendation, RequiresReApproval = e.RequiresReApproval,
        RespondedAt = e.RespondedAt, Deadline = e.Deadline, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
