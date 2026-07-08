using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IQuoteService
{
    Task<QuoteDto> CreateAsync(CreateQuoteDto dto, Guid userId);
    Task<QuoteDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<QuoteDto> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<QuoteDto>> GetByDealIdAsync(Guid dealId);
    Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
