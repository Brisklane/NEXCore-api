using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class ContactListService : IContactListService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<ContactListService> _logger;

    public ContactListService(CrmDbContext db, ILogger<ContactListService> logger) { _db = db; _logger = logger; }

    public async Task<ContactListDto> CreateAsync(CreateContactListDto dto, Guid userId)
    {
        var entity = new ContactList
        {
            ListName = dto.ListName, Description = dto.Description,
            IsDynamic = dto.IsDynamic, FilterCriteria = dto.FilterCriteria,
            OwnerId = dto.OwnerId ?? userId, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.ContactLists.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity, 0);
    }

    public async Task<ContactListDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.ContactLists.AsNoTracking().Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return null;
        var count = e.Members.Count(m => !m.IsDeleted);
        return MapToDto(e, count);
    }

    public async Task<(IEnumerable<ContactListDto> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _db.ContactLists.AsNoTracking().Where(x => !x.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.ListName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(e => MapToDto(e, 0)), total);
    }

    public async Task<ContactListDto> UpdateAsync(Guid id, UpdateContactListDto dto, Guid userId)
    {
        var entity = await _db.ContactLists.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Contact list not found");
        entity.ListName = dto.ListName; entity.Description = dto.Description;
        entity.IsDynamic = dto.IsDynamic; entity.FilterCriteria = dto.FilterCriteria;
        entity.OwnerId = dto.OwnerId; entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity, 0);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.ContactLists.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Contact list not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<ContactListMemberDto> AddMemberAsync(Guid listId, CreateContactListMemberDto dto, Guid userId)
    {
        var entity = new ContactListMember
        {
            ContactListId = listId, ContactId = dto.ContactId, LeadId = dto.LeadId,
            MemberType = dto.MemberType, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.ContactListMembers.Add(entity);
        await _db.SaveChangesAsync();
        return MapMemberDto(entity);
    }

    public async Task<IEnumerable<ContactListMemberDto>> GetMembersAsync(Guid listId)
    {
        var items = await _db.ContactListMembers.AsNoTracking()
            .Include(x => x.Contact).Include(x => x.Lead)
            .Where(x => x.ContactListId == listId && !x.IsDeleted).ToListAsync();
        return items.Select(MapMemberDto);
    }

    public async Task RemoveMemberAsync(Guid listId, Guid memberId, Guid userId)
    {
        var entity = await _db.ContactListMembers
            .FirstOrDefaultAsync(x => x.Id == memberId && x.ContactListId == listId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Member not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static ContactListDto MapToDto(ContactList e, int memberCount) => new()
    {
        Id = e.Id, ListName = e.ListName, Description = e.Description,
        IsDynamic = e.IsDynamic, FilterCriteria = e.FilterCriteria,
        OwnerId = e.OwnerId, MemberCount = memberCount, CreatedAt = e.CreatedAt
    };

    private static ContactListMemberDto MapMemberDto(ContactListMember e) => new()
    {
        Id = e.Id, ContactListId = e.ContactListId,
        ContactId = e.ContactId,
        ContactName = e.Contact is null ? null : $"{e.Contact.FirstName} {e.Contact.LastName}",
        LeadId = e.LeadId,
        LeadName = e.Lead is null ? null : $"{e.Lead.FirstName} {e.Lead.LastName}",
        MemberType = e.MemberType, CreatedAt = e.CreatedAt
    };
}
