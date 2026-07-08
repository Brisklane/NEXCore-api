using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor / Supplier master record.
/// Aligned with SAP Business Partner (LFA1), Oracle Supplier, Dynamics Vendor, Odoo res.partner (supplier).
/// </summary>
public class Vendor : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated vendor code (e.g., V-00001).</summary>
    public string VendorNumber { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public VendorType Type { get; set; } = VendorType.Company;
    public VendorStatus Status { get; set; } = VendorStatus.PendingApproval;
    public VendorOnboardingStatus OnboardingStatus { get; set; } = VendorOnboardingStatus.Draft;

    // ─── Registration ──────────────────────────────────────────────────────────
    public string? TaxRegistrationNumber { get; set; }
    public string? CompanyRegistrationNumber { get; set; }
    public string? VATNumber { get; set; }
    public string? Website { get; set; }

    // ─── Classification ────────────────────────────────────────────────────────
    public Guid? VendorCategoryId { get; set; }
    public VendorCategory? VendorCategory { get; set; }

    // ─── Contact Snapshot (primary contact for fast reads) ─────────────────────
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PrimaryMobile { get; set; }

    // ─── Procurement Defaults ──────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public int LeadTimeDays { get; set; } = 7;
    public decimal CreditLimit { get; set; }
    public bool IsPreferredVendor { get; set; }

    // ─── Financial ─────────────────────────────────────────────────────────────
    /// <summary>Cross-module reference to Accounting AP subledger account. ID only.</summary>
    public Guid? ApLedgerAccountId { get; set; }
    public SubledgerType SubledgerType { get; set; } = SubledgerType.AccountsPayable;

    // ─── Performance (computed / updated periodically) ─────────────────────────
    public decimal? OverallRating { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }
    public decimal? QualityScore { get; set; }

    // ─── Blocking ──────────────────────────────────────────────────────────────
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime? BlockedAt { get; set; }
    public Guid? BlockedByUserId { get; set; }

    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorContact> Contacts { get; set; } = [];
    public ICollection<VendorAddress> Addresses { get; set; } = [];
    public ICollection<VendorBankAccount> BankAccounts { get; set; } = [];
    public ICollection<VendorPerformance> PerformanceRecords { get; set; } = [];
    public ICollection<VendorPricelist> Pricelists { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = [];
    public ICollection<PurchaseContract> Contracts { get; set; } = [];
    public ICollection<VendorPayment> Payments { get; set; } = [];
}
