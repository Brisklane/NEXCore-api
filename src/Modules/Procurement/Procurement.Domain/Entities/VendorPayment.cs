using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Outgoing payment to a vendor covering one or more invoices.
/// Aligned with SAP F110 (Automatic Payment), Oracle Quick Payment,
/// Dynamics Vendor Payment Journal, Odoo account.payment.
/// </summary>
public class VendorPayment : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated payment number (e.g., PAY-2024-00001).</summary>
    public string PaymentNumber { get; set; } = string.Empty;

    // ─── Vendor ────────────────────────────────────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string? VendorName { get; set; }

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public DateTime? ValueDate { get; set; }
    public DateTime? ClearedAt { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public VendorPaymentStatus Status { get; set; } = VendorPaymentStatus.Draft;

    // ─── Method ────────────────────────────────────────────────────────────────
    public VendorPaymentMethod PaymentMethod { get; set; } = VendorPaymentMethod.BankTransfer;

    // ─── Bank ──────────────────────────────────────────────────────────────────
    /// <summary>Our company bank account. Cross-module ref to Accounting. ID only.</summary>
    public Guid? CompanyBankAccountId { get; set; }
    public Guid? VendorBankAccountId { get; set; }
    public VendorBankAccount? VendorBankAccount { get; set; }

    // ─── Currency & Amount ─────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal TotalAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal UnallocatedAmount { get; set; }

    // ─── References ────────────────────────────────────────────────────────────
    public string? BankReferenceNumber { get; set; }
    public string? CheckNumber { get; set; }
    public string? TransactionReference { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>Cross-module reference to AP payment Journal Entry. ID only.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    /// <summary>Cross-module reference to Accounting FiscalPeriod. ID only.</summary>
    public Guid? FiscalPeriodId { get; set; }

    /// <summary>
    /// Withholding tax deducted from this payment at source (WHT/TDS).
    /// Applicable in jurisdictions such as Pakistan, India, Kenya, etc.
    /// Net payment to vendor = TotalAmount - WithholdingTaxAmount.
    /// </summary>
    public decimal WithholdingTaxAmount { get; set; }

    /// <summary>
    /// Cross-module reference to the WHT liability GL account in Accounting.
    /// Entry: Dr Accounts Payable, Cr Bank (net), Cr WHT Payable.
    /// ID only.
    /// </summary>
    public Guid? WithholdingTaxLedgerAccountId { get; set; }

    // ─── Approval ──────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorPaymentLine> Allocations { get; set; } = [];
}
