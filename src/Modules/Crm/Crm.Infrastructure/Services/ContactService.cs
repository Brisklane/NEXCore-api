using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class ContactService : IContactService
{
    private readonly IContactRepository _repository;
    private readonly ILogger<ContactService> _logger;

    public ContactService(IContactRepository repository, ILogger<ContactService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ContactDto> CreateAsync(CreateContactDto dto, Guid userId)
    {
        var entity = new Contact
        {
            Salutation = dto.Salutation,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            AccountId = dto.AccountId,
            Title = dto.Title,
            ReportsToId = dto.ReportsToId,
            Phone = dto.Phone,
            Email = dto.Email,
            MailingStreet = dto.MailingStreet,
            MailingCity = dto.MailingCity,
            MailingState = dto.MailingState,
            MailingPostalCode = dto.MailingPostalCode,
            MailingCountry = dto.MailingCountry,
            EmailOptOut = dto.EmailOptOut,
            OwnerId = dto.OwnerId ?? userId,
            Description = dto.Description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ContactDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdWithDetailsAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<ContactDto>> GetPagedAsync(PaginationParams pagination)
    {
        // The repository orders inside the query; re-sorting here would only reorder the
        // page that already came back and hide the fact that paging was unordered.
        var (items, total) = await _repository.SearchPagedAsync(pagination.PageNumber, pagination.PageSize, pagination.SearchTerm);
        return PaginatedResponse<ContactDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<ContactDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _repository.GetByAccountIdAsync(accountId);
        return items.Select(MapToDto);
    }

    public async Task<ContactDto> UpdateAsync(Guid id, UpdateContactDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Contact not found");
        entity.Salutation = dto.Salutation;
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.AccountId = dto.AccountId;
        entity.Title = dto.Title;
        entity.ReportsToId = dto.ReportsToId;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.MailingStreet = dto.MailingStreet;
        entity.MailingCity = dto.MailingCity;
        entity.MailingState = dto.MailingState;
        entity.MailingPostalCode = dto.MailingPostalCode;
        entity.MailingCountry = dto.MailingCountry;
        entity.EmailOptOut = dto.EmailOptOut;
        entity.OwnerId = dto.OwnerId;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Contact not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static ContactDto MapToDto(Contact e) => new()
    {
        Id = e.Id, Salutation = e.Salutation, FirstName = e.FirstName, LastName = e.LastName,
        AccountId = e.AccountId, AccountName = e.Account?.AccountName,
        Title = e.Title, ReportsToId = e.ReportsToId, Phone = e.Phone, Email = e.Email,
        MailingStreet = e.MailingStreet, MailingCity = e.MailingCity, MailingState = e.MailingState,
        MailingPostalCode = e.MailingPostalCode, MailingCountry = e.MailingCountry,
        EmailOptOut = e.EmailOptOut, OwnerId = e.OwnerId, Description = e.Description, CreatedAt = e.CreatedAt,
        IsAnonymous = e.IsAnonymous
    };
}
