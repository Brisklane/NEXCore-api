using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;
using Manufacturing.Domain.Entities;
using Manufacturing.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Manufacturing.Infrastructure.Services;

public class BillOfMaterialService : IBillOfMaterialService
{
    private readonly IBillOfMaterialRepository _repo;
    private readonly IBOMItemRepository _itemRepo;
    private readonly IBOMByProductRepository _byProductRepo;
    private readonly ILogger<BillOfMaterialService> _logger;

    public BillOfMaterialService(IBillOfMaterialRepository repo, IBOMItemRepository itemRepo,
        IBOMByProductRepository byProductRepo, ILogger<BillOfMaterialService> logger)
    {
        _repo = repo; _itemRepo = itemRepo; _byProductRepo = byProductRepo; _logger = logger;
    }

    public async Task<BillOfMaterialDto> CreateAsync(CreateBillOfMaterialDto request, Guid userId)
    {
        var bom = new BillOfMaterial
        {
            FinishedProductId = request.FinishedProductId, Version = request.Version,
            EffectiveFrom = request.EffectiveFrom, EffectiveTo = request.EffectiveTo,
            Notes = request.Notes, IsActive = true,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _repo.AddAsync(bom);
        await _repo.SaveChangesAsync();

        // Add items
        foreach (var itemDto in request.Items)
        {
            var item = new BOMItem
            {
                BillOfMaterialId = bom.Id, MaterialId = itemDto.MaterialId,
                QuantityRequired = itemDto.QuantityRequired, ScrapPercentage = itemDto.ScrapPercentage,
                UnitOfMeasure = itemDto.UnitOfMeasure, Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _itemRepo.AddAsync(item);
        }

        // Add by-products
        foreach (var bpDto in request.ByProducts)
        {
            var bp = new BOMByProduct
            {
                BillOfMaterialId = bom.Id, ProductId = bpDto.ProductId, Type = bpDto.Type,
                Quantity = bpDto.Quantity, UnitOfMeasure = bpDto.UnitOfMeasure,
                CostAllocationPercent = bpDto.CostAllocationPercent, WarehouseId = bpDto.WarehouseId,
                Notes = bpDto.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
            };
            await _byProductRepo.AddAsync(bp);
        }

        if (request.Items.Count > 0 || request.ByProducts.Count > 0)
            await _itemRepo.SaveChangesAsync();

        _logger.LogInformation("BOM created for product {ProductId}", bom.FinishedProductId);
        var full = await _repo.GetWithItemsAsync(bom.Id);
        return ManufacturingMapper.ToDto(full!);
    }

    public async Task<BillOfMaterialDto?> GetByIdAsync(Guid id)
    {
        var e = await _repo.GetWithItemsAsync(id);
        return e == null ? null : ManufacturingMapper.ToDto(e);
    }

    public async Task<PaginatedResponse<BillOfMaterialDto>> GetAllAsync(PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, orderBy: q => q.OrderByDescending(x => x.CreatedAt));
        return PaginatedResponse<BillOfMaterialDto>.Ok(items.Select(e => ManufacturingMapper.ToDto(e)), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<BillOfMaterialDto>> GetByProductAsync(Guid productId, PaginationParams pagination)
    {
        var (items, total) = await _repo.GetPagedAsync(pagination.PageNumber, pagination.PageSize, predicate: b => b.FinishedProductId == productId);
        return PaginatedResponse<BillOfMaterialDto>.Ok(items.Select(e => ManufacturingMapper.ToDto(e)), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<BillOfMaterialDto> UpdateAsync(Guid id, UpdateBillOfMaterialDto request, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("BOM not found");
        if (request.Version.HasValue) e.Version = request.Version.Value;
        if (request.IsActive.HasValue) e.IsActive = request.IsActive.Value;
        if (request.EffectiveFrom.HasValue) e.EffectiveFrom = request.EffectiveFrom;
        if (request.EffectiveTo.HasValue) e.EffectiveTo = request.EffectiveTo;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var e = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("BOM not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _repo.Update(e);
        await _repo.SaveChangesAsync();
    }

    public async Task<BOMItemDto> AddItemAsync(Guid bomId, CreateBOMItemDto request, Guid userId)
    {
        var item = new BOMItem
        {
            BillOfMaterialId = bomId, MaterialId = request.MaterialId,
            QuantityRequired = request.QuantityRequired, ScrapPercentage = request.ScrapPercentage,
            UnitOfMeasure = request.UnitOfMeasure, Notes = request.Notes,
            CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _itemRepo.AddAsync(item);
        await _itemRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(item);
    }

    public async Task<BOMItemDto> UpdateItemAsync(Guid itemId, UpdateBOMItemDto request, Guid userId)
    {
        var e = await _itemRepo.GetByIdAsync(itemId) ?? throw new InvalidOperationException("BOM Item not found");
        if (request.QuantityRequired.HasValue) e.QuantityRequired = request.QuantityRequired.Value;
        if (request.ScrapPercentage.HasValue) e.ScrapPercentage = request.ScrapPercentage.Value;
        if (request.UnitOfMeasure != null) e.UnitOfMeasure = request.UnitOfMeasure;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _itemRepo.Update(e);
        await _itemRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteItemAsync(Guid itemId, Guid userId)
    {
        var e = await _itemRepo.GetByIdAsync(itemId) ?? throw new InvalidOperationException("BOM Item not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _itemRepo.Update(e);
        await _itemRepo.SaveChangesAsync();
    }

    public async Task<BOMByProductDto> AddByProductAsync(Guid bomId, CreateBOMByProductDto request, Guid userId)
    {
        var entity = new BOMByProduct
        {
            BillOfMaterialId = bomId, ProductId = request.ProductId, Type = request.Type,
            Quantity = request.Quantity, UnitOfMeasure = request.UnitOfMeasure,
            CostAllocationPercent = request.CostAllocationPercent, WarehouseId = request.WarehouseId,
            Notes = request.Notes, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId
        };
        await _byProductRepo.AddAsync(entity);
        await _byProductRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(entity);
    }

    public async Task<BOMByProductDto> UpdateByProductAsync(Guid byProductId, UpdateBOMByProductDto request, Guid userId)
    {
        var e = await _byProductRepo.GetByIdAsync(byProductId) ?? throw new InvalidOperationException("ByProduct not found");
        if (request.Quantity.HasValue) e.Quantity = request.Quantity.Value;
        if (request.UnitOfMeasure != null) e.UnitOfMeasure = request.UnitOfMeasure;
        if (request.CostAllocationPercent.HasValue) e.CostAllocationPercent = request.CostAllocationPercent;
        if (request.WarehouseId.HasValue) e.WarehouseId = request.WarehouseId;
        if (request.Notes != null) e.Notes = request.Notes;
        e.UpdatedAt = DateTime.UtcNow; e.UpdatedByUserId = userId;
        _byProductRepo.Update(e);
        await _byProductRepo.SaveChangesAsync();
        return ManufacturingMapper.ToDto(e);
    }

    public async Task DeleteByProductAsync(Guid byProductId, Guid userId)
    {
        var e = await _byProductRepo.GetByIdAsync(byProductId) ?? throw new InvalidOperationException("ByProduct not found");
        e.IsDeleted = true; e.DeletedAt = DateTime.UtcNow; e.DeletedByUserId = userId;
        _byProductRepo.Update(e);
        await _byProductRepo.SaveChangesAsync();
    }
}
