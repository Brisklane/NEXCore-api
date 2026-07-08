using Hr.Application.DTOs;
using Hr.Application.Enums;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CandidateService : ICandidateService
{
    private readonly ICandidateRepository _repository;
    private readonly ILogger<CandidateService> _logger;

    public CandidateService(ICandidateRepository repository, ILogger<CandidateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<CandidateDto>> GetAllAsync()
    {
        try
        {
            var candidates = await _repository.GetAllByTenantAsync();
            return candidates.Where(c => !c.IsDeleted).Select(MapToDto);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidates"); throw; }
    }

    public async Task<CandidateDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var candidate = await _repository.GetByIdAsync(id);
            return candidate == null ? null : MapToDto(candidate);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate: {CandidateId}", id); throw; }
    }

    public async Task<CandidateDto> CreateAsync(CreateCandidateDto request, Guid userId)
    {
        try
        {
            var candidate = new Candidate
            {
                CandidateCode = request.CandidateCode,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                Phone = request.Phone,
                ResumeUrl = request.ResumeUrl,
                Source = request.Source?.ToString(),
                CurrentCompany = request.CurrentCompany,
                CurrentDesignationId = request.CurrentDesignationId,
                TotalExperienceYears = request.TotalExperienceYears,
                CurrentSalary = request.CurrentSalary,
                ExpectedSalary = request.ExpectedSalary,
                ConsentGiven = request.ConsentGiven,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(candidate);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Candidate created: {FullName} (ID: {CandidateId})", $"{candidate.FirstName} {candidate.LastName}", candidate.Id);
            return MapToDto(candidate);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate"); throw; }
    }

    public async Task<CandidateDto> UpdateAsync(Guid id, UpdateCandidateDto request, Guid userId)
    {
        try
        {
            var candidate = await _repository.GetByIdAsync(id);
            if (candidate == null || candidate.IsDeleted)
                throw new InvalidOperationException("Candidate not found");

            if (!string.IsNullOrWhiteSpace(request.FirstName)) candidate.FirstName = request.FirstName;
            if (!string.IsNullOrWhiteSpace(request.LastName)) candidate.LastName = request.LastName;
            if (request.Phone != null) candidate.Phone = request.Phone;
            if (request.ResumeUrl != null) candidate.ResumeUrl = request.ResumeUrl;
            if (request.Source.HasValue) candidate.Source = request.Source.Value.ToString();
            if (request.CurrentCompany != null) candidate.CurrentCompany = request.CurrentCompany;
            if (request.CurrentDesignationId.HasValue) candidate.CurrentDesignationId = request.CurrentDesignationId;
            if (request.TotalExperienceYears.HasValue) candidate.TotalExperienceYears = request.TotalExperienceYears;
            if (request.CurrentSalary.HasValue) candidate.CurrentSalary = request.CurrentSalary;
            if (request.ExpectedSalary.HasValue) candidate.ExpectedSalary = request.ExpectedSalary;
            if (request.CandidateRating.HasValue) candidate.CandidateRating = request.CandidateRating.Value.ToString();
            if (request.IsBlacklisted.HasValue)
            {
                candidate.IsBlacklisted = request.IsBlacklisted.Value;
                candidate.BlacklistedAt = request.IsBlacklisted.Value ? (candidate.BlacklistedAt ?? DateTime.UtcNow) : null;
            }
            if (request.BlacklistReasonLookupValueId.HasValue) candidate.BlacklistReasonLookupValueId = request.BlacklistReasonLookupValueId;
            if (request.ConsentGiven.HasValue) candidate.ConsentGiven = request.ConsentGiven.Value;

            candidate.UpdatedAt = DateTime.UtcNow;
            candidate.UpdatedByUserId = userId;

            _repository.Update(candidate);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Candidate updated: {CandidateId}", id);
            return MapToDto(candidate);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate: {CandidateId}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var candidate = await _repository.GetByIdAsync(id);
            if (candidate == null || candidate.IsDeleted)
                throw new InvalidOperationException("Candidate not found");

            candidate.IsDeleted = true;
            candidate.DeletedAt = DateTime.UtcNow;
            candidate.DeletedByUserId = userId;

            _repository.Update(candidate);
            await _repository.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate: {CandidateId}", id); throw; }
    }

    private static CandidateDto MapToDto(Candidate c) => new()
    {
        Id = c.Id,
        CompanyId = c.CompanyId,
        CandidateCode = c.CandidateCode,
        FirstName = c.FirstName,
        LastName = c.LastName,
        Email = c.Email,
        Phone = c.Phone,
        ResumeUrl = c.ResumeUrl,
        Source = Enum.TryParse<CandidateSource>(c.Source, out var source) ? source : null,
        CurrentCompany = c.CurrentCompany,
        CurrentDesignationId = c.CurrentDesignationId,
        TotalExperienceYears = c.TotalExperienceYears,
        CurrentSalary = c.CurrentSalary,
        ExpectedSalary = c.ExpectedSalary,
        CandidateRating = Enum.TryParse<CandidateRating>(c.CandidateRating, out var rating) ? rating : null,
        IsBlacklisted = c.IsBlacklisted,
        BlacklistReasonLookupValueId = c.BlacklistReasonLookupValueId,
        BlacklistedAt = c.BlacklistedAt,
        BlacklistedByEmployeeId = c.BlacklistedByEmployeeId,
        ConsentGiven = c.ConsentGiven,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
