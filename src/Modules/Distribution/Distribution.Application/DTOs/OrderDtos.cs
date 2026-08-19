using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Orders ───────────────────────────────────────────────────────────────────

public class DistributionOrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderSource Source { get; set; }
    public DistributionOrderKind Kind { get; set; }
    public DistributionOrderStatus Status { get; set; }

    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public string? OutletCode { get; set; }
    public OutletChannel? OutletChannel { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? TerritoryId { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? LinkedPurchaseOrderId { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? SalesInvoiceId { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeDiscountAmount { get; set; }
    public decimal FreeGoodsValue { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginPercent { get; set; }

    public bool RequiresApproval { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? HoldReason { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancelNote { get; set; }
    public bool HasCreditOverride { get; set; }
    public bool IsBackorderParent { get; set; }
    public Guid? BackorderOfOrderId { get; set; }

    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal FillRatePercent { get; set; }
    public string? ExternalReference { get; set; }
    public string? Note { get; set; }

    public List<DistributionOrderLineDto> Lines { get; set; } = [];
    public List<OrderStatusEventDto> StatusEvents { get; set; } = [];
    public List<OrderApprovalDto> Approvals { get; set; } = [];
    public List<SchemeApplicationDto> AppliedSchemes { get; set; } = [];

    /// <summary>What the customer could still earn by buying a little more.</summary>
    public List<NextSlabHintDto> NextSlabHints { get; set; } = [];
}

public class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DistributionOrderStatus Status { get; set; }
    public OrderSource Source { get; set; }
    public DistributionOrderKind Kind { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? FieldRepName { get; set; }
    public string? RouteName { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int LineCount { get; set; }
    public decimal FillRatePercent { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsOverdueForDelivery { get; set; }
}

public class DistributionOrderLineDto
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal AllocatedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal DispatchedQuantity { get; set; }
    public decimal DeliveredQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal BackorderedQuantity { get; set; }

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

    public bool IsFreeGoods { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public PriceScope? PriceScope { get; set; }
    public bool IsPriceOverridden { get; set; }
    public string? PriceOverrideReason { get; set; }
    public bool IsShortPicked { get; set; }
    public string? Note { get; set; }

    /// <summary>Free stock at the servicing location, so the counter knows before promising.</summary>
    public decimal AvailableQuantity { get; set; }
}

public class SaveOrderLineDto
{
    public Guid? Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid? BatchId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }

    /// <summary>Null lets the pricing engine resolve it; a value is an override needing authority.</summary>
    public decimal? UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? PriceOverrideReason { get; set; }
    public string? Note { get; set; }
}

public class CreateOrderDto
{
    public OrderSource Source { get; set; } = OrderSource.FieldTerminal;
    public DistributionOrderKind Kind { get; set; } = DistributionOrderKind.Standard;
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }

    public Guid? VisitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }

    public DateTime? OrderDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal FreightAmount { get; set; }
    public string? ExternalReference { get; set; }
    public string? Note { get; set; }

    public List<SaveOrderLineDto> Lines { get; set; } = [];

    /// <summary>Submit straight away rather than leaving a draft.</summary>
    public bool SubmitImmediately { get; set; } = true;

    /// <summary>A van sale: invoice and deplete van stock in the same call.</summary>
    public bool IsVanSale { get; set; }

    /// <summary>Collected at the door as part of the same transaction.</summary>
    public RecordCollectionDto? Collection { get; set; }

    public string? IdempotencyKey { get; set; }
}

public class UpdateOrderDto
{
    public DateTime? RequestedDeliveryDate { get; set; }
    public DateTime? PromisedDeliveryDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public decimal? FreightAmount { get; set; }
    public string? Note { get; set; }
    public List<SaveOrderLineDto>? Lines { get; set; }
}

public class OrderDecisionDto
{
    public Guid OrderId { get; set; }
    public bool IsApproved { get; set; }
    public string? Comment { get; set; }
    public Guid? ReasonCodeId { get; set; }
}

public class CancelOrderDto
{
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

public class OrderStatusEventDto
{
    public DistributionOrderStatus FromStatus { get; set; }
    public DistributionOrderStatus ToStatus { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ActorName { get; set; }
    public string? Note { get; set; }
}

public class OrderApprovalDto
{
    public Guid Id { get; set; }
    public int StepNumber { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string? TriggerReason { get; set; }
    public string? ApproverName { get; set; }
    public bool? IsApproved { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? Comment { get; set; }
    public bool IsEscalated { get; set; }
}

/// <summary>
/// A priced basket returned before anything is saved.
///
/// The field terminal calls this on every quantity change so the retailer sees price, scheme,
/// free goods and credit position live at the counter. Nothing is persisted.
/// </summary>
public class OrderQuoteDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeDiscountAmount { get; set; }
    public decimal FreeGoodsValue { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginPercent { get; set; }

    public List<DistributionOrderLineDto> Lines { get; set; } = [];
    public List<SchemeApplicationDto> AppliedSchemes { get; set; } = [];
    public List<NextSlabHintDto> NextSlabHints { get; set; } = [];

    public CreditCheckResultDto Credit { get; set; } = new();

    /// <summary>Rules the basket breaks: below minimum order value, unauthorised SKU, no stock.</summary>
    public List<string> Warnings { get; set; } = [];
    public List<string> Blockers { get; set; } = [];
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
}

public class QuoteOrderDto
{
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? FieldRepId { get; set; }
    public DateTime? OrderDate { get; set; }
    public List<SaveOrderLineDto> Lines { get; set; } = [];
}

/// <summary>
/// "Two more cases unlocks the next slab" — the highest-return sentence in trade selling, and
/// the reason the quote endpoint exists at all.
/// </summary>
public class NextSlabHintDto
{
    public Guid SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal CurrentQuantity { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal ShortfallQuantity { get; set; }
    public decimal CurrentBenefit { get; set; }
    public decimal NextBenefit { get; set; }
    public decimal ExtraBenefit { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>The catalogue as the field terminal renders it, priced for this outlet.</summary>
public class CatalogueItemDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public List<CatalogueUomDto> Uoms { get; set; } = [];
    public decimal Mrp { get; set; }
    public decimal TaxPercent { get; set; }

    public decimal AvailableQuantity { get; set; }
    public bool IsAuthorised { get; set; } = true;
    public string? UnauthorisedReason { get; set; }

    /// <summary>Schemes live on this SKU right now, summarised for the shelf-talker line.</summary>
    public List<string> ActiveSchemes { get; set; } = [];

    /// <summary>What this outlet bought last time, so a reorder is one tap.</summary>
    public decimal LastOrderedQuantity { get; set; }
    public DateTime? LastOrderedAt { get; set; }

    /// <summary>Computed from the outlet's own offtake and observed shelf stock.</summary>
    public decimal SuggestedQuantity { get; set; }

    public bool IsFocusItem { get; set; }
    public bool IsNeverBought { get; set; }
    public DateTime? NearestExpiryDate { get; set; }
}

public class CatalogueUomDto
{
    public string Uom { get; set; } = "PCS";
    public decimal Factor { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public bool IsDefault { get; set; }
}

// ── Allocation, picking, packing, dispatch ───────────────────────────────────

public class StockAllocationDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public AllocationStrategy Strategy { get; set; }
    public bool IsHardAllocation { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsFefoOverridden { get; set; }
}

public class AllocateOrderDto
{
    public Guid OrderId { get; set; }
    public AllocationStrategy? Strategy { get; set; }
    public Guid? WarehouseId { get; set; }
    public bool HardAllocate { get; set; }
}

public class PickWaveDto
{
    public Guid Id { get; set; }
    public string WaveNumber { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public PickStrategy Strategy { get; set; }
    public DateTime ReleasedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
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
    public decimal ProgressPercent { get; set; }
    public List<PickTaskDto> Tasks { get; set; } = [];
}

public class CreatePickWaveDto
{
    public Guid? WarehouseId { get; set; }
    public PickStrategy Strategy { get; set; } = PickStrategy.Wave;
    public List<Guid> OrderIds { get; set; } = [];
    public Guid? RouteId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public DateTime? CarrierCutOffAt { get; set; }
    public int Priority { get; set; }
    public string? Note { get; set; }

    /// <summary>Zone names for zone picking; each becomes its own task.</summary>
    public List<string> Zones { get; set; } = [];
}

public class PickTaskDto
{
    public Guid Id { get; set; }
    public Guid WaveId { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public PickTaskStatus Status { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToName { get; set; }
    public string? ZoneName { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LineCount { get; set; }
    public int ShortLineCount { get; set; }
    public List<PickTaskLineDto> Lines { get; set; } = [];
}

public class PickTaskLineDto
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BinId { get; set; }
    public string? BinCode { get; set; }
    public Guid? NominatedBatchId { get; set; }
    public string? NominatedBatchNumber { get; set; }
    public Guid? PickedBatchId { get; set; }
    public string? PickedBatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal RequiredQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public int PickSequence { get; set; }
    public bool IsShort { get; set; }
    public bool IsFefoOverridden { get; set; }
    public string? OverrideReason { get; set; }
    public DateTime? PickedAt { get; set; }
}

public class ConfirmPickDto
{
    public Guid TaskLineId { get; set; }
    public decimal PickedQuantity { get; set; }
    public Guid? PickedBatchId { get; set; }

    /// <summary>Required when the picker took a lot other than the one FEFO nominated.</summary>
    public string? OverrideReason { get; set; }

    /// <summary>Required on a short pick.</summary>
    public Guid? ShortReasonCodeId { get; set; }
}

public class PackageDto
{
    public Guid Id { get; set; }
    public string LicencePlate { get; set; } = string.Empty;
    public PackageKind Kind { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? TripId { get; set; }
    public Guid? DispatchId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? StagingLocation { get; set; }
    public bool IsSealed { get; set; }
    public string? SealNumber { get; set; }
    public List<PackageContentDto> Contents { get; set; } = [];
}

public class PackageContentDto
{
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

public class CreatePackageDto
{
    public PackageKind Kind { get; set; } = PackageKind.Carton;
    public Guid? OrderId { get; set; }
    public Guid? WaveId { get; set; }
    public decimal WeightKg { get; set; }
    public decimal LengthCm { get; set; }
    public decimal WidthCm { get; set; }
    public decimal HeightCm { get; set; }
    public string? StagingLocation { get; set; }
    public string? SealNumber { get; set; }
    public List<PackageContentDto> Contents { get; set; } = [];
}

public class DispatchDto
{
    public Guid Id { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? TripId { get; set; }
    public string? TripNumber { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public DateTime DispatchedAt { get; set; }
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
    public List<DispatchLineDto> Lines { get; set; } = [];
}

public class DispatchLineDto
{
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public int PackageCount { get; set; }
    public decimal Value { get; set; }
    public int StopSequence { get; set; }
}

public class CreateDispatchDto
{
    public Guid? WarehouseId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
    public List<Guid> PackageIds { get; set; } = [];
    public string? GatePassNumber { get; set; }
    public string? TransportDocumentNumber { get; set; }
    public string? CarrierName { get; set; }
    public string? AirwayBillNumber { get; set; }
    public decimal FreightCost { get; set; }
    public string? Note { get; set; }
}

/// <summary>The dispatch desk's live board: what is waiting, picking, staged and gone.</summary>
public class DispatchBoardDto
{
    public Guid? WarehouseId { get; set; }
    public DateTime AsOf { get; set; }

    public int PendingAllocation { get; set; }
    public int AwaitingPick { get; set; }
    public int Picking { get; set; }
    public int Packed { get; set; }
    public int Staged { get; set; }
    public int Dispatched { get; set; }

    public decimal PendingValue { get; set; }
    public decimal DispatchedValue { get; set; }

    public List<PickWaveDto> ActiveWaves { get; set; } = [];
    public List<OrderSummaryDto> UrgentOrders { get; set; } = [];
    public List<TripSummaryDto> TodayTrips { get; set; } = [];

    /// <summary>Orders whose promised date has passed without dispatch.</summary>
    public List<OrderSummaryDto> LateOrders { get; set; } = [];
}

// ── Van sales ────────────────────────────────────────────────────────────────

public class VanUnitDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? HomeWarehouseId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }
    public bool IsRefrigerated { get; set; }
    public decimal? MinSafeCelsius { get; set; }
    public decimal? MaxSafeCelsius { get; set; }
    public bool IsMultiDay { get; set; }
    public DateTime? LastLoadedAt { get; set; }
    public DateTime? LastSettledAt { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    public decimal StockValue { get; set; }
    public int StockLineCount { get; set; }
    public decimal UtilisationPercent { get; set; }
}

public class SaveVanUnitDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? HomeWarehouseId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }
    public bool IsRefrigerated { get; set; }
    public decimal? MinSafeCelsius { get; set; }
    public decimal? MaxSafeCelsius { get; set; }
    public bool IsMultiDay { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public class VanLoadSheetDto
{
    public Guid Id { get; set; }
    public string LoadNumber { get; set; } = string.Empty;
    public Guid VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public DateTime LoadDate { get; set; }
    public VanLoadStatus Status { get; set; }
    public bool IsSuggested { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LoadedAt { get; set; }
    public string? RejectionReason { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal TotalWeightKg { get; set; }
    public int VarianceLineCount { get; set; }
    public string? Note { get; set; }
    public List<VanLoadLineDto> Lines { get; set; } = [];
}

public class VanLoadLineDto
{
    public Guid Id { get; set; }
    public int DisplayOrder { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal SuggestedQuantity { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal LoadedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineValue { get; set; }
    public VanCompartment Compartment { get; set; }
    public string? VarianceReason { get; set; }

    /// <summary>Free stock at the source warehouse, so a load cannot be built on air.</summary>
    public decimal AvailableAtSource { get; set; }
}

public class CreateVanLoadDto
{
    public Guid VanUnitId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public DateTime LoadDate { get; set; }
    public string? Note { get; set; }
    public List<VanLoadLineDto> Lines { get; set; } = [];

    /// <summary>Ask the engine to propose the lines from beat history, open orders and par levels.</summary>
    public bool AutoSuggest { get; set; }
}

public class ConfirmVanLoadDto
{
    public Guid LoadSheetId { get; set; }

    /// <summary>Actual loaded quantity per line. Any mismatch with picked needs a reason.</summary>
    public List<VanLoadConfirmLineDto> Lines { get; set; } = [];
    public string? Note { get; set; }
}

public class VanLoadConfirmLineDto
{
    public Guid LineId { get; set; }
    public decimal LoadedQuantity { get; set; }
    public string? VarianceReason { get; set; }
}

public class VanStockBalanceDto
{
    public Guid Id { get; set; }
    public Guid VanUnitId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public VanCompartment Compartment { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal StockValue { get; set; }
    public DateTime? LastMovementAt { get; set; }

    /// <summary>Negative once past expiry; drives the near-expiry banner on the van screen.</summary>
    public int? DaysToExpiry { get; set; }
}

public class VanStockSummaryDto
{
    public Guid VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
    public decimal SellableValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal DamagedValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public decimal TotalValue { get; set; }
    public int LineCount { get; set; }
    public int NearExpiryLineCount { get; set; }
    public List<VanStockBalanceDto> Balances { get; set; } = [];
}

public class VanStockMovementDto
{
    public Guid Id { get; set; }
    public VanMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public VanCompartment Compartment { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal BalanceAfter { get; set; }
    public decimal Value { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Reason { get; set; }
}

public class VanTransferDto
{
    public Guid FromVanUnitId { get; set; }
    public Guid? ToVanUnitId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public List<VanTransferLineDto> Lines { get; set; } = [];
    public string? Reason { get; set; }
}

public class VanTransferLineDto
{
    public Guid ItemId { get; set; }
    public Guid? BatchId { get; set; }
    public VanCompartment Compartment { get; set; } = VanCompartment.Sellable;
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
}

public class VanCycleCountDto
{
    public Guid Id { get; set; }
    public string CountNumber { get; set; } = string.Empty;
    public Guid VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public DateTime CountedAt { get; set; }
    public bool IsFullCount { get; set; }
    public bool IsBlind { get; set; }
    public int LineCount { get; set; }
    public int VarianceLineCount { get; set; }
    public decimal VarianceValue { get; set; }
    public bool IsApproved { get; set; }
    public string? Note { get; set; }
    public List<VanCycleCountLineDto> Lines { get; set; } = [];
}

public class VanCycleCountLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public VanCompartment Compartment { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal ExpectedQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal VarianceValue { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public string? ReasonNote { get; set; }
    public bool IsAdjusted { get; set; }
}

public class StartVanCountDto
{
    public Guid VanUnitId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }
    public bool IsFullCount { get; set; }
    public bool IsBlind { get; set; } = true;

    /// <summary>Empty means every SKU currently on the van.</summary>
    public List<Guid> ItemIds { get; set; } = [];
}

public class SubmitVanCountDto
{
    public Guid CountId { get; set; }
    public List<SubmitVanCountLineDto> Lines { get; set; } = [];
    public string? Note { get; set; }
}

public class SubmitVanCountLineDto
{
    public Guid LineId { get; set; }
    public decimal CountedQuantity { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonNote { get; set; }
}
