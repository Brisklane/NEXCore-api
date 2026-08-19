using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Fleet ────────────────────────────────────────────────────────────────────

public class VehicleDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public VehicleKind Kind { get; set; }
    public VehicleOwnership Ownership { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? ManufactureYear { get; set; }
    public string? FuelType { get; set; }
    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }
    public bool IsRefrigerated { get; set; }
    public decimal? MinSafeCelsius { get; set; }
    public decimal? MaxSafeCelsius { get; set; }
    public Guid? DefaultDriverId { get; set; }
    public string? DefaultDriverName { get; set; }
    public Guid? HomeWarehouseId { get; set; }
    public Guid? TerritoryId { get; set; }
    public decimal CurrentOdometerKm { get; set; }
    public decimal? FuelEfficiencyKmPerUnit { get; set; }
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceReason { get; set; }
    public DateTime? NextServiceDueOn { get; set; }
    public decimal? NextServiceDueAtKm { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    public List<VehicleComplianceDto> ComplianceRecords { get; set; } = [];

    /// <summary>Compliance documents already expired or expiring inside the alert window.</summary>
    public int ExpiringComplianceCount { get; set; }
    public bool HasExpiredCompliance { get; set; }
    public DateTime? EarliestComplianceExpiry { get; set; }
}

public class SaveVehicleDto
{
    public string? Code { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public VehicleKind Kind { get; set; } = VehicleKind.Van;
    public VehicleOwnership Ownership { get; set; } = VehicleOwnership.Owned;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? ManufactureYear { get; set; }
    public string? FuelType { get; set; }
    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }
    public bool IsRefrigerated { get; set; }
    public decimal? MinSafeCelsius { get; set; }
    public decimal? MaxSafeCelsius { get; set; }
    public Guid? DefaultDriverId { get; set; }
    public Guid? HomeWarehouseId { get; set; }
    public Guid? TerritoryId { get; set; }
    public decimal CurrentOdometerKm { get; set; }
    public decimal? FuelEfficiencyKmPerUnit { get; set; }
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceReason { get; set; }
    public DateTime? NextServiceDueOn { get; set; }
    public decimal? NextServiceDueAtKm { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public List<VehicleComplianceDto> ComplianceRecords { get; set; } = [];
}

public class VehicleComplianceDto
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public VehicleComplianceKind Kind { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Issuer { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public decimal Cost { get; set; }
    public string? FileUrl { get; set; }
    public bool IsSuperseded { get; set; }
    public string? Note { get; set; }
    public int DaysToExpiry { get; set; }
}

public class DriverDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? LicenceNumber { get; set; }
    public string? LicenceClass { get; set; }
    public DateTime? LicenceExpiresOn { get; set; }
    public Guid? DefaultVehicleId { get; set; }
    public string? DefaultVehicleRegistration { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }
    public int? DaysToLicenceExpiry { get; set; }
}

public class SaveDriverDto
{
    public string? Code { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? LicenceNumber { get; set; }
    public string? LicenceClass { get; set; }
    public DateTime? LicenceExpiresOn { get; set; }
    public Guid? DefaultVehicleId { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

// ── Trips ────────────────────────────────────────────────────────────────────

public class TripDto
{
    public Guid Id { get; set; }
    public string TripNumber { get; set; } = string.Empty;
    public DateTime TripDate { get; set; }
    public TripStatus Status { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? HelperName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? FieldRepId { get; set; }

    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }

    public decimal OdometerOutKm { get; set; }
    public decimal OdometerInKm { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal PlannedDistanceKm { get; set; }
    public decimal FuelIssued { get; set; }

    public int PlannedStops { get; set; }
    public int CompletedStops { get; set; }
    public int FailedStops { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal DeliveredValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal FillRatePercent { get; set; }
    public decimal OnTimePercent { get; set; }

    /// <summary>Expense plus notional driver time, over the value delivered.</summary>
    public decimal CostPerDrop { get; set; }

    public string? CancelReason { get; set; }
    public string? Note { get; set; }
    public List<TripStopDto> Stops { get; set; } = [];
    public List<TripExpenseDto> Expenses { get; set; } = [];
}

public class TripSummaryDto
{
    public Guid Id { get; set; }
    public string TripNumber { get; set; } = string.Empty;
    public DateTime TripDate { get; set; }
    public TripStatus Status { get; set; }
    public string? VehicleRegistration { get; set; }
    public string? DriverName { get; set; }
    public string? RouteName { get; set; }
    public int PlannedStops { get; set; }
    public int CompletedStops { get; set; }
    public int FailedStops { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal DeliveredValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal FillRatePercent { get; set; }
    public decimal ProgressPercent { get; set; }
}

public class TripStopDto
{
    public Guid Id { get; set; }
    public int StopSequence { get; set; }
    public TripStopStatus Status { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? DestinationName { get; set; }
    public string? AddressLine { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? PlannedArrivalAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public int? ServiceMinutes { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal DeliveredValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }
    public Guid? FailureReasonCodeId { get; set; }
    public string? FailureReasonName { get; set; }
    public string? FailureNote { get; set; }
    public DateTime? RescheduledFor { get; set; }
    public int AttemptNumber { get; set; }
    public Guid? ProofOfDeliveryId { get; set; }
    public string? Note { get; set; }
    public bool IsLate { get; set; }
}

public class CreateTripDto
{
    public DateTime TripDate { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string? HelperName { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? FieldRepId { get; set; }
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public string? Note { get; set; }

    /// <summary>Orders to load onto this trip, in the order given.</summary>
    public List<Guid> OrderIds { get; set; } = [];

    /// <summary>Build stops from the route's beat sequence rather than from the order list.</summary>
    public bool UseRouteSequence { get; set; } = true;
}

public class ResequenceTripDto
{
    public Guid TripId { get; set; }
    public List<Guid> OrderedStopIds { get; set; } = [];
}

public class StartTripDto
{
    public Guid TripId { get; set; }
    public decimal OdometerOutKm { get; set; }
    public decimal FuelIssued { get; set; }
    public DateTime? DepartedAt { get; set; }
}

public class EndTripDto
{
    public Guid TripId { get; set; }
    public decimal OdometerInKm { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? Note { get; set; }
}

public class ArriveAtStopDto
{
    public Guid StopId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime? ArrivedAt { get; set; }
}

public class FailStopDto
{
    public Guid StopId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
    public DateTime? RescheduleFor { get; set; }
}

public class TripExpenseDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string? TripNumber { get; set; }
    public TripExpenseKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime IncurredAt { get; set; }
    public string? Reference { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? Note { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}

public class SaveTripExpenseDto
{
    public Guid TripId { get; set; }
    public TripExpenseKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateTime IncurredAt { get; set; }
    public string? Reference { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? Note { get; set; }
}

// ── Proof of delivery ────────────────────────────────────────────────────────

public class PodDto
{
    public Guid Id { get; set; }
    public string PodNumber { get; set; } = string.Empty;
    public Guid? TripId { get; set; }
    public string? TripNumber { get; set; }
    public Guid? TripStopId { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? VisitId { get; set; }

    public DateTime DeliveredAt { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ReceivedByPhone { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? PhotoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public GeoValidation GeoValidation { get; set; }
    public bool OtpVerified { get; set; }

    public decimal DeliveredValue { get; set; }
    public decimal ShortValue { get; set; }
    public decimal DamagedValue { get; set; }
    public decimal RejectedValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public Guid? CreditNoteId { get; set; }
    public bool IsClean { get; set; }
    public bool IsExceptionResolved { get; set; }
    public string? ExceptionNote { get; set; }
    public string? ReceiptSentTo { get; set; }
    public string? Note { get; set; }

    public List<PodLineDto> Lines { get; set; } = [];
}

public class PodLineDto
{
    public Guid Id { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal DespatchedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal ShortQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public PodLineOutcome Outcome { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CreditValue { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

public class CapturePodDto
{
    public Guid? TripId { get; set; }
    public Guid? TripStopId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? VisitId { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ReceivedByPhone { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? PhotoUrl { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool OtpVerified { get; set; }
    public string? OtpReference { get; set; }
    public string? ReceiptSentTo { get; set; }
    public string? Note { get; set; }

    public List<PodLineDto> Lines { get; set; } = [];

    /// <summary>Money taken at the door in the same breath as the delivery.</summary>
    public RecordCollectionDto? Collection { get; set; }

    /// <summary>Raise the credit note for short and damaged lines immediately.</summary>
    public bool AutoCreditExceptions { get; set; } = true;

    public string? IdempotencyKey { get; set; }
}

// ── Returns ──────────────────────────────────────────────────────────────────

public class ReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public ReturnKind Kind { get; set; }
    public Domain.Enums.ReturnStatus Status { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? OriginalOrderId { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public DateTime RequestedOn { get; set; }
    public DateTime? ValidUntil { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public string? ReasonNote { get; set; }
    public ReturnValuationBasis ValuationBasis { get; set; }
    public decimal ValuationPercent { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ClaimedValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal CreditedValue { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? CollectionTripId { get; set; }
    public Guid? CollectionVanUnitId { get; set; }
    public DateTime? CollectedAt { get; set; }
    public Guid? LinkedClaimId { get; set; }
    public Guid? CreditNoteId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
    public bool IsExpired { get; set; }

    public List<ReturnLineDto> Lines { get; set; } = [];
    public List<ReturnReceiptDto> Receipts { get; set; } = [];
}

public class ReturnSummaryDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public ReturnKind Kind { get; set; }
    public Domain.Enums.ReturnStatus Status { get; set; }
    public DateTime RequestedOn { get; set; }
    public string? OutletName { get; set; }
    public string? PartnerName { get; set; }
    public string? ReasonCodeName { get; set; }
    public decimal ClaimedValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int LineCount { get; set; }
}

public class ReturnLineDto
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
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineValue { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonCodeName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

public class RequestReturnDto
{
    public ReturnKind Kind { get; set; } = ReturnKind.SaleableMarketReturn;
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? OriginalOrderId { get; set; }
    public Guid? OriginalInvoiceId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonNote { get; set; }
    public ReturnValuationBasis ValuationBasis { get; set; } = ReturnValuationBasis.OriginalInvoicePrice;
    public decimal ValuationPercent { get; set; } = 100;
    public DateTime? ValidUntil { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
    public List<ReturnLineDto> Lines { get; set; } = [];

    /// <summary>Van sales: the goods are taken back on the spot, into the returns compartment.</summary>
    public bool CollectOnVanNow { get; set; }
    public Guid? VanUnitId { get; set; }

    public string? IdempotencyKey { get; set; }
}

public class DecideReturnDto
{
    public bool IsApproved { get; set; }
    public List<ReturnLineDecisionDto> Lines { get; set; } = [];
    public string? RejectionReason { get; set; }
}

public class ReturnLineDecisionDto
{
    public Guid LineId { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public string? Note { get; set; }
}

public class ReturnReceiptDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid? ReturnId { get; set; }
    public string? ReturnNumber { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? InspectedAt { get; set; }
    public decimal TotalValue { get; set; }
    public decimal RestockedValue { get; set; }
    public decimal ScrappedValue { get; set; }
    public string? DestructionCertificateNumber { get; set; }
    public string? DestructionCertificateUrl { get; set; }
    public DateTime? DestroyedOn { get; set; }
    public string? Note { get; set; }
    public List<ReturnReceiptLineDto> Lines { get; set; } = [];
}

public class ReturnReceiptLineDto
{
    public Guid Id { get; set; }
    public Guid? ReturnLineId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineValue { get; set; }
    public ReturnDispositionKind Disposition { get; set; }
    public Guid? PutawayBinId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

public class ReceiveReturnDto
{
    public Guid? ReturnId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? TripId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Note { get; set; }
    public List<ReturnReceiptLineDto> Lines { get; set; } = [];
}

public class DispositionReturnDto
{
    public Guid ReceiptLineId { get; set; }
    public ReturnDispositionKind Kind { get; set; }
    public decimal Quantity { get; set; }
    public Guid? TargetBinId { get; set; }
    public Guid? TargetSupplierId { get; set; }
    public Guid? LiquidationSchemeId { get; set; }
    public string? Note { get; set; }
}
