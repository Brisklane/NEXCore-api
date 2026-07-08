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

    // Mapping

    internal static PriceListDto MapToDto(PriceList p) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, ListType = p.Type,
        CurrencyCode = p.CurrencyCode, IsActive = p.IsActive,
        ValidFrom = p.ValidFrom, ValidTo = p.ValidTo,
        ItemCount = p.Items?.Count ?? 0,
    };
}
