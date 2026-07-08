using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// A single product line on a SalesOrder - used by ALL channels.
/// WPF POS, Android POS, and Customer App all write the same entity.
/// Business logic (price, discount, tax) is calculated once here.
/// </summary>
public class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int LineNumber { get; set; }

    // ? Product
    /// <summary>FK to Inventory Item - referenced by ID only.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Snapshot of product code at time of order (receipt / audit).</summary>
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public string? ProductImageUrl { get; set; }

    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }

    // ? Quantity
    public decimal OrderedQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal CancelledQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    // ? Warehouse
    /// <summary>
    /// Source warehouse for stock deduction.
    /// Defaults to PosStore.DefaultWarehouseId for POS channels.
    /// </summary>
    public Guid? WarehouseId { get; set; }

    // ? Pricing (calculated by promotion engine - same for all clients)
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal LineAmount { get; set; }

    // ── Tax ───────────────────────────────────────────────────────────────────
    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    // ── Margin (optional — set from Inventory cost price at order creation) ──
    /// <summary>Unit cost price at time of order (from Inventory). Null if not available.</summary>
    public decimal? CostPrice { get; set; }
    /// <summary>LineAmount minus (CostPrice × OrderedQuantity). Null when CostPrice is unknown.</summary>
    public decimal? MarginAmount { get; set; }
    /// <summary>MarginAmount / LineAmount × 100. Null when CostPrice is unknown.</summary>
    public decimal? MarginPercentage { get; set; }

    // ? Delivery
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? ConfirmedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }

    // ? Status
    public string LineStatus { get; set; } = "Open";

    public string? Notes { get; set; }

    // ? Navigation
    /// <summary>
    /// Add-ons / modifiers (extra cheese, sauce choice, gift wrap, etc.).
    /// Used by Customer App and food-service POS. Empty for standard retail POS.
    /// </summary>
    public ICollection<SalesOrderLineAddon> Addons { get; set; } = new List<SalesOrderLineAddon>();

    public ICollection<DeliveryLine> DeliveryLines { get; set; } = new List<DeliveryLine>();
    public ICollection<SalesInvoiceLine> InvoiceLines { get; set; } = new List<SalesInvoiceLine>();
}
