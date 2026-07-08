using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class ContractService : IContractService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<ContractService> _logger;

    public ContractService(CrmDbContext db, ILogger<ContractService> logger) { _db = db; _logger = logger; }

    public async Task<ContractDto> CreateAsync(CreateContractDto dto, Guid userId)
    {
        var count = await _db.Contracts.CountAsync() + 1;
        var entity = new Contract
        {
            ContractNumber = $"CTR-{DateTime.UtcNow:yyyyMMdd}-{count:D4}",
            AccountId = dto.AccountId, OwnerId = dto.OwnerId ?? userId,
            Status = ContractStatus.Draft, StartDate = dto.StartDate,
            ContractTermMonths = dto.ContractTermMonths, EndDate = dto.EndDate,
            ContractValue = dto.ContractValue, CurrencyCode = dto.CurrencyCode,
            BillingContactId = dto.BillingContactId, SpecialTerms = dto.SpecialTerms,
            Description = dto.Description, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Contracts.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ContractDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Contracts.AsNoTracking().Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(IEnumerable<ContractDto> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _db.Contracts.AsNoTracking().Include(x => x.Account).Where(x => !x.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), total);
    }

    public async Task<IEnumerable<ContractDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _db.Contracts.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<ContractDto> UpdateAsync(Guid id, UpdateContractDto dto, Guid userId)
    {
        var entity = await _db.Contracts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Contract not found");
        entity.AccountId = dto.AccountId; entity.OwnerId = dto.OwnerId;
        entity.StartDate = dto.StartDate; entity.ContractTermMonths = dto.ContractTermMonths;
        entity.EndDate = dto.EndDate; entity.ContractValue = dto.ContractValue;
        entity.CurrencyCode = dto.CurrencyCode; entity.BillingContactId = dto.BillingContactId;
        entity.SpecialTerms = dto.SpecialTerms; entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Contracts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Contract not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<ContractDto> ActivateAsync(Guid id, Guid userId)
    {
        var entity = await _db.Contracts.Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Contract not found");
        if (entity.Status == ContractStatus.Activated)
            throw new InvalidOperationException("Contract is already activated");
        entity.Status = ContractStatus.Activated;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    private static ContractDto MapToDto(Contract e) => new()
    {
        Id = e.Id, ContractNumber = e.ContractNumber, AccountId = e.AccountId,
        AccountName = e.Account?.AccountName, OwnerId = e.OwnerId, Status = e.Status,
        StartDate = e.StartDate, ContractTermMonths = e.ContractTermMonths, EndDate = e.EndDate,
        ContractValue = e.ContractValue, CurrencyCode = e.CurrencyCode,
        BillingContactId = e.BillingContactId, SpecialTerms = e.SpecialTerms,
        Description = e.Description, CreatedAt = e.CreatedAt
    };
}
