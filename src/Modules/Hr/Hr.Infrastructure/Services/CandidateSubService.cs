using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;
using Hr.Domain.Entities;
using Hr.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hr.Infrastructure.Services;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly ICandidateProfileRepository _repo;
    private readonly ILogger<CandidateProfileService> _logger;

    public CandidateProfileService(ICandidateProfileRepository repo, ILogger<CandidateProfileService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateProfileDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate profiles"); throw; }
    }

    public async Task<CandidateProfileDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate profile {Id}", id); throw; }
    }

    public async Task<CandidateProfileDto?> GetByCandidateIdAsync(Guid candidateId)
    {
        try { var e = await _repo.GetByCandidateIdAsync(candidateId); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate profile for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateProfileDto> CreateAsync(CreateCandidateProfileDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateProfile
            {
                CandidateId = request.CandidateId, MiddleName = request.MiddleName,
                AlternateEmail = request.AlternateEmail, MobilePhone = request.MobilePhone,
                CurrentCompany = request.CurrentCompany, CurrentDesignation = request.CurrentDesignation,
                ReferredBy = request.ReferredBy, DateOfBirth = request.DateOfBirth,
                Gender = request.Gender, Nationality = request.Nationality,
                CountryOfResidence = request.CountryOfResidence, City = request.City,
                CurrentNoticePeriodDays = request.CurrentNoticePeriodDays,
                AvailableFromDate = request.AvailableFromDate,
                PreferredWorkLocation = request.PreferredWorkLocation,
                RemotePreference = request.RemotePreference,
                HighestEducationLevel = request.HighestEducationLevel,
                HighestEducationField = request.HighestEducationField,
                TalentPoolId = request.TalentPoolId,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            _logger.LogInformation("CandidateProfile created for candidate {CandidateId}", entity.CandidateId);
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate profile"); throw; }
    }

    public async Task<CandidateProfileDto> UpdateAsync(Guid id, UpdateCandidateProfileDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate profile not found");
            if (request.MiddleName != null) entity.MiddleName = request.MiddleName;
            if (request.AlternateEmail != null) entity.AlternateEmail = request.AlternateEmail;
            if (request.MobilePhone != null) entity.MobilePhone = request.MobilePhone;
            if (request.CurrentCompany != null) entity.CurrentCompany = request.CurrentCompany;
            if (request.CurrentDesignation != null) entity.CurrentDesignation = request.CurrentDesignation;
            if (request.ReferredBy != null) entity.ReferredBy = request.ReferredBy;
            if (request.DateOfBirth.HasValue) entity.DateOfBirth = request.DateOfBirth;
            if (request.Gender != null) entity.Gender = request.Gender;
            if (request.Nationality != null) entity.Nationality = request.Nationality;
            if (request.CountryOfResidence != null) entity.CountryOfResidence = request.CountryOfResidence;
            if (request.City != null) entity.City = request.City;
            if (request.CurrentNoticePeriodDays.HasValue) entity.CurrentNoticePeriodDays = request.CurrentNoticePeriodDays;
            if (request.AvailableFromDate.HasValue) entity.AvailableFromDate = request.AvailableFromDate;
            if (request.PreferredWorkLocation != null) entity.PreferredWorkLocation = request.PreferredWorkLocation;
            if (request.RemotePreference != null) entity.RemotePreference = request.RemotePreference;
            if (request.HighestEducationLevel != null) entity.HighestEducationLevel = request.HighestEducationLevel;
            if (request.HighestEducationField != null) entity.HighestEducationField = request.HighestEducationField;
            if (request.TalentPoolId.HasValue) entity.TalentPoolId = request.TalentPoolId;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate profile {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate profile not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate profile {Id}", id); throw; }
    }

    private static CandidateProfileDto Map(CandidateProfile e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId,
        MiddleName = e.MiddleName, AlternateEmail = e.AlternateEmail, MobilePhone = e.MobilePhone,
        CurrentCompany = e.CurrentCompany, CurrentDesignation = e.CurrentDesignation,
        ReferredBy = e.ReferredBy, DateOfBirth = e.DateOfBirth, Gender = e.Gender,
        Nationality = e.Nationality, CountryOfResidence = e.CountryOfResidence, City = e.City,
        CurrentNoticePeriodDays = e.CurrentNoticePeriodDays, AvailableFromDate = e.AvailableFromDate,
        PreferredWorkLocation = e.PreferredWorkLocation, RemotePreference = e.RemotePreference,
        HighestEducationLevel = e.HighestEducationLevel, HighestEducationField = e.HighestEducationField,
        PortalRegistered = e.PortalRegistered, ProfileCompletionPercent = e.ProfileCompletionPercent,
        TalentPoolId = e.TalentPoolId, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CandidateContactService : ICandidateContactService
{
    private readonly ICandidateContactRepository _repo;
    private readonly ILogger<CandidateContactService> _logger;

    public CandidateContactService(ICandidateContactRepository repo, ILogger<CandidateContactService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateContactDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate contacts"); throw; }
    }

    public async Task<CandidateContactDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate contact {Id}", id); throw; }
    }

    public async Task<IEnumerable<CandidateContactDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contacts for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateContactDto> CreateAsync(CreateCandidateContactDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateContact { CandidateId = request.CandidateId, ContactTypeLookupValueId = request.ContactTypeLookupValueId, ContactValue = request.ContactValue, IsPrimary = request.IsPrimary, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate contact"); throw; }
    }

    public async Task<CandidateContactDto> UpdateAsync(Guid id, UpdateCandidateContactDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate contact not found");
            if (request.ContactTypeLookupValueId.HasValue) entity.ContactTypeLookupValueId = request.ContactTypeLookupValueId.Value;
            if (request.ContactValue != null) entity.ContactValue = request.ContactValue;
            if (request.IsPrimary.HasValue) entity.IsPrimary = request.IsPrimary.Value;
            if (request.IsVerified.HasValue) entity.IsVerified = request.IsVerified.Value;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate contact {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate contact not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate contact {Id}", id); throw; }
    }

    private static CandidateContactDto Map(CandidateContact e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId,
        ContactTypeLookupValueId = e.ContactTypeLookupValueId, ContactValue = e.ContactValue,
        IsPrimary = e.IsPrimary, IsVerified = e.IsVerified, VerifiedAt = e.VerifiedAt,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CandidateAddressService : ICandidateAddressService
{
    private readonly ICandidateAddressRepository _repo;
    private readonly ILogger<CandidateAddressService> _logger;

    public CandidateAddressService(ICandidateAddressRepository repo, ILogger<CandidateAddressService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateAddressDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate addresses"); throw; }
    }

    public async Task<CandidateAddressDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate address {Id}", id); throw; }
    }

    public async Task<IEnumerable<CandidateAddressDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving addresses for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateAddressDto> CreateAsync(CreateCandidateAddressDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateAddress { CandidateId = request.CandidateId, AddressTypeLookupValueId = request.AddressTypeLookupValueId, Line1 = request.Line1, Line2 = request.Line2, City = request.City, StateProvince = request.StateProvince, PostalCode = request.PostalCode, Country = request.Country, Latitude = request.Latitude, Longitude = request.Longitude, IsPrimary = request.IsPrimary, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate address"); throw; }
    }

    public async Task<CandidateAddressDto> UpdateAsync(Guid id, UpdateCandidateAddressDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate address not found");
            if (request.AddressTypeLookupValueId.HasValue) entity.AddressTypeLookupValueId = request.AddressTypeLookupValueId.Value;
            if (request.Line1 != null) entity.Line1 = request.Line1;
            if (request.Line2 != null) entity.Line2 = request.Line2;
            if (request.City != null) entity.City = request.City;
            if (request.StateProvince != null) entity.StateProvince = request.StateProvince;
            if (request.PostalCode != null) entity.PostalCode = request.PostalCode;
            if (request.Country != null) entity.Country = request.Country;
            if (request.Latitude.HasValue) entity.Latitude = request.Latitude;
            if (request.Longitude.HasValue) entity.Longitude = request.Longitude;
            if (request.IsPrimary.HasValue) entity.IsPrimary = request.IsPrimary.Value;
            if (request.IsVerified.HasValue) entity.IsVerified = request.IsVerified.Value;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate address {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate address not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate address {Id}", id); throw; }
    }

    private static CandidateAddressDto Map(CandidateAddress e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId,
        AddressTypeLookupValueId = e.AddressTypeLookupValueId, Line1 = e.Line1, Line2 = e.Line2,
        City = e.City, StateProvince = e.StateProvince, PostalCode = e.PostalCode, Country = e.Country,
        Latitude = e.Latitude, Longitude = e.Longitude, IsPrimary = e.IsPrimary, IsVerified = e.IsVerified,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CandidateMediaLinkService : ICandidateMediaLinkService
{
    private readonly ICandidateMediaLinkRepository _repo;
    private readonly ILogger<CandidateMediaLinkService> _logger;

    public CandidateMediaLinkService(ICandidateMediaLinkRepository repo, ILogger<CandidateMediaLinkService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateMediaLinkDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate media links"); throw; }
    }

    public async Task<CandidateMediaLinkDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate media link {Id}", id); throw; }
    }

    public async Task<IEnumerable<CandidateMediaLinkDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving media links for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateMediaLinkDto> CreateAsync(CreateCandidateMediaLinkDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateMediaLink { CandidateId = request.CandidateId, MediaTypeLookupValueId = request.MediaTypeLookupValueId, Url = request.Url, Title = request.Title, IsPrimary = request.IsPrimary, SortOrder = request.SortOrder, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate media link"); throw; }
    }

    public async Task<CandidateMediaLinkDto> UpdateAsync(Guid id, UpdateCandidateMediaLinkDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate media link not found");
            if (request.MediaTypeLookupValueId.HasValue) entity.MediaTypeLookupValueId = request.MediaTypeLookupValueId.Value;
            if (request.Url != null) entity.Url = request.Url;
            if (request.Title != null) entity.Title = request.Title;
            if (request.IsPrimary.HasValue) entity.IsPrimary = request.IsPrimary.Value;
            if (request.SortOrder.HasValue) entity.SortOrder = request.SortOrder.Value;
            if (request.IsVerified.HasValue) entity.IsVerified = request.IsVerified.Value;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate media link {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate media link not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate media link {Id}", id); throw; }
    }

    private static CandidateMediaLinkDto Map(CandidateMediaLink e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId,
        MediaTypeLookupValueId = e.MediaTypeLookupValueId, Url = e.Url, Title = e.Title,
        IsPrimary = e.IsPrimary, SortOrder = e.SortOrder, IsVerified = e.IsVerified,
        Notes = e.Notes, CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}

public class CandidateSkillService : ICandidateSkillService
{
    private readonly ICandidateSkillRepository _repo;
    private readonly ILogger<CandidateSkillService> _logger;

    public CandidateSkillService(ICandidateSkillRepository repo, ILogger<CandidateSkillService> logger)
    { _repo = repo; _logger = logger; }

    public async Task<IEnumerable<CandidateSkillDto>> GetAllAsync()
    {
        try { return (await _repo.GetAllByTenantAsync()).Where(e => !e.IsDeleted).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate skills"); throw; }
    }

    public async Task<CandidateSkillDto?> GetByIdAsync(Guid id)
    {
        try { var e = await _repo.GetByIdAsync(id); return e is null ? null : Map(e); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate skill {Id}", id); throw; }
    }

    public async Task<IEnumerable<CandidateSkillDto>> GetByCandidateIdAsync(Guid candidateId)
    {
        try { return (await _repo.GetByCandidateIdAsync(candidateId)).Select(Map); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills for candidate {Id}", candidateId); throw; }
    }

    public async Task<CandidateSkillDto> CreateAsync(CreateCandidateSkillDto request, Guid userId)
    {
        try
        {
            var entity = new CandidateSkill { CandidateId = request.CandidateId, SkillId = request.SkillId, ProficiencyLookupValueId = request.ProficiencyLookupValueId, YearsExperience = request.YearsExperience, Source = request.Source, Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
            await _repo.AddAsync(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate skill"); throw; }
    }

    public async Task<CandidateSkillDto> UpdateAsync(Guid id, UpdateCandidateSkillDto request, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate skill not found");
            if (request.SkillId.HasValue) entity.SkillId = request.SkillId.Value;
            if (request.ProficiencyLookupValueId.HasValue) entity.ProficiencyLookupValueId = request.ProficiencyLookupValueId.Value;
            if (request.YearsExperience.HasValue) entity.YearsExperience = request.YearsExperience;
            if (request.IsVerified.HasValue) entity.IsVerified = request.IsVerified.Value;
            if (request.VerifiedByEmployeeId.HasValue) entity.VerifiedByEmployeeId = request.VerifiedByEmployeeId;
            if (request.Source != null) entity.Source = request.Source;
            if (request.Notes != null) entity.Notes = request.Notes;
            entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
            return Map(entity);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate skill {Id}", id); throw; }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var entity = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Candidate skill not found");
            entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
            _repo.Update(entity); await _repo.SaveChangesAsync();
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate skill {Id}", id); throw; }
    }

    private static CandidateSkillDto Map(CandidateSkill e) => new()
    {
        Id = e.Id, CompanyId = e.CompanyId, CandidateId = e.CandidateId, SkillId = e.SkillId,
        ProficiencyLookupValueId = e.ProficiencyLookupValueId, YearsExperience = e.YearsExperience,
        IsVerified = e.IsVerified, VerifiedByEmployeeId = e.VerifiedByEmployeeId,
        VerifiedAt = e.VerifiedAt, Source = e.Source, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
