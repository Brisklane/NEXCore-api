using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Vendor price catalogue / price list.
/// Used to auto-populate unit prices when creating PO lines.
/// Aligned with SAP Info Record (ME11), Oracle Approved Supplier List pricing, Odoo pricelist.
/// </summary>
public class VendorPricelist : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public required string Name { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public bool IsDefault { get; set; }

    public string? Notes { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<VendorPricelistItem> Items { get; set; } = [];
}
