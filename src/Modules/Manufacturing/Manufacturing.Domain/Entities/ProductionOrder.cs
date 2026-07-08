using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Order - main production transaction representing a manufacturing request
/// </summary>
public class ProductionOrder : BaseEntity
{
    /// <summary>
    /// Unique production order number
    /// </summary>
    public required string OrderNumber { get; set; }

    /// <summary>
    /// Product to be manufactured (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Bill of Materials reference
    /// </summary>
    public Guid BillOfMaterialId { get; set; }

    /// <summary>
    /// Routing reference
    /// </summary>
    public Guid RoutingId { get; set; }

    /// <summary>
    /// Planned Order reference (optional)
    /// </summary>
    public Guid? PlannedOrderId { get; set; }

    /// <summary>
    /// Planned quantity to produce
    /// </summary>
    public decimal QuantityPlanned { get; set; }

    /// <summary>
    /// Actually produced quantity
    /// </summary>
    public decimal QuantityProduced { get; set; } = 0;

    /// <summary>
    /// Rejected/Scrap quantity
    /// </summary>
    public decimal QuantityRejected { get; set; } = 0;

    /// <summary>
    /// Current status of the production order
    /// </summary>
    public required string Status { get; set; } = "Draft"; // Draft, Planned, Released, InProgress, QAPending, Completed, Closed, Cancelled

    /// <summary>
    /// Scheduled start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Scheduled end date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Due date for completion
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// User who created the order
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public BillOfMaterial? BillOfMaterial { get; set; }
    public Routing? Routing { get; set; }
    public PlannedOrder? PlannedOrder { get; set; }
    public ICollection<MaterialIssue>? MaterialIssues { get; set; }
    public WorkInProgress? WorkInProgress { get; set; }
    public ICollection<Inspection>? Inspections { get; set; }
    public ICollection<FinishedGoodsReceipt>? FinishedGoodsReceipts { get; set; }
    public CostEntry? CostEntry { get; set; }
    public ICollection<ProductionOrderOperation>? Operations { get; set; }
    public ICollection<ProductionOrderComponent>? Components { get; set; }
    public ICollection<ProductionSchedule>? Schedules { get; set; }
    public ICollection<SubContractOrder>? SubContractOrders { get; set; }
    public ProductionVariance? Variance { get; set; }
    public ICollection<ProductionBatch>? Batches { get; set; }
    public ICollection<MachineDowntime>? MachineDowntimes { get; set; }
    public ICollection<ReworkOrder>? ReworkOrders { get; set; }
}