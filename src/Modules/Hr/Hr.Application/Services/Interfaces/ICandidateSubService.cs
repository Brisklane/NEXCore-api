using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface ICandidateProfileService
{
    Task<IEnumerable<CandidateProfileDto>> GetAllAsync();
    Task<CandidateProfileDto?> GetByIdAsync(Guid id);
    Task<CandidateProfileDto?> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateProfileDto> CreateAsync(CreateCandidateProfileDto request, Guid userId);
    Task<CandidateProfileDto> UpdateAsync(Guid id, UpdateCandidateProfileDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICandidateContactService
{
    Task<IEnumerable<CandidateContactDto>> GetAllAsync();
    Task<CandidateContactDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateContactDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateContactDto> CreateAsync(CreateCandidateContactDto request, Guid userId);
    Task<CandidateContactDto> UpdateAsync(Guid id, UpdateCandidateContactDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICandidateAddressService
{
    Task<IEnumerable<CandidateAddressDto>> GetAllAsync();
    Task<CandidateAddressDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateAddressDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateAddressDto> CreateAsync(CreateCandidateAddressDto request, Guid userId);
    Task<CandidateAddressDto> UpdateAsync(Guid id, UpdateCandidateAddressDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICandidateMediaLinkService
{
    Task<IEnumerable<CandidateMediaLinkDto>> GetAllAsync();
    Task<CandidateMediaLinkDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateMediaLinkDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateMediaLinkDto> CreateAsync(CreateCandidateMediaLinkDto request, Guid userId);
    Task<CandidateMediaLinkDto> UpdateAsync(Guid id, UpdateCandidateMediaLinkDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface ICandidateSkillService
{
    Task<IEnumerable<CandidateSkillDto>> GetAllAsync();
    Task<CandidateSkillDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CandidateSkillDto>> GetByCandidateIdAsync(Guid candidateId);
    Task<CandidateSkillDto> CreateAsync(CreateCandidateSkillDto request, Guid userId);
    Task<CandidateSkillDto> UpdateAsync(Guid id, UpdateCandidateSkillDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
