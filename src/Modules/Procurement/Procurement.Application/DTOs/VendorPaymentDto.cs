using Procurement.Domain.Enums;

namespace Procurement.Application.DTOs;

// ── Vendor Payment Read ───────────────────────────────────────────────────────

public class VendorPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid VendorId { get; set; }
    public string? VendorName { get; set; }

    public DateTime PaymentDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public DateTime? ClearedAt { get; set; }

    public VendorPaymentStatus Status { get; set; }
    public VendorPaymentMethod PaymentMethod { get; set; }

    public Guid? CompanyBankAccountId { get; set; }
    public Guid? VendorBankAccountId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }

    public string? BankReferenceNumber { get; set; }
    public string? CheckNumber { get; set; }
    public string? TransactionReference { get; set; }

    public Guid? AccountingJournalEntryId { get; set; }
    public Guid? FiscalPeriodId { get; set; }

    public decimal WithholdingTaxAmount { get; set; }
    public Guid? WithholdingTaxLedgerAccountId { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public List<VendorPaymentAllocationDto> Allocations { get; set; } = [];
}

public class VendorPaymentAllocationDto
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? VendorInvoiceNumber { get; set; }
    public decimal InvoiceTotalAmount { get; set; }
    public decimal InvoiceOutstandingAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal DiscountTaken { get; set; }
    public decimal WriteOffAmount { get; set; }
    public decimal FXGainLossAmount { get; set; }
    public Guid? FXGainLossLedgerAccountId { get; set; }
    public string? Notes { get; set; }
}

// ── Vendor Payment Create ─────────────────────────────────────────────────────

public class CreateVendorPaymentDto
{
    public Guid VendorId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValueDate { get; set; }

    public VendorPaymentMethod PaymentMethod { get; set; } = VendorPaymentMethod.BankTransfer;

    public Guid? CompanyBankAccountId { get; set; }
    public Guid? VendorBankAccountId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal TotalAmount { get; set; }

    public string? BankReferenceNumber { get; set; }
    public string? CheckNumber { get; set; }

    public Guid? FiscalPeriodId { get; set; }
    public decimal WithholdingTaxAmount { get; set; }
    public Guid? WithholdingTaxLedgerAccountId { get; set; }

    public string? Notes { get; set; }

    public List<CreatePaymentAllocationDto> Allocations { get; set; } = [];
}

public class CreatePaymentAllocationDto
{
    public Guid InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal DiscountTaken { get; set; }
    public decimal WriteOffAmount { get; set; }
    public Guid? FXGainLossLedgerAccountId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVendorPaymentDto
{
    public DateTime? PaymentDate { get; set; }
    public DateTime? ValueDate { get; set; }
    public Guid? VendorBankAccountId { get; set; }
    public string? BankReferenceNumber { get; set; }
    public string? CheckNumber { get; set; }
    public string? Notes { get; set; }
}
