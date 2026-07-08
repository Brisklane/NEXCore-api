using Hr.Application.DTOs;

namespace Hr.Application.Services.Interfaces;

public interface IOfferLetterDetailService
{
    Task<IEnumerable<OfferLetterDetailDto>> GetAllAsync();
    Task<OfferLetterDetailDto?> GetByIdAsync(Guid id);
    Task<OfferLetterDetailDto?> GetByOfferLetterIdAsync(Guid offerLetterId);
    Task<OfferLetterDetailDto> CreateAsync(CreateOfferLetterDetailDto request, Guid userId);
    Task<OfferLetterDetailDto> UpdateAsync(Guid id, UpdateOfferLetterDetailDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface IOfferNegotiationService
{
    Task<IEnumerable<OfferNegotiationDto>> GetAllAsync();
    Task<OfferNegotiationDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<OfferNegotiationDto>> GetByOfferIdAsync(Guid offerId);
    Task<OfferNegotiationDto> CreateAsync(CreateOfferNegotiationDto request, Guid userId);
    Task<OfferNegotiationDto> UpdateAsync(Guid id, UpdateOfferNegotiationDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
