using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public class QuoteDto
{
    public Guid Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public string QuoteName { get; set; } = string.Empty;
    public Guid DealId { get; set; }
    public Guid? PricebookId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? AccountId { get; set; }
    public QuoteStatus Status { get; set; }
    public bool IsSyncing { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? Discount { get; set; }
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
    public string? Description { get; set; }
    public string? TermsAndConditions { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QuoteLineItemDto> LineItems { get; set; } = new();
}

public class CreateQuoteDto
{
    public string QuoteName { get; set; } = string.Empty;
    public Guid DealId { get; set; }
    public Guid? PricebookId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? AccountId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal? Discount { get; set; }
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
    public string? Description { get; set; }
    public string? TermsAndConditions { get; set; }
    public Guid? OwnerId { get; set; }
    public List<CreateQuoteLineItemDto> LineItems { get; set; } = new();
}

public class UpdateQuoteDto : CreateQuoteDto { }

public class QuoteLineItemDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}

public class CreateQuoteLineItemDto
{
    public Guid ProductId { get; set; }
    public Guid? PricebookEntryId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
}
