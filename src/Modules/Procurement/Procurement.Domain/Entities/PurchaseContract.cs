using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Long-term purchase agreement / blanket order with a vendor.
/// Aligned with SAP Outline Agreement (ME31K/ME31L), Oracle Blanket PO,
/// Dynamics Purchase Agreement, Odoo purchase.agreement.
/// POs drawn against this contract reference it for price/term compliance.
/// </summary>
public class PurchaseContract : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Auto-generated contract number (e.g., PC-2024-00001).</summary>
    public string ContractNumber { get; set; } = string.Empty;
    public required string Title { get; set; }

    // ─── Vendor ────────────────────────────────────────────────────────────────
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;
    public string? VendorName { get; set; }

    // ─── Type & Status ─────────────────────────────────────────────────────────
    public PurchaseContractType ContractType { get; set; } = PurchaseContractType.FrameworkAgreement;
    public PurchaseContractStatus Status { get; set; } = PurchaseContractStatus.Draft;

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }

    // ─── Auto-Renewal ──────────────────────────────────────────────────────────
    public bool AutoRenew { get; set; }
    public int RenewalNoticeDays { get; set; }
    public int? RenewalDurationMonths { get; set; }

    // ─── Financials ────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal? MaximumContractValue { get; set; }
    public decimal CommittedValue { get; set; }
    public decimal UsedValue { get; set; }
    public decimal RemainingValue => (MaximumContractValue ?? 0) - UsedValue;

    // ─── Payment & Delivery ────────────────────────────────────────────────────
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }

    // ─── Approvals ─────────────────────────────────────────────────────────────
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? TerminationReason { get; set; }
    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<PurchaseContractLine> Lines { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
}
