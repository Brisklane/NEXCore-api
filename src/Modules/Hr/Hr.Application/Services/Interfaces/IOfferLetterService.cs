using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IOfferLetterService
{
    Task<IEnumerable<OfferLetterDto>> GetAllAsync();
    Task<OfferLetterDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<OfferLetterDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<OfferLetterDto> CreateAsync(CreateOfferLetterDto request, Guid userId);
    Task<OfferLetterDto> UpdateAsync(Guid id, UpdateOfferLetterDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
