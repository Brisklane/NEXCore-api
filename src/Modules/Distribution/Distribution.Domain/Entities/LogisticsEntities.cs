using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>A vehicle in the fleet, owned, leased or contracted.</summary>
public class Vehicle : BaseEntity
{
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

    /// <summary>Off the road — service, accident, impound. Trips cannot be planned on it.</summary>
    public bool IsOutOfService { get; set; }
    public string? OutOfServiceReason { get; set; }

    public DateTime? NextServiceDueOn { get; set; }
    public decimal? NextServiceDueAtKm { get; set; }

    public string? Note { get; set; }

    public ICollection<VehicleCompliance> ComplianceRecords { get; set; } = [];
}

/// <summary>
/// A statutory document a vehicle must carry, with its expiry.
///
/// Stored one row per document type rather than as six date columns, because a fleet manager
/// needs "everything expiring in the next thirty days" across types, and that is a query, not
/// six comparisons.
/// </summary>
public class VehicleCompliance : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public VehicleComplianceKind Kind { get; set; }
    public string? DocumentNumber { get; set; }
    public string? Issuer { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime ExpiresOn { get; set; }
    public decimal Cost { get; set; }
    public string? FileUrl { get; set; }

    /// <summary>Superseded by a renewal; kept for the audit trail.</summary>
    public bool IsSuperseded { get; set; }
    public string? Note { get; set; }
}

/// <summary>A driver, with the licence detail that makes them legal to be behind the wheel.</summary>
public class Driver : BaseEntity
{
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

    public string? Note { get; set; }
}

/// <summary>
/// A delivery run: one vehicle, one driver, one ordered list of stops, one day.
///
/// The trip is where cost to serve becomes measurable. Fuel, tolls, labour and distance are
/// captured against it, so the question "does visiting this outlet weekly actually pay?" has an
/// answer instead of an opinion.
/// </summary>
public class DeliveryTrip : BaseEntity
{
    public string TripNumber { get; set; } = string.Empty;
    public DateTime TripDate { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Planned;

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }

    public string? HelperName { get; set; }
    public Guid? RouteId { get; set; }
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

    // ── Outcome ──────────────────────────────────────────────────────────────
    public int PlannedStops { get; set; }
    public int CompletedStops { get; set; }
    public int FailedStops { get; set; }

    public decimal PlannedValue { get; set; }
    public decimal DeliveredValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal TotalExpense { get; set; }

    /// <summary>Delivered value ÷ planned value. The trip's fill rate.</summary>
    public decimal FillRatePercent { get; set; }

    /// <summary>Stops reached inside their promised window, as a percentage.</summary>
    public decimal OnTimePercent { get; set; }

    public string? CancelReason { get; set; }
    public string? Note { get; set; }

    public ICollection<TripStop> Stops { get; set; } = [];
    public ICollection<TripExpense> Expenses { get; set; } = [];
}

/// <summary>One stop on a trip, with the timestamps that make on-time measurable.</summary>
public class TripStop : BaseEntity
{
    public Guid TripId { get; set; }
    public DeliveryTrip? Trip { get; set; }

    public int StopSequence { get; set; }
    public TripStopStatus Status { get; set; } = TripStopStatus.Pending;

    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OrderId { get; set; }

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

    /// <summary>Mandatory on anything other than a clean delivery.</summary>
    public Guid? FailureReasonCodeId { get; set; }
    public string? FailureNote { get; set; }
    public DateTime? RescheduledFor { get; set; }
    public int AttemptNumber { get; set; } = 1;

    public Guid? ProofOfDeliveryId { get; set; }
    public string? Note { get; set; }
}

/// <summary>A cost incurred on a trip. Cost to serve is the sum of these plus the driver's time.</summary>
public class TripExpense : BaseEntity
{
    public Guid TripId { get; set; }
    public DeliveryTrip? Trip { get; set; }

    public TripExpenseKind Kind { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime IncurredAt { get; set; }

    public string? Reference { get; set; }
    public string? ReceiptUrl { get; set; }
    public string? Note { get; set; }

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// The record of what was actually handed over at the door.
///
/// Signature, photo, geo and time are the four things that end a delivery dispute. Line-level
/// acceptance is the fifth and the one most systems skip, which is why they generate credit
/// notes a week later from a phone call instead of at the door from a record.
/// </summary>
public class ProofOfDelivery : BaseEntity
{
    public string PodNumber { get; set; } = string.Empty;

    public Guid? TripId { get; set; }
    public Guid? TripStopId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? VisitId { get; set; }

    public DateTime DeliveredAt { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ReceivedByPhone { get; set; }
    public string? SignatureImageUrl { get; set; }
    public string? PhotoUrl { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public GeoValidation GeoValidation { get; set; } = GeoValidation.NoFix;

    /// <summary>One-time code the recipient reads out, where the channel demands it.</summary>
    public bool OtpVerified { get; set; }
    public string? OtpReference { get; set; }

    public decimal DeliveredValue { get; set; }
    public decimal ShortValue { get; set; }
    public decimal DamagedValue { get; set; }
    public decimal RejectedValue { get; set; }
    public decimal CollectedAmount { get; set; }

    /// <summary>Set when a short or damaged line produced a credit note there and then.</summary>
    public Guid? CreditNoteId { get; set; }

    public bool IsClean { get; set; } = true;
    public bool IsExceptionResolved { get; set; }
    public string? ExceptionNote { get; set; }

    /// <summary>Digital receipt destination, when one was sent.</summary>
    public string? ReceiptSentTo { get; set; }

    public string? Note { get; set; }

    public ICollection<PodLine> Lines { get; set; } = [];
}

/// <summary>
/// Line-level acceptance at the door.
///
/// "Delivered" as a single flag is the root of most distribution disputes. Recording that four
/// of five cases were accepted and one was dented, at the moment it happened, is what makes the
/// credit note uncontroversial.
/// </summary>
public class PodLine : BaseEntity
{
    public Guid PodId { get; set; }
    public ProofOfDelivery? Pod { get; set; }

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

    public PodLineOutcome Outcome { get; set; } = PodLineOutcome.Accepted;
    public decimal UnitPrice { get; set; }
    public decimal CreditValue { get; set; }

    public Guid? ReasonCodeId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}
