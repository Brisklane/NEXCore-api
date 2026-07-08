using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Landed Cost document — captures additional procurement costs (freight, duties,
/// insurance) and allocates them to received goods for accurate inventory valuation.
///
/// Aligned with:
///   SAP  — Planned Delivery Costs / Condition Types on PO (KOMV)
///   Oracle — Oracle Landed Cost Management
///   Dynamics — Miscellaneous charges on purchase
///   Odoo — stock.landed.cost
///
/// Flow: Post GRN → Create LandedCost → Add cost lines → Validate →
///       System allocates amounts to GRN lines → Posts Inventory + AP journal entries.
/// </summary>
public class LandedCost : BaseEntity
{
    // ─── Identity ──────────────────────────────────────────────────────────────
    public string LandedCostNumber { get; set; } = string.Empty;

    // ─── Vendor ────────────────────────────────────────────────────────────────
    /// <summary>Vendor charging the additional cost (e.g. freight forwarder). May differ from PO vendor.</summary>
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    // ─── Status ────────────────────────────────────────────────────────────────
    public LandedCostStatus Status { get; set; } = LandedCostStatus.Draft;

    // ─── Dates ─────────────────────────────────────────────────────────────────
    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;
    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }

    // ─── Totals ────────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal TotalLandedCostAmount { get; set; }

    // ─── Accounting Integration ────────────────────────────────────────────────
    /// <summary>Cross-module reference to posted Journal Entry in Accounting. ID only.</summary>
    public Guid? AccountingJournalEntryId { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    /// <summary>Cost components: freight, duty, insurance, etc.</summary>
    public ICollection<LandedCostLine> CostLines { get; set; } = [];

    /// <summary>Goods receipts this landed cost is applied to.</summary>
    public ICollection<LandedCostGoodsReceipt> GoodsReceipts { get; set; } = [];

    /// <summary>Allocation of each cost to individual GRN lines.</summary>
    public ICollection<LandedCostAllocation> Allocations { get; set; } = [];
}
