using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Subcontract Order - tracks manufacturing operations outsourced to external vendors.
/// Materials are sent to the vendor and finished/processed goods are received back.
/// SAP Equivalent: Subcontracting PO (ME21N) | Oracle Equivalent: Outside Processing Operation
/// </summary>
public class SubContractOrder : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Routing operation being subcontracted
    /// </summary>
    public Guid ProductionOrderOperationId { get; set; }

    /// <summary>
    /// Vendor reference (External - from Procurement module)
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// Purchase Order reference created for this subcontract (External - from Procurement module)
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    /// <summary>
    /// Quantity sent to vendor for processing
    /// </summary>
    public decimal QuantitySent { get; set; }

    /// <summary>
    /// Quantity received back from vendor after processing
    /// </summary>
    public decimal QuantityReceived { get; set; } = 0;

    /// <summary>
    /// Quantity rejected upon return from vendor
    /// </summary>
    public decimal QuantityRejected { get; set; } = 0;

    /// <summary>
    /// Timestamp when materials were sent to vendor
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Expected return date from vendor
    /// </summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>
    /// Actual return date from vendor
    /// </summary>
    public DateTime? ActualReturnDate { get; set; }

    /// <summary>
    /// Agreed unit cost for the subcontract operation
    /// </summary>
    public decimal? UnitCost { get; set; }

    /// <summary>
    /// Total subcontract cost (QuantityReceived * UnitCost)
    /// </summary>
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Current status of the subcontract order
    /// </summary>
    public required string Status { get; set; } = "Draft"; // Draft, Sent, PartiallyReceived, Received, Closed, Cancelled

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public ProductionOrderOperation? ProductionOrderOperation { get; set; }
}
