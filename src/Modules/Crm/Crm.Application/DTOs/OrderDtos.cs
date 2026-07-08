using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? OrderName { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? PricebookId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime OrderStartDate { get; set; }
    public DateTime? OrderEndDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? Tax { get; set; }
    public decimal? ShippingAndHandling { get; set; }
    public decimal GrandTotal { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? ShippingStreet { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderLineItemDto> LineItems { get; set; } = new();
}

public class CreateOrderDto
{
    public string? OrderName { get; set; }
    public Guid AccountId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? PricebookId { get; set; }
    public DateTime OrderStartDate { get; set; }
    public DateTime? OrderEndDate { get; set; }
    public decimal? Tax { get; set; }
    public decimal? ShippingAndHandling { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? ShippingStreet { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Description { get; set; }
    public List<CreateOrderLineItemDto> LineItems { get; set; } = new();
}

public class UpdateOrderDto : CreateOrderDto { }

public class OrderLineItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Description { get; set; }
    public decimal? QuantityShipped { get; set; }
    public decimal? QuantityReturned { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateOrderLineItemDto
{
    public Guid ProductId { get; set; }
    public Guid? PricebookEntryId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}
