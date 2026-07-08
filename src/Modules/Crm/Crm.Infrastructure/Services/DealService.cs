using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class DealService : IDealService
{
    private readonly IDealRepository _repository;
    private readonly IDealProductRepository _dealProductRepository;
    private readonly IDealContactRepository _dealContactRepository;
    private readonly IPricebookEntryRepository _pricebookEntryRepository;
    private readonly ILogger<DealService> _logger;

    public DealService(
        IDealRepository repository,
        IDealProductRepository dealProductRepository,
        IDealContactRepository dealContactRepository,
        IPricebookEntryRepository pricebookEntryRepository,
        ILogger<DealService> logger)
    {
        _repository = repository;
        _dealProductRepository = dealProductRepository;
        _dealContactRepository = dealContactRepository;
        _pricebookEntryRepository = pricebookEntryRepository;
        _logger = logger;
    }

    public async Task<DealDto> CreateAsync(CreateDealDto dto, Guid userId)
    {
        var entity = new Deal
        {
            OpportunityName = dto.OpportunityName, AccountId = dto.AccountId,
            CloseDate = dto.CloseDate, Amount = dto.Amount, Stage = dto.Stage,
            ForecastCategory = dto.ForecastCategory, OwnerId = dto.OwnerId ?? userId,
            PipelineId = dto.PipelineId, PipelineStageId = dto.PipelineStageId,
            Description = dto.Description, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<DealDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdWithDetailsAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<DealDto>> GetPagedAsync(PaginationParams pagination, string? stage = null)
    {
        var (items, total) = await _repository.SearchPagedAsync(pagination.PageNumber, pagination.PageSize, pagination.SearchTerm, stage);
        return PaginatedResponse<DealDto>.Ok(items.OrderByDescending(x => x.CreatedAt).Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<IEnumerable<DealDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _repository.GetByAccountIdAsync(accountId);
        return items.Select(MapToDto);
    }

    public async Task<DealDto> UpdateAsync(Guid id, UpdateDealDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Deal not found");
        entity.OpportunityName = dto.OpportunityName; entity.AccountId = dto.AccountId;
        entity.CloseDate = dto.CloseDate; entity.Amount = dto.Amount; entity.Stage = dto.Stage;
        entity.ForecastCategory = dto.ForecastCategory; entity.OwnerId = dto.OwnerId;
        entity.PipelineId = dto.PipelineId; entity.PipelineStageId = dto.PipelineStageId;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Deal not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    // ??? Deal Products ??????????????????????????????????????????????????????????
    public async Task<DealProductDto> AddProductAsync(Guid dealId, CreateDealProductDto dto, Guid userId)
    {
        PricebookEntry? entry = null;
        if (dto.PricebookEntryId.HasValue)
            entry = await _pricebookEntryRepository.GetByIdAsync(dto.PricebookEntryId.Value);
        var listPrice = entry?.UnitPrice ?? dto.UnitPrice;
        var discountedUnit = dto.Discount.HasValue ? dto.UnitPrice * (1 - dto.Discount.Value / 100) : dto.UnitPrice;
        var entity = new DealProduct
        {
            DealId = dealId, ProductId = dto.ProductId, PricebookEntryId = dto.PricebookEntryId,
            Quantity = dto.Quantity, UnitPrice = dto.UnitPrice, ListPrice = listPrice,
            Discount = dto.Discount, TotalPrice = dto.Quantity * discountedUnit,
            Description = dto.Description, SortOrder = dto.SortOrder,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _dealProductRepository.AddAsync(entity);
        await _dealProductRepository.SaveChangesAsync();
        return MapDealProductDto(entity);
    }

    public async Task<IEnumerable<DealProductDto>> GetProductsAsync(Guid dealId)
    {
        var items = await _dealProductRepository.GetByDealIdAsync(dealId);
        return items.OrderBy(x => x.SortOrder).Select(MapDealProductDto);
    }

    public async Task<DealProductDto> UpdateProductAsync(Guid dealId, Guid dealProductId, UpdateDealProductDto dto, Guid userId)
    {
        var entity = await _dealProductRepository.GetByIdAsync(dealProductId)
            ?? throw new InvalidOperationException("Deal product not found");
        if (entity.DealId != dealId) throw new InvalidOperationException("Deal product not found");
        var discountedUnit = dto.Discount.HasValue ? dto.UnitPrice * (1 - dto.Discount.Value / 100) : dto.UnitPrice;
        entity.ProductId = dto.ProductId; entity.Quantity = dto.Quantity; entity.UnitPrice = dto.UnitPrice;
        entity.Discount = dto.Discount; entity.TotalPrice = dto.Quantity * discountedUnit;
        entity.Description = dto.Description; entity.SortOrder = dto.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _dealProductRepository.Update(entity);
        await _dealProductRepository.SaveChangesAsync();
        return MapDealProductDto(entity);
    }

    public async Task RemoveProductAsync(Guid dealId, Guid dealProductId, Guid userId)
    {
        var entity = await _dealProductRepository.GetByIdAsync(dealProductId)
            ?? throw new InvalidOperationException("Deal product not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _dealProductRepository.Update(entity);
        await _dealProductRepository.SaveChangesAsync();
    }

    // ??? Deal Contacts ??????????????????????????????????????????????????????????
    public async Task<DealContactDto> AddContactAsync(Guid dealId, CreateDealContactDto dto, Guid userId)
    {
        if (dto.IsPrimary)
        {
            var existing = await _dealContactRepository.GetByDealIdAsync(dealId);
            foreach (var dc in existing.Where(x => x.IsPrimary))
            {
                dc.IsPrimary = false;
                _dealContactRepository.Update(dc);
            }
        }
        var entity = new DealContact
        {
            DealId = dealId, ContactId = dto.ContactId, Role = dto.Role, IsPrimary = dto.IsPrimary,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _dealContactRepository.AddAsync(entity);
        await _dealContactRepository.SaveChangesAsync();
        return MapDealContactDto(entity);
    }

    public async Task<IEnumerable<DealContactDto>> GetContactsAsync(Guid dealId)
    {
        var items = await _dealContactRepository.GetByDealIdAsync(dealId);
        return items.Select(MapDealContactDto);
    }

    public async Task RemoveContactAsync(Guid dealId, Guid dealContactId, Guid userId)
    {
        var entity = await _dealContactRepository.GetByIdAsync(dealContactId)
            ?? throw new InvalidOperationException("Deal contact not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _dealContactRepository.Update(entity);
        await _dealContactRepository.SaveChangesAsync();
    }

    private static DealDto MapToDto(Deal e) => new()
    {
        Id = e.Id, OpportunityName = e.OpportunityName, AccountId = e.AccountId,
        AccountName = e.Account?.AccountName, CloseDate = e.CloseDate, Amount = e.Amount,
        Stage = e.Stage, ForecastCategory = e.ForecastCategory, OwnerId = e.OwnerId,
        PipelineId = e.PipelineId, PipelineStageId = e.PipelineStageId,
        Description = e.Description, CreatedAt = e.CreatedAt
    };

    private static DealProductDto MapDealProductDto(DealProduct e) => new()
    {
        Id = e.Id, DealId = e.DealId, ProductId = e.ProductId,
        ProductName = e.Product?.ProductName, Quantity = e.Quantity,
        UnitPrice = e.UnitPrice, ListPrice = e.ListPrice, Discount = e.Discount,
        TotalPrice = e.TotalPrice, Description = e.Description, SortOrder = e.SortOrder
    };

    private static DealContactDto MapDealContactDto(DealContact e) => new()
    {
        Id = e.Id, DealId = e.DealId, ContactId = e.ContactId,
        ContactName = e.Contact is null ? null : $"{e.Contact.FirstName} {e.Contact.LastName}",
        Role = e.Role, IsPrimary = e.IsPrimary
    };
}
