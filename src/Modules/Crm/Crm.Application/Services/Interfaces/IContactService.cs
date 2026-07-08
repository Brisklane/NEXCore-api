using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface IContactService
{
    Task<ContactDto> CreateAsync(CreateContactDto dto, Guid userId);
    Task<ContactDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<ContactDto>> GetPagedAsync(PaginationParams pagination);
    Task<IEnumerable<ContactDto>> GetByAccountIdAsync(Guid accountId);
    Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
