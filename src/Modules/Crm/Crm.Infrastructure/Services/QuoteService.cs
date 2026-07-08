using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class QuoteService : IQuoteService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(CrmDbContext db, ILogger<QuoteService> logger) { _db = db; _logger = logger; }

    public async Task<QuoteDto> CreateAsync(CreateQuoteDto dto, Guid userId)
    {
        var count = await _db.Quotes.CountAsync() + 1;
        var lineItems = dto.LineItems.Select(l =>
        {
            var disc = l.Discount ?? 0m;
            var unitAfterDisc = l.UnitPrice * (1 - disc / 100);
            return new QuoteLineItem
            {
                ProductId = l.ProductId, PricebookEntryId = l.PricebookEntryId,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, ListPrice = l.UnitPrice,
                Discount = l.Discount, TotalPrice = l.Quantity * unitAfterDisc,
                Description = l.Description, SortOrder = l.SortOrder,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        var subtotal = lineItems.Sum(x => x.TotalPrice);
        var grandTotal = subtotal - (dto.Discount ?? 0) + (dto.Tax ?? 0) + (dto.ShippingAndHandling ?? 0);

        var entity = new Quote
        {
            QuoteNumber = $"Q-{DateTime.UtcNow:yyyyMMdd}-{count:D4}",
            QuoteName = dto.QuoteName, DealId = dto.DealId, PricebookId = dto.PricebookId,
            ContactId = dto.ContactId, AccountId = dto.AccountId,
            Status = QuoteStatus.Draft, ExpirationDate = dto.ExpirationDate,
            Subtotal = subtotal, Discount = dto.Discount, Tax = dto.Tax,
            ShippingAndHandling = dto.ShippingAndHandling, GrandTotal = grandTotal,
            BillingStreet = dto.BillingStreet, BillingCity = dto.BillingCity,
            BillingState = dto.BillingState, BillingPostalCode = dto.BillingPostalCode,
            BillingCountry = dto.BillingCountry, ShippingStreet = dto.ShippingStreet,
            ShippingCity = dto.ShippingCity, ShippingState = dto.ShippingState,
            ShippingPostalCode = dto.ShippingPostalCode, ShippingCountry = dto.ShippingCountry,
            Description = dto.Description, TermsAndConditions = dto.TermsAndConditions,
            OwnerId = dto.OwnerId ?? userId, LineItems = lineItems,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Quotes.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<QuoteDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Quotes.AsNoTracking().Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(IEnumerable<QuoteDto> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _db.Quotes.AsNoTracking().Where(x => !x.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), total);
    }

    public async Task<IEnumerable<QuoteDto>> GetByDealIdAsync(Guid dealId)
    {
        var items = await _db.Quotes.AsNoTracking().Include(x => x.LineItems)
            .Where(x => x.DealId == dealId && !x.IsDeleted).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto, Guid userId)
    {
        var entity = await _db.Quotes.Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Quote not found");

        entity.QuoteName = dto.QuoteName; entity.DealId = dto.DealId;
        entity.PricebookId = dto.PricebookId; entity.ContactId = dto.ContactId;
        entity.AccountId = dto.AccountId; entity.ExpirationDate = dto.ExpirationDate;
        entity.Discount = dto.Discount; entity.Tax = dto.Tax;
        entity.ShippingAndHandling = dto.ShippingAndHandling;
        entity.BillingStreet = dto.BillingStreet; entity.BillingCity = dto.BillingCity;
        entity.BillingState = dto.BillingState; entity.BillingPostalCode = dto.BillingPostalCode;
        entity.BillingCountry = dto.BillingCountry; entity.ShippingStreet = dto.ShippingStreet;
        entity.ShippingCity = dto.ShippingCity; entity.ShippingState = dto.ShippingState;
        entity.ShippingPostalCode = dto.ShippingPostalCode; entity.ShippingCountry = dto.ShippingCountry;
        entity.Description = dto.Description; entity.TermsAndConditions = dto.TermsAndConditions;
        entity.OwnerId = dto.OwnerId;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;

        // Replace line items
        foreach (var li in entity.LineItems) { li.IsDeleted = true; li.DeletedAt = DateTime.UtcNow; }
        foreach (var l in dto.LineItems)
        {
            var disc = l.Discount ?? 0m;
            var unitAfterDisc = l.UnitPrice * (1 - disc / 100);
            entity.LineItems.Add(new QuoteLineItem
            {
                ProductId = l.ProductId, PricebookEntryId = l.PricebookEntryId,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, ListPrice = l.UnitPrice,
                Discount = l.Discount, TotalPrice = l.Quantity * unitAfterDisc,
                Description = l.Description, SortOrder = l.SortOrder,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            });
        }
        entity.Subtotal = entity.LineItems.Where(x => !x.IsDeleted).Sum(x => x.TotalPrice);
        entity.GrandTotal = entity.Subtotal - (dto.Discount ?? 0) + (dto.Tax ?? 0) + (dto.ShippingAndHandling ?? 0);

        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Quotes.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Quote not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static QuoteDto MapToDto(Quote e) => new()
    {
        Id = e.Id, QuoteNumber = e.QuoteNumber, QuoteName = e.QuoteName,
        DealId = e.DealId, PricebookId = e.PricebookId, ContactId = e.ContactId, AccountId = e.AccountId,
        Status = e.Status, IsSyncing = e.IsSyncing, ExpirationDate = e.ExpirationDate,
        Subtotal = e.Subtotal, Discount = e.Discount, Tax = e.Tax,
        ShippingAndHandling = e.ShippingAndHandling, GrandTotal = e.GrandTotal,
        BillingStreet = e.BillingStreet, BillingCity = e.BillingCity, BillingState = e.BillingState,
        BillingPostalCode = e.BillingPostalCode, BillingCountry = e.BillingCountry,
        ShippingStreet = e.ShippingStreet, ShippingCity = e.ShippingCity, ShippingState = e.ShippingState,
        ShippingPostalCode = e.ShippingPostalCode, ShippingCountry = e.ShippingCountry,
        Description = e.Description, TermsAndConditions = e.TermsAndConditions,
        OwnerId = e.OwnerId, CreatedAt = e.CreatedAt,
        LineItems = e.LineItems?.Where(x => !x.IsDeleted).Select(l => new QuoteLineItemDto
        {
            Id = l.Id, QuoteId = l.QuoteId, ProductId = l.ProductId,
            Quantity = l.Quantity, UnitPrice = l.UnitPrice, ListPrice = l.ListPrice,
            Discount = l.Discount, TotalPrice = l.TotalPrice,
            Description = l.Description, SortOrder = l.SortOrder
        }).ToList() ?? []
    };
}
