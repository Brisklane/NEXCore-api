using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

public class VendorDebitNoteDto
{
    public Guid Id { get; set; }
    public string DebitNoteNumber { get; set; } = string.Empty;
    public Guid PurchaseReturnId { get; set; }
    public string? PurchaseReturnNumber { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public DateTime DebitNoteDate { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? SettledAt { get; set; }

    public DebitNoteStatus Status { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public decimal OutstandingAmount { get; set; }

    public string? Notes { get; set; }
    public List<VendorDebitNoteLineDto> Lines { get; set; } = [];
}

public class VendorDebitNoteLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ReturnLineId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A debit note is raised from a posted purchase return; lines are derived from the return.</summary>
public class CreateVendorDebitNoteDto
{
    public required Guid PurchaseReturnId { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public DateTime DebitNoteDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
