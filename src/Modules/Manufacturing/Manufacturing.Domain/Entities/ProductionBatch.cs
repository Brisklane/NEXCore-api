using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Batch - dedicated batch/lot master for manufactured goods.
/// Supports full traceability, expiry tracking, and batch status management.
/// Critical for pharma, food, chemicals, and automotive industries.
/// SAP Equivalent: Batch Master (MSC1N / MCHB) | Oracle Equivalent: Lot Number
/// </summary>
public class ProductionBatch : BaseEntity
{
    /// <summary>
    /// Production Order that created this batch
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Finished product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Unique batch/lot number
    /// </summary>
    public required string BatchNumber { get; set; }

    /// <summary>
    /// Date of manufacture
    /// </summary>
    public DateTime ManufacturingDate { get; set; }

    /// <summary>
    /// Expiry/best before date (if applicable)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Re-test / re-inspection date
    /// </summary>
    public DateTime? ReTestDate { get; set; }

    /// <summary>
    /// Total quantity in this batch
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public required string UnitOfMeasure { get; set; }

    /// <summary>
    /// Current batch status
    /// </summary>
    public required string Status { get; set; } = "Active"; // Active, Restricted, Blocked, Expired, Consumed

    /// <summary>
    /// Warehouse where this batch is stored (External - from Inventory module)
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Certificate of Analysis number (for regulated industries)
    /// </summary>
    public string? CertificateOfAnalysis { get; set; }

    /// <summary>
    /// Vendor batch number if raw materials were traced (for backward traceability)
    /// </summary>
    public string? VendorBatchNumber { get; set; }

    /// <summary>
    /// Indicates if this batch has passed quality inspection
    /// </summary>
    public bool QualityApproved { get; set; } = false;

    /// <summary>
    /// Inspection reference that approved this batch
    /// </summary>
    public Guid? InspectionId { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public Inspection? Inspection { get; set; }
}
