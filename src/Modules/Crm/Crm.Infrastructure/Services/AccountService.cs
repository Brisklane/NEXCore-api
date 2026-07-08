using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly ILogger<AccountService> _logger;

    public AccountService(IAccountRepository repository, ILogger<AccountService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AccountDto> CreateAsync(CreateAccountDto dto, Guid userId)
    {
        var entity = new Account
        {
            AccountName = dto.AccountName,
            Website = dto.Website,
            Phone = dto.Phone,
            Type = dto.Type,
            Industry = dto.Industry,
            BillingStreet = dto.BillingStreet,
            BillingCity = dto.BillingCity,
            BillingState = dto.BillingState,
            BillingPostalCode = dto.BillingPostalCode,
            BillingCountry = dto.BillingCountry,
            ShippingStreet = dto.ShippingStreet,
            ShippingCity = dto.ShippingCity,
            ShippingState = dto.ShippingState,
            ShippingPostalCode = dto.ShippingPostalCode,
            ShippingCountry = dto.ShippingCountry,
            ParentAccountId = dto.ParentAccountId,
            OwnerId = dto.OwnerId ?? userId,
            Description = dto.Description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        // CompanyId, BranchId, BusinessUnitId are set automatically by TenantAwareRepository
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        _logger.LogInformation("Account created: {AccountId}", entity.Id);
        return MapToDto(entity);
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<PaginatedResponse<AccountDto>> GetPagedAsync(PaginationParams pagination)
    {
        var (items, total) = await _repository.SearchPagedAsync(pagination.PageNumber, pagination.PageSize, pagination.SearchTerm);
        return PaginatedResponse<AccountDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<AccountDto> UpdateAsync(Guid id, UpdateAccountDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Account not found");
        entity.AccountName = dto.AccountName;
        entity.Website = dto.Website;
        entity.Phone = dto.Phone;
        entity.Type = dto.Type;
        entity.Industry = dto.Industry;
        entity.BillingStreet = dto.BillingStreet;
        entity.BillingCity = dto.BillingCity;
        entity.BillingState = dto.BillingState;
        entity.BillingPostalCode = dto.BillingPostalCode;
        entity.BillingCountry = dto.BillingCountry;
        entity.ShippingStreet = dto.ShippingStreet;
        entity.ShippingCity = dto.ShippingCity;
        entity.ShippingState = dto.ShippingState;
        entity.ShippingPostalCode = dto.ShippingPostalCode;
        entity.ShippingCountry = dto.ShippingCountry;
        entity.ParentAccountId = dto.ParentAccountId;
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
            ?? throw new InvalidOperationException("Account not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static AccountDto MapToDto(Account e) => new()
    {
        Id = e.Id,
        AccountName = e.AccountName,
        Website = e.Website,
        Phone = e.Phone,
        Type = e.Type,
        Industry = e.Industry,
        BillingStreet = e.BillingStreet,
        BillingCity = e.BillingCity,
        BillingState = e.BillingState,
        BillingPostalCode = e.BillingPostalCode,
        BillingCountry = e.BillingCountry,
        ShippingStreet = e.ShippingStreet,
        ShippingCity = e.ShippingCity,
        ShippingState = e.ShippingState,
        ShippingPostalCode = e.ShippingPostalCode,
        ShippingCountry = e.ShippingCountry,
        ParentAccountId = e.ParentAccountId,
        OwnerId = e.OwnerId,
        Description = e.Description,
        CreatedAt = e.CreatedAt
    };
}
