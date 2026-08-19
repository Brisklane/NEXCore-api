using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A distribution order — the demand a visit, a van stop or a portal produced.
///
/// It is not a Sales order. A Sales order is a commercial agreement; this is a *fulfilment
/// instruction with a route attached*. It knows which beat it came from, which rep took it,
/// which van will carry it and which trip will deliver it, and it carries the scheme benefit
/// that was computed at the counter so the customer sees on the invoice what they were promised
/// at the door. <see cref="SalesOrderId"/> links to Sales when a company wants one order register.
/// </summary>
public class DistributionOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public OrderSource Source { get; set; } = OrderSource.FieldTerminal;
    public DistributionOrderKind Kind { get; set; } = DistributionOrderKind.Standard;
    public DistributionOrderStatus Status { get; set; } = DistributionOrderStatus.Draft;

    // ── Who it is for ────────────────────────────────────────────────────────
    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    // ── Where it came from ───────────────────────────────────────────────────
    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? TerritoryId { get; set; }

    // ── How it will be served ────────────────────────────────────────────────
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }

    /// <summary>Set on a drop-ship line set: the purchase order raised on the supplier.</summary>
    public Guid? LinkedPurchaseOrderId { get; set; }

    /// <summary>Sales module order this was mirrored into, when the tenant keeps one register.</summary>
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesInvoiceId { get; set; }

    // ── Dates ────────────────────────────────────────────────────────────────
    public DateTime OrderDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1;

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>Value of on-invoice scheme benefit, kept apart from ordinary discount for trade reporting.</summary>
    public decimal SchemeDiscountAmount { get; set; }

    /// <summary>Cost of goods given away free. Revenue-neutral, margin-negative, and always visible.</summary>
    public decimal FreeGoodsValue { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CostAmount { get; set; }

    /// <summary>Margin at order time, stored so a later price change does not rewrite history.</summary>
    public decimal MarginAmount { get; set; }

    // ── Control ──────────────────────────────────────────────────────────────
    public bool RequiresApproval { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? HoldReason { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? CancelReasonCodeId { get; set; }
    public string? CancelNote { get; set; }

    /// <summary>Credit was over limit and someone signed for it. The override record has the detail.</summary>
    public bool HasCreditOverride { get; set; }
    public Guid? CreditOverrideId { get; set; }

    public bool IsBackorderParent { get; set; }
    public Guid? BackorderOfOrderId { get; set; }

    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }

    /// <summary>Delivered quantity as a percentage of ordered — the fill rate for this order.</summary>
    public decimal FillRatePercent { get; set; }

    /// <summary>Client-supplied key. A double-tap in the market must never produce two orders.</summary>
    public string? IdempotencyKey { get; set; }

    public string? ExternalReference { get; set; }
    public string? Note { get; set; }

    public ICollection<DistributionOrderLine> Lines { get; set; } = [];
    public ICollection<OrderStatusEvent> StatusEvents { get; set; } = [];
    public ICollection<OrderApproval> Approvals { get; set; } = [];
}

/// <summary>
/// One SKU on an order.
///
/// The UoM handling is the subtle part: a salesman says "five cases" and the stock ledger needs
/// pieces. Both are stored — the quantity as entered plus the factor that converts it — so the
/// document reads the way the conversation went and the ledger still balances.
/// </summary>
public class DistributionOrderLine : BaseEntity
{
    public Guid OrderId { get; set; }
    public DistributionOrder? Order { get; set; }

    public int DisplayOrder { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    // ── Quantity, in the unit the customer speaks ────────────────────────────
    public string Uom { get; set; } = "PCS";

    /// <summary>Pieces per <see cref="Uom"/>. A case of 24 stores 24 here.</summary>
    public decimal UomFactor { get; set; } = 1;

    /// <summary>As entered, in <see cref="Uom"/>.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Quantity × factor. What the stock ledger moves.</summary>
    public decimal BaseQuantity { get; set; }

    public decimal AllocatedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal BackorderedQuantity { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────
    public decimal UnitPrice { get; set; }
    public decimal Mrp { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeDiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }
    public decimal MarginAmount { get; set; }

    /// <summary>
    /// True for a line the customer is not paying for — scheme free goods, a sample, a
    /// replacement. It still moves stock and still costs money, which is why it is a line.
    /// </summary>
    public bool IsFreeGoods { get; set; }

    /// <summary>Which scheme produced this line or its discount, for scheme cost reporting.</summary>
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }

    /// <summary>Which price rule won, so "why this price" has an answer at the counter.</summary>
    public Guid? PriceListLineId { get; set; }
    public PriceScope? PriceScope { get; set; }

    /// <summary>Set when the rep priced below the resolved price; requires authority or approval.</summary>
    public bool IsPriceOverridden { get; set; }
    public string? PriceOverrideReason { get; set; }

    public bool IsShortPicked { get; set; }
    public Guid? ShortPickReasonCodeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>Every status transition on an order, stamped with who and why.</summary>
public class OrderStatusEvent : BaseEntity
{
    public Guid OrderId { get; set; }
    public DistributionOrder? Order { get; set; }

    public DistributionOrderStatus FromStatus { get; set; }
    public DistributionOrderStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }

    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// One step of an order's approval chain.
///
/// Multi-step by design: a deep discount may need the ASM, and a credit exception the controller,
/// and neither should be able to sign for the other.
/// </summary>
public class OrderApproval : BaseEntity
{
    public Guid OrderId { get; set; }
    public DistributionOrder? Order { get; set; }

    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;

    /// <summary>What triggered this step — "discount above 10%", "credit limit exceeded".</summary>
    public string? TriggerReason { get; set; }

    public Guid? ApproverUserId { get; set; }
    public string? ApproverName { get; set; }
    public Guid? DelegatedToUserId { get; set; }

    public bool? IsApproved { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }

    /// <summary>Escalated after sitting unanswered past the SLA.</summary>
    public bool IsEscalated { get; set; }
    public DateTime? EscalatedAt { get; set; }
}

/// <summary>
/// Stock committed to an order line, down to the batch.
///
/// Soft on approval, hard on pick release. Keeping it as a row rather than a number on the line
/// is what lets one line be satisfied from three batches, which is normal the moment expiry
/// dates exist.
/// </summary>
public class StockAllocation : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid OrderLineId { get; set; }

    public Guid ItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public decimal Quantity { get; set; }
    public AllocationStrategy Strategy { get; set; } = AllocationStrategy.Fefo;

    /// <summary>Soft holds release on a timer; hard holds survive until picked or cancelled.</summary>
    public bool IsHardAllocation { get; set; }

    public DateTime AllocatedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Set when a picker took a different lot than FEFO nominated.</summary>
    public bool IsFefoOverridden { get; set; }
    public string? FefoOverrideReason { get; set; }
}

/// <summary>
/// A release of work to the warehouse floor: these orders, this strategy, now.
///
/// The wave is what turns a pile of orders into a shift's worth of walking. Grouping by route is
/// the distribution-specific part — pick in delivery sequence and the truck loads itself.
/// </summary>
public class PickWave : BaseEntity
{
    public string WaveNumber { get; set; } = string.Empty;

    public Guid? WarehouseId { get; set; }
    public PickStrategy Strategy { get; set; } = PickStrategy.Wave;

    public DateTime ReleasedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? ReleasedByUserId { get; set; }

    /// <summary>Grouping axis — the route, trip or cut-off this wave was built around.</summary>
    public Guid? RouteId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? CarrierCutOffAt { get; set; }

    public int Priority { get; set; }

    public int OrderCount { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public decimal TotalLines { get; set; }
    public decimal PickedLines { get; set; }
    public decimal ShortLines { get; set; }

    public bool IsClosed { get; set; }
    public string? Note { get; set; }

    public ICollection<PickTask> Tasks { get; set; } = [];
}

/// <summary>One picker's assignment inside a wave — a trolley, a zone, or a single order.</summary>
public class PickTask : BaseEntity
{
    public Guid WaveId { get; set; }
    public PickWave? Wave { get; set; }

    public string TaskNumber { get; set; } = string.Empty;
    public PickTaskStatus Status { get; set; } = PickTaskStatus.Released;

    public Guid? OrderId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }

    /// <summary>Aisle range for zone picking.</summary>
    public string? ZoneName { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LineCount { get; set; }
    public int ShortLineCount { get; set; }

    public ICollection<PickTaskLine> Lines { get; set; } = [];
}

/// <summary>
/// One pick: this quantity of this batch from this bin.
///
/// The nominated batch is stored next to the picked batch on purpose. FEFO is only real if
/// deviating from it leaves a trace, and a warehouse that overrides FEFO daily is a finding.
/// </summary>
public class PickTaskLine : BaseEntity
{
    public Guid TaskId { get; set; }
    public PickTask? Task { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? OrderLineId { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public Guid? BinId { get; set; }
    public string? BinCode { get; set; }

    /// <summary>What FEFO said to take.</summary>
    public Guid? NominatedBatchId { get; set; }
    public string? NominatedBatchNumber { get; set; }

    /// <summary>What was actually taken.</summary>
    public Guid? PickedBatchId { get; set; }
    public string? PickedBatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal RequiredQuantity { get; set; }
    public decimal PickedQuantity { get; set; }

    /// <summary>Sequence along the pick path, so the picker walks once.</summary>
    public int PickSequence { get; set; }

    public bool IsShort { get; set; }
    public Guid? ShortReasonCodeId { get; set; }
    public bool IsFefoOverridden { get; set; }
    public string? OverrideReason { get; set; }

    public DateTime? PickedAt { get; set; }
    public Guid? PickedByUserId { get; set; }
}

/// <summary>
/// A carton or pallet with a licence plate.
///
/// The plate is what makes a delivery dispute short: "carton LP0004821 was signed for" beats
/// "the driver says he left it".
/// </summary>
public class PackageUnit : BaseEntity
{
    public string LicencePlate { get; set; } = string.Empty;
    public PackageKind Kind { get; set; } = PackageKind.Carton;

    public Guid? OrderId { get; set; }
    public Guid? WaveId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? DispatchId { get; set; }

    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }

    public DateTime? PackedAt { get; set; }
    public Guid? PackedByUserId { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    /// <summary>Staging lane the carton waits in before its truck arrives.</summary>
    public string? StagingLocation { get; set; }

    public bool IsSealed { get; set; }
    public string? SealNumber { get; set; }

    public ICollection<PackageContent> Contents { get; set; } = [];
}

/// <summary>What is inside a package, so a short-delivery claim can name a carton.</summary>
public class PackageContent : BaseEntity
{
    public Guid PackageId { get; set; }
    public PackageUnit? Package { get; set; }

    public Guid? OrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public string? SerialNumbers { get; set; }
}

/// <summary>
/// Goods leaving the building: the gate pass, the challan, the transport document.
///
/// Separate from the trip because a dispatch can go by third-party courier with no trip at all,
/// and a trip can carry several dispatches.
/// </summary>
public class Dispatch : BaseEntity
{
    public string DispatchNumber { get; set; } = string.Empty;

    public Guid? WarehouseId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }

    public DateTime DispatchedAt { get; set; }
    public Guid? DispatchedByUserId { get; set; }

    public string? GatePassNumber { get; set; }
    public string? TransportDocumentNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? AirwayBillNumber { get; set; }
    public decimal FreightCost { get; set; }

    public int PackageCount { get; set; }
    public decimal TotalWeightKg { get; set; }
    public decimal TotalValue { get; set; }

    public string? DriverAcknowledgement { get; set; }
    public string? Note { get; set; }

    public ICollection<DispatchLine> Lines { get; set; } = [];
}

/// <summary>One order's presence on a dispatch.</summary>
public class DispatchLine : BaseEntity
{
    public Guid DispatchId { get; set; }
    public Dispatch? Dispatch { get; set; }

    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }

    public int PackageCount { get; set; }
    public decimal Value { get; set; }
    public int StopSequence { get; set; }
}
