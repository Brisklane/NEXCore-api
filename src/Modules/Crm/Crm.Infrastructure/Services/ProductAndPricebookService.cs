using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(CrmDbContext db, ILogger<ProductService> logger) { _db = db; _logger = logger; }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, Guid userId)
    {
        var entity = new Product
        {
            ProductName = dto.ProductName, ProductCode = dto.ProductCode,
            ProductFamily = dto.ProductFamily, Description = dto.Description,
            IsActive = dto.IsActive, QuantityUnit = dto.QuantityUnit,
            QuantityUnitPrice = dto.QuantityUnitPrice,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Products.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(IEnumerable<ProductDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var query = _db.Products.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Lower-cased on both sides so the match stays case-insensitive. SQL Server's
            // default collation did this implicitly; PostgreSQL compares case-sensitively.
            var term = search.Trim().ToLower();
            query = query.Where(x => x.ProductName.ToLower().Contains(term)
                                  || (x.ProductCode != null && x.ProductCode.ToLower().Contains(term)));
        }
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.ProductName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), total);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductDto dto, Guid userId)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Product not found");
        entity.ProductName = dto.ProductName; entity.ProductCode = dto.ProductCode;
        entity.ProductFamily = dto.ProductFamily; entity.Description = dto.Description;
        entity.IsActive = dto.IsActive; entity.QuantityUnit = dto.QuantityUnit;
        entity.QuantityUnitPrice = dto.QuantityUnitPrice;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Products.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Product not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static ProductDto MapToDto(Product e) => new()
    {
        Id = e.Id, ProductName = e.ProductName, ProductCode = e.ProductCode,
        ProductFamily = e.ProductFamily, Description = e.Description,
        IsActive = e.IsActive, QuantityUnit = e.QuantityUnit,
        QuantityUnitPrice = e.QuantityUnitPrice, CreatedAt = e.CreatedAt
    };
}

public class PricebookService : IPricebookService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<PricebookService> _logger;

    public PricebookService(CrmDbContext db, ILogger<PricebookService> logger) { _db = db; _logger = logger; }

    public async Task<PricebookDto> CreateAsync(CreatePricebookDto dto, Guid userId)
    {
        var entity = new Pricebook
        {
            PricebookName = dto.PricebookName, Description = dto.Description,
            IsActive = dto.IsActive, CurrencyCode = dto.CurrencyCode, IsStandard = false,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Pricebooks.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<PricebookDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Pricebooks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<PricebookDto>> GetAllAsync()
    {
        var items = await _db.Pricebooks.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.PricebookName).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<PricebookDto> UpdateAsync(Guid id, UpdatePricebookDto dto, Guid userId)
    {
        var entity = await _db.Pricebooks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pricebook not found");
        entity.PricebookName = dto.PricebookName; entity.Description = dto.Description;
        entity.IsActive = dto.IsActive; entity.CurrencyCode = dto.CurrencyCode;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Pricebooks.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pricebook not found");
        if (entity.IsStandard) throw new InvalidOperationException("Cannot delete the standard pricebook");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<PricebookEntryDto> AddEntryAsync(Guid pricebookId, CreatePricebookEntryDto dto, Guid userId)
    {
        var entity = new PricebookEntry
        {
            PricebookId = pricebookId, ProductId = dto.ProductId,
            UnitPrice = dto.UnitPrice, IsActive = dto.IsActive,
            UseStandardPrice = dto.UseStandardPrice, CurrencyCode = dto.CurrencyCode,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.PricebookEntries.Add(entity);
        await _db.SaveChangesAsync();
        var product = await _db.Products.FindAsync(dto.ProductId);
        entity.Product = product!;
        return MapEntryDto(entity);
    }

    public async Task<IEnumerable<PricebookEntryDto>> GetEntriesAsync(Guid pricebookId)
    {
        var items = await _db.PricebookEntries.AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.PricebookId == pricebookId && !x.IsDeleted)
            .ToListAsync();
        return items.Select(MapEntryDto);
    }

    public async Task<PricebookEntryDto> UpdateEntryAsync(Guid pricebookId, Guid entryId, UpdatePricebookEntryDto dto, Guid userId)
    {
        var entity = await _db.PricebookEntries.Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == entryId && x.PricebookId == pricebookId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pricebook entry not found");
        entity.UnitPrice = dto.UnitPrice; entity.IsActive = dto.IsActive;
        entity.UseStandardPrice = dto.UseStandardPrice;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapEntryDto(entity);
    }

    public async Task RemoveEntryAsync(Guid pricebookId, Guid entryId, Guid userId)
    {
        var entity = await _db.PricebookEntries
            .FirstOrDefaultAsync(x => x.Id == entryId && x.PricebookId == pricebookId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Pricebook entry not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static PricebookDto MapToDto(Pricebook e) => new()
    {
        Id = e.Id, PricebookName = e.PricebookName, Description = e.Description,
        IsActive = e.IsActive, IsStandard = e.IsStandard, CurrencyCode = e.CurrencyCode, CreatedAt = e.CreatedAt
    };

    private static PricebookEntryDto MapEntryDto(PricebookEntry e) => new()
    {
        Id = e.Id, PricebookId = e.PricebookId, ProductId = e.ProductId,
        ProductName = e.Product?.ProductName, UnitPrice = e.UnitPrice,
        IsActive = e.IsActive, UseStandardPrice = e.UseStandardPrice, CurrencyCode = e.CurrencyCode
    };
}
