using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IContactListService
{
    Task<ContactListDto> CreateAsync(CreateContactListDto dto, Guid userId);
    Task<ContactListDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<ContactListDto> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task<ContactListDto> UpdateAsync(Guid id, UpdateContactListDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<ContactListMemberDto> AddMemberAsync(Guid listId, CreateContactListMemberDto dto, Guid userId);
    Task<IEnumerable<ContactListMemberDto>> GetMembersAsync(Guid listId);
    Task RemoveMemberAsync(Guid listId, Guid memberId, Guid userId);
}
