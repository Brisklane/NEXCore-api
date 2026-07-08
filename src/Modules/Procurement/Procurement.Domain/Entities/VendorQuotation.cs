using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor's response / bid to an RFQ.
/// Aligned with SAP Quotation (ME47), Oracle Supplier Quote, Odoo purchase.order (quotation).
/// </summary>
public class VendorQuotation : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    public string QuotationNumber { get; set; } = string.Empty;
    /// <summary>Vendor's own reference number for this quotation.</summary>
    public string? VendorQuotationReference { get; set; }

    // ─── Links ─────────────────────────────────────────────────────────────────
    public Guid RFQId { get; set; }
    public RequestForQuotation RFQ { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    // ─── Financials ────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal SubTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // ─── Commercial Terms ──────────────────────────────────────────────────────
    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;
    public Incoterm? Incoterm { get; set; }
    public string? IncotermLocation { get; set; }
    public int DeliveryLeadTimeDays { get; set; }

    // ─── Evaluation (filled during comparison) ─────────────────────────────────
    public decimal? TechnicalScore { get; set; }
    public decimal? CommercialScore { get; set; }
    public decimal? OverallScore { get; set; }
    public bool IsRecommended { get; set; }

    public string? RejectionReason { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorQuotationLine> Lines { get; set; } = [];
}
