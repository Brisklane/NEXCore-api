using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(CrmDbContext db, ILogger<OrderService> logger) { _db = db; _logger = logger; }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid userId)
    {
        var count = await _db.Orders.CountAsync() + 1;
        var lineItems = dto.LineItems.Select(l =>
        {
            var disc = l.Discount ?? 0m;
            var unitAfterDisc = l.UnitPrice * (1 - disc / 100);
            return new OrderLineItem
            {
                ProductId = l.ProductId, PricebookEntryId = l.PricebookEntryId,
                Quantity = l.Quantity, UnitPrice = l.UnitPrice, ListPrice = l.UnitPrice,
                Discount = l.Discount, TotalPrice = l.Quantity * unitAfterDisc,
                Description = l.Description, SortOrder = l.SortOrder,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            };
        }).ToList();

        var subtotal = lineItems.Sum(x => x.TotalPrice);
        var grandTotal = subtotal + (dto.Tax ?? 0) + (dto.ShippingAndHandling ?? 0);

        var entity = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{count:D4}",
            OrderName = dto.OrderName, AccountId = dto.AccountId,
            ContractId = dto.ContractId, QuoteId = dto.QuoteId, PricebookId = dto.PricebookId,
            Status = OrderStatus.Draft, OrderStartDate = dto.OrderStartDate, OrderEndDate = dto.OrderEndDate,
            Subtotal = subtotal, Tax = dto.Tax, ShippingAndHandling = dto.ShippingAndHandling,
            GrandTotal = grandTotal, BillingStreet = dto.BillingStreet, BillingCity = dto.BillingCity,
            BillingState = dto.BillingState, BillingPostalCode = dto.BillingPostalCode,
            BillingCountry = dto.BillingCountry, ShippingStreet = dto.ShippingStreet,
            ShippingCity = dto.ShippingCity, ShippingState = dto.ShippingState,
            ShippingPostalCode = dto.ShippingPostalCode, ShippingCountry = dto.ShippingCountry,
            OwnerId = dto.OwnerId ?? userId, Description = dto.Description,
            LineItems = lineItems, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Orders.AsNoTracking().Include(x => x.LineItems)
            .Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(IEnumerable<OrderDto> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _db.Orders.AsNoTracking().Include(x => x.Account).Where(x => !x.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), total);
    }

    public async Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _db.Orders.AsNoTracking().Include(x => x.LineItems)
            .Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto, Guid userId)
    {
        var entity = await _db.Orders.Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Order not found");

        entity.OrderName = dto.OrderName; entity.AccountId = dto.AccountId;
        entity.ContractId = dto.ContractId; entity.QuoteId = dto.QuoteId;
        entity.OrderStartDate = dto.OrderStartDate; entity.OrderEndDate = dto.OrderEndDate;
        entity.Tax = dto.Tax; entity.ShippingAndHandling = dto.ShippingAndHandling;
        entity.BillingStreet = dto.BillingStreet; entity.BillingCity = dto.BillingCity;
        entity.BillingState = dto.BillingState; entity.BillingPostalCode = dto.BillingPostalCode;
        entity.BillingCountry = dto.BillingCountry; entity.ShippingStreet = dto.ShippingStreet;
        entity.ShippingCity = dto.ShippingCity; entity.ShippingState = dto.ShippingState;
        entity.ShippingPostalCode = dto.ShippingPostalCode; entity.ShippingCountry = dto.ShippingCountry;
        entity.OwnerId = dto.OwnerId; entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;

        foreach (var li in entity.LineItems) { li.IsDeleted = true; li.DeletedAt = DateTime.UtcNow; }
        foreach (var l in dto.LineItems)
        {
            var disc = l.Discount ?? 0m;
            var unitAfterDisc = l.UnitPrice * (1 - disc / 100);
            entity.LineItems.Add(new OrderLineItem
            {
                ProductId = l.ProductId, Quantity = l.Quantity, UnitPrice = l.UnitPrice,
                ListPrice = l.UnitPrice, Discount = l.Discount, TotalPrice = l.Quantity * unitAfterDisc,
                Description = l.Description, SortOrder = l.SortOrder,
                CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
            });
        }
        entity.Subtotal = entity.LineItems.Where(x => !x.IsDeleted).Sum(x => x.TotalPrice);
        entity.GrandTotal = entity.Subtotal + (dto.Tax ?? 0) + (dto.ShippingAndHandling ?? 0);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Order not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static OrderDto MapToDto(Order e) => new()
    {
        Id = e.Id, OrderNumber = e.OrderNumber, OrderName = e.OrderName,
        AccountId = e.AccountId, AccountName = e.Account?.AccountName,
        ContractId = e.ContractId, QuoteId = e.QuoteId, PricebookId = e.PricebookId,
        Status = e.Status, OrderStartDate = e.OrderStartDate, OrderEndDate = e.OrderEndDate,
        Subtotal = e.Subtotal, Tax = e.Tax, ShippingAndHandling = e.ShippingAndHandling,
        GrandTotal = e.GrandTotal, BillingStreet = e.BillingStreet, BillingCity = e.BillingCity,
        BillingState = e.BillingState, BillingPostalCode = e.BillingPostalCode,
        BillingCountry = e.BillingCountry, ShippingStreet = e.ShippingStreet,
        ShippingCity = e.ShippingCity, ShippingState = e.ShippingState,
        ShippingPostalCode = e.ShippingPostalCode, ShippingCountry = e.ShippingCountry,
        OwnerId = e.OwnerId, Description = e.Description, CreatedAt = e.CreatedAt,
        LineItems = e.LineItems?.Where(x => !x.IsDeleted).Select(l => new OrderLineItemDto
        {
            Id = l.Id, ProductId = l.ProductId, Quantity = l.Quantity,
            UnitPrice = l.UnitPrice, ListPrice = l.ListPrice, Discount = l.Discount,
            TotalPrice = l.TotalPrice, Description = l.Description,
            QuantityShipped = l.QuantityShipped, QuantityReturned = l.QuantityReturned, SortOrder = l.SortOrder
        }).ToList() ?? []
    };
}
