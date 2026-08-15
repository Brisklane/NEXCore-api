using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// PriceList service - handles code uniqueness, active filtering, and item retrieval.
/// </summary>
public class PriceListService : IPriceListService
{
    private readonly IPriceListRepository _priceLists;
    private readonly ILogger<PriceListService> _logger;

    public PriceListService(IPriceListRepository priceLists, ILogger<PriceListService> logger)
    {
        _priceLists = priceLists;
        _logger = logger;
    }

    public async Task<List<PriceListDto>> GetAllAsync()
    {
        var list = await _priceLists.GetAllAsync();
        return list.OrderByDescending(p => p.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<PriceListDto?> GetByIdAsync(Guid id)
    {
        var pl = await _priceLists.GetWithItemsAsync(id);
        return pl is null ? null : MapToDto(pl);
    }

    public async Task<PriceListDto?> GetByCodeAsync(string code)
    {
        var pl = await _priceLists.GetByCodeAsync(code);
        return pl is null ? null : MapToDto(pl);
    }

    public async Task<List<PriceListDto>> GetActiveAsync()
    {
        var list = await _priceLists.GetActiveAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<PriceListDto> CreateAsync(CreatePriceListDto dto)
    {
        // Auto-generate a sequential code (PL-0001, PL-0002, …) when none is supplied.
        var code = string.IsNullOrWhiteSpace(dto.Code) ? await GenerateCodeAsync() : dto.Code.Trim();

        var existing = await _priceLists.GetByCodeAsync(code);
        if (existing is not null)
            throw new InvalidOperationException("Price list code already exists");

        var pl = new PriceList
        {
            Code = code,
            Name = dto.Name,
            Type = dto.ListType,
            CurrencyCode = dto.CurrencyCode,
            ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
            ValidTo = dto.ValidTo,
            IsActive = true,
        };

        await _priceLists.AddAsync(pl);
        await _priceLists.SaveChangesAsync();

        _logger.LogInformation("Price list created: {Code} (ID: {Id})", pl.Code, pl.Id);
        return MapToDto(pl);
    }

    public async Task<PriceListDto> UpdateAsync(Guid id, UpdatePriceListDto dto)
    {
        // Tracked load — GetByIdAsync is AsNoTracking, which made SaveChanges a silent no-op.
        var pl = await _priceLists.GetWithItemsAsync(id)
            ?? throw new InvalidOperationException("Price list not found");

        if (dto.Name         != null) pl.Name         = dto.Name;
        if (dto.CurrencyCode != null) pl.CurrencyCode = dto.CurrencyCode;
        if (dto.ValidFrom    != null) pl.ValidFrom     = dto.ValidFrom.Value;
        if (dto.ValidTo      != null) pl.ValidTo       = dto.ValidTo;
        if (dto.IsActive     != null) pl.IsActive      = dto.IsActive.Value;

        await _priceLists.SaveChangesAsync();

        _logger.LogInformation("Price list updated: {Id}", id);
        return MapToDto(pl);
    }

    /// <summary>Next sequential price-list code as PL-#### based on the highest existing code.</summary>
    private async Task<string> GenerateCodeAsync()
    {
        var all = await _priceLists.GetAllAsync();
        var max = 0;
        foreach (var p in all)
        {
            var code = p.Code ?? string.Empty;
            if (code.StartsWith("PL-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(code[3..], out var n) && n > max)
                max = n;
        }
        return $"PL-{max + 1:D4}";
    }

    public async Task DeleteAsync(Guid id)
    {
        var pl = await _priceLists.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Price list not found");

        _priceLists.Delete(pl);
        await _priceLists.SaveChangesAsync();

        _logger.LogInformation("Price list deleted: {Id}", id);
    }

    // ── Lines ─────────────────────────────────────────────────────────────

    public async Task<List<PriceListItemDto>> GetItemsAsync(Guid priceListId)
    {
        var items = await _priceLists.GetItemsAsync(priceListId);
        return items.Select(MapItem).ToList();
    }

    public async Task<PriceListItemDto> AddItemAsync(Guid priceListId, CreatePriceListItemDto dto)
    {
        var list = await _priceLists.GetByIdAsync(priceListId)
            ?? throw new InvalidOperationException("Price list not found");

        if (dto.ProductId == Guid.Empty)
            throw new InvalidOperationException("A product is required");
        if (dto.UnitPrice < 0)
            throw new InvalidOperationException("Unit price cannot be negative");
        if (dto.MinQuantity is { } min && dto.MaxQuantity is { } max && min > max)
            throw new InvalidOperationException("Minimum quantity cannot exceed the maximum");

        // Overlapping tiers for one product make pricing ambiguous — the engine would
        // have to pick arbitrarily, so reject it at entry instead.
        var existing = await _priceLists.GetItemsAsync(priceListId);
        var clash = existing.FirstOrDefault(i =>
            i.ProductId == dto.ProductId && Overlaps(i, dto.MinQuantity, dto.MaxQuantity));
        if (clash is not null)
            throw new InvalidOperationException(
                "That product already has a price covering this quantity range on this list");

        var item = new PriceListItem
        {
            Id = Guid.NewGuid(),
            PriceListId = priceListId,
            ProductId = dto.ProductId,
            UnitOfMeasure = dto.UnitOfMeasure,
            UnitPrice = dto.UnitPrice,
            MinQuantity = dto.MinQuantity,
            MaxQuantity = dto.MaxQuantity,
            ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
            ValidTo = dto.ValidTo,
            IsActive = true,
            CompanyId = list.CompanyId,
            BranchId = list.BranchId,
            BusinessUnitId = list.BusinessUnitId,
        };

        await _priceLists.AddItemAsync(item);
        _logger.LogInformation("Price list {ListId} line added for product {ProductId}", priceListId, dto.ProductId);

        return MapItem(item);
    }

    public async Task<PriceListItemDto> UpdateItemAsync(Guid itemId, UpdatePriceListItemDto dto)
    {
        var item = await _priceLists.GetItemAsync(itemId)
            ?? throw new InvalidOperationException("Price list line not found");

        if (dto.UnitPrice is { } price)
        {
            if (price < 0) throw new InvalidOperationException("Unit price cannot be negative");
            item.UnitPrice = price;
        }

        if (dto.UnitOfMeasure is not null) item.UnitOfMeasure = dto.UnitOfMeasure;
        if (dto.MinQuantity.HasValue) item.MinQuantity = dto.MinQuantity;
        if (dto.MaxQuantity.HasValue) item.MaxQuantity = dto.MaxQuantity;
        if (dto.ValidFrom.HasValue) item.ValidFrom = dto.ValidFrom.Value;
        if (dto.ValidTo.HasValue) item.ValidTo = dto.ValidTo;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;

        if (item.MinQuantity is { } lo && item.MaxQuantity is { } hi && lo > hi)
            throw new InvalidOperationException("Minimum quantity cannot exceed the maximum");

        item.UpdatedAt = DateTime.UtcNow;
        await _priceLists.SaveChangesAsync();

        return MapItem(item);
    }

    public async Task DeleteItemAsync(Guid itemId)
    {
        var item = await _priceLists.GetItemAsync(itemId)
            ?? throw new InvalidOperationException("Price list line not found");

        await _priceLists.RemoveItemAsync(item);
        _logger.LogInformation("Price list line removed: {ItemId}", itemId);
    }

    /// <summary>Do two quantity bands cover any of the same quantities? Null = unbounded.</summary>
    private static bool Overlaps(PriceListItem existing, decimal? min, decimal? max)
    {
        var aLo = existing.MinQuantity ?? 0m;
        var aHi = existing.MaxQuantity ?? decimal.MaxValue;
        var bLo = min ?? 0m;
        var bHi = max ?? decimal.MaxValue;
        return aLo <= bHi && bLo <= aHi;
    }

    internal static PriceListItemDto MapItem(PriceListItem i) => new()
    {
        Id = i.Id,
        PriceListId = i.PriceListId,
        ProductId = i.ProductId,
        UnitOfMeasure = i.UnitOfMeasure,
        UnitPrice = i.UnitPrice,
        MinQuantity = i.MinQuantity,
        MaxQuantity = i.MaxQuantity,
        ValidFrom = i.ValidFrom,
        ValidTo = i.ValidTo,
        IsActive = i.IsActive,
    };

    // Mapping

    internal static PriceListDto MapToDto(PriceList p) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, ListType = p.Type,
        CurrencyCode = p.CurrencyCode, IsActive = p.IsActive,
        ValidFrom = p.ValidFrom, ValidTo = p.ValidTo,
        ItemCount = p.Items?.Count ?? 0,
    };
}
