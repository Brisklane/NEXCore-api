using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Sales Agreement / Blanket Order / Framework Agreement.
/// Customer commits to buying a certain volume over a period.
/// Aligned with SAP Scheduling Agreement, Oracle Blanket Sales Agreement, Dynamics Trade Agreement.
/// </summary>
public class SalesAgreement : BaseEntity
{
    public string AgreementNumber { get; set; } = string.Empty;
    public string AgreementName { get; set; } = string.Empty;

    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>Snapshot of contact name for fast reads.</summary>
    public string? ContactName { get; set; }

    public SalesAgreementStatus Status { get; set; } = SalesAgreementStatus.Draft;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>ISO 4217 currency code, e.g., "USD". No cross-module FK.</summary>
    public string CurrencyCode { get; set; } = "USD";

    public PaymentTerms PaymentTerms { get; set; } = PaymentTerms.Net30;

    /// <summary>Total committed amount across the agreement period.</summary>
    public decimal CommittedAmount { get; set; }
    public decimal ReleasedAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Notes { get; set; }

    public ICollection<SalesAgreementLine> Lines { get; set; } = new List<SalesAgreementLine>();
    public ICollection<SalesOrder> ReleasedOrders { get; set; } = new List<SalesOrder>();
}
