using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _repository;
    private readonly IAccountRepository _accountRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IDealRepository _dealRepository;
    private readonly ILogger<LeadService> _logger;

    public LeadService(
        ILeadRepository repository,
        IAccountRepository accountRepository,
        IContactRepository contactRepository,
        IDealRepository dealRepository,
        ILogger<LeadService> logger)
    {
        _repository = repository;
        _accountRepository = accountRepository;
        _contactRepository = contactRepository;
        _dealRepository = dealRepository;
        _logger = logger;
    }

    public async Task<LeadDto> CreateAsync(CreateLeadDto dto, Guid userId)
    {
        var entity = new Lead
        {
            Salutation = dto.Salutation, FirstName = dto.FirstName, LastName = dto.LastName,
            Company = dto.Company, Title = dto.Title, Website = dto.Website,
            Phone = dto.Phone, Email = dto.Email, Street = dto.Street, City = dto.City,
            State = dto.State, PostalCode = dto.PostalCode, Country = dto.Country,
            NumberOfEmployees = dto.NumberOfEmployees, AnnualRevenue = dto.AnnualRevenue,
            LeadSource = dto.LeadSource, Industry = dto.Industry, Status = dto.Status,
            OwnerId = dto.OwnerId ?? userId, AssignedEmployeeId = dto.AssignedEmployeeId,
            Description = dto.Description, EmailOptOut = dto.EmailOptOut,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<LeadDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<LeadDto>> GetPagedAsync(PaginationParams pagination, string? status = null)
    {
        var (items, total) = await _repository.SearchPagedAsync(pagination.PageNumber, pagination.PageSize, pagination.SearchTerm, status);
        return PaginatedResponse<LeadDto>.Ok(items.OrderByDescending(x => x.CreatedAt).Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<LeadDto> UpdateAsync(Guid id, UpdateLeadDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Lead not found");
        entity.Salutation = dto.Salutation; entity.FirstName = dto.FirstName; entity.LastName = dto.LastName;
        entity.Company = dto.Company; entity.Title = dto.Title; entity.Website = dto.Website;
        entity.Phone = dto.Phone; entity.Email = dto.Email; entity.Street = dto.Street;
        entity.City = dto.City; entity.State = dto.State; entity.PostalCode = dto.PostalCode;
        entity.Country = dto.Country; entity.NumberOfEmployees = dto.NumberOfEmployees;
        entity.AnnualRevenue = dto.AnnualRevenue; entity.LeadSource = dto.LeadSource;
        entity.Industry = dto.Industry; entity.Status = dto.Status; entity.OwnerId = dto.OwnerId;
        entity.AssignedEmployeeId = dto.AssignedEmployeeId;
        entity.Description = dto.Description; entity.EmailOptOut = dto.EmailOptOut;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Lead not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<LeadDto> ConvertAsync(Guid id, ConvertLeadDto dto, Guid userId)
    {
        var lead = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Lead not found");
        if (lead.IsConverted) throw new InvalidOperationException("Lead is already converted");

        Account? account = null;
        Contact? contact = null;
        Deal? deal = null;

        if (dto.CreateAccount)
        {
            account = new Account
            {
                AccountName = lead.Company, Phone = lead.Phone, Website = lead.Website,
                BillingStreet = lead.Street, BillingCity = lead.City, BillingState = lead.State,
                BillingPostalCode = lead.PostalCode, BillingCountry = lead.Country,
                OwnerId = userId, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            };
            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();
        }

        if (dto.CreateContact)
        {
            contact = new Contact
            {
                FirstName = lead.FirstName, LastName = lead.LastName, Title = lead.Title,
                Phone = lead.Phone, Email = lead.Email,
                MailingStreet = lead.Street, MailingCity = lead.City, MailingState = lead.State,
                MailingPostalCode = lead.PostalCode, MailingCountry = lead.Country,
                AccountId = account?.Id, OwnerId = userId,
                ConvertedFromLeadId = lead.Id,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            };
            await _contactRepository.AddAsync(contact);
            await _contactRepository.SaveChangesAsync();
        }

        if (dto.CreateDeal && !string.IsNullOrWhiteSpace(dto.DealName) && account != null)
        {
            deal = new Deal
            {
                OpportunityName = dto.DealName, AccountId = account.Id,
                Amount = dto.DealAmount, CloseDate = dto.DealCloseDate ?? DateTime.UtcNow.AddDays(30),
                Stage = "--None--", OwnerId = userId,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            };
            await _dealRepository.AddAsync(deal);
            await _dealRepository.SaveChangesAsync();
        }

        lead.IsConverted = true;
        lead.ConvertedAt = DateTime.UtcNow;
        lead.Status = "Converted";
        lead.ConvertedAccountId = account?.Id;
        lead.ConvertedContactId = contact?.Id;
        lead.ConvertedDealId = deal?.Id;
        lead.UpdatedAt = DateTime.UtcNow;
        lead.UpdatedByUserId = userId;
        _repository.Update(lead);
        await _repository.SaveChangesAsync();
        return MapToDto(lead);
    }

    private static LeadDto MapToDto(Lead e) => new()
    {
        Id = e.Id, Salutation = e.Salutation, FirstName = e.FirstName, LastName = e.LastName,
        Company = e.Company, Title = e.Title, Website = e.Website, Phone = e.Phone, Email = e.Email,
        Street = e.Street, City = e.City, State = e.State, PostalCode = e.PostalCode, Country = e.Country,
        NumberOfEmployees = e.NumberOfEmployees, AnnualRevenue = e.AnnualRevenue,
        LeadSource = e.LeadSource, Industry = e.Industry, Status = e.Status,
        OwnerId = e.OwnerId, AssignedEmployeeId = e.AssignedEmployeeId,
        Description = e.Description, EmailOptOut = e.EmailOptOut,
        IsConverted = e.IsConverted, ConvertedAt = e.ConvertedAt, CreatedAt = e.CreatedAt
    };
}
