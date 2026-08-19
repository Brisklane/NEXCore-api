using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A temperature-controlled point: a cold room, a reefer, an outlet's cooler.
///
/// The safe range lives on the checkpoint rather than in a policy document, because the whole
/// value of the record is that a reading can be judged against it automatically at the moment it
/// is taken.
/// </summary>
public class ColdChainCheckpoint : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ColdChainPointKind Kind { get; set; } = ColdChainPointKind.ColdRoom;

    public Guid? WarehouseId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? OutletAssetId { get; set; }

    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }

    /// <summary>How often a reading is expected. An overdue checkpoint is itself a finding.</summary>
    public int CheckIntervalHours { get; set; } = 8;

    public string? Location { get; set; }
    public string? SensorIdentifier { get; set; }

    public DateTime? LastReadingAt { get; set; }
    public decimal? LastReadingCelsius { get; set; }
    public bool IsInBreach { get; set; }

    public string? Note { get; set; }

    public ICollection<ColdChainLog> Logs { get; set; } = [];
}

/// <summary>
/// One temperature reading.
///
/// An out-of-range reading cannot be closed without a corrective action. A log full of breaches
/// and no actions is precisely what an inspector looks for, and precisely what a paper clipboard
/// produces.
/// </summary>
public class ColdChainLog : BaseEntity
{
    public Guid CheckpointId { get; set; }
    public ColdChainCheckpoint? Checkpoint { get; set; }

    public DateTime RecordedAt { get; set; }
    public decimal ReadingCelsius { get; set; }

    public bool IsOutOfRange { get; set; }

    /// <summary>How long the excursion lasted, once it is closed.</summary>
    public int? ExcursionMinutes { get; set; }

    public Guid? RecordedByUserId { get; set; }
    public string? RecordedByName { get; set; }

    /// <summary>True when a sensor supplied it rather than a person. Phase 2 populates this.</summary>
    public bool IsAutomatic { get; set; }

    /// <summary>Mandatory before an out-of-range reading can be marked resolved.</summary>
    public string? CorrectiveAction { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    /// <summary>Stock judged unsafe and written off because of this excursion.</summary>
    public decimal AffectedStockValue { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// One hop of a batch's journey: received here, shipped there, sold to that outlet.
///
/// Traceability is only useful if it answers both directions in one query — forward from a batch
/// to every outlet that got it, and backward from a complaint to the supplier receipt. Storing the
/// links as their own rows is what makes both a single indexed scan during a recall, when the
/// question is urgent and nobody has an hour.
/// </summary>
public class BatchTraceLink : BaseEntity
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;

    public Guid? BatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }

    /// <summary>The supplier's own lot reference, which is what a recall notice will quote.</summary>
    public string? SupplierLotReference { get; set; }
    public Guid? SupplierId { get; set; }

    /// <summary>Set when the goods were made in-house rather than bought.</summary>
    public Guid? ProductionOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }

    // ── Where it went ────────────────────────────────────────────────────────
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? DocumentNumber { get; set; }

    public DateTime MovedAt { get; set; }

    /// <summary>"Receipt", "Transfer", "Sale", "Return" — the shape of this hop.</summary>
    public string MovementType { get; set; } = string.Empty;

    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

/// <summary>
/// A recall or withdrawal on a batch.
///
/// The record's job is to turn "which shops have it" into a list, and then to track how much of
/// what went out has actually come back — because the recovery percentage is the number a
/// regulator asks for and the one nobody can produce from invoices alone.
/// </summary>
public class ProductRecall : BaseEntity
{
    public string RecallNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public RecallStatus Status { get; set; } = RecallStatus.Draft;
    public RecallSeverity Severity { get; set; } = RecallSeverity.ClassII;

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? SupplierId { get; set; }

    public DateTime InitiatedOn { get; set; }
    public DateTime? AnnouncedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? TargetCompletionOn { get; set; }

    public string? Reason { get; set; }
    public string? RegulatoryReference { get; set; }
    public string? PublicNotice { get; set; }

    // ── Recovery ─────────────────────────────────────────────────────────────
    public decimal DespatchedQuantity { get; set; }
    public decimal RecoveredQuantity { get; set; }
    public decimal DestroyedQuantity { get; set; }

    /// <summary>Recovered ÷ despatched. What the regulator asks for.</summary>
    public decimal RecoveryPercent { get; set; }

    public decimal EstimatedValue { get; set; }
    public decimal RecoveredValue { get; set; }

    public int AffectedOutletCount { get; set; }
    public int NotifiedOutletCount { get; set; }
    public int RespondedOutletCount { get; set; }

    public Guid? InitiatedByUserId { get; set; }
    public string? ClosureReport { get; set; }
    public string? Note { get; set; }

    public ICollection<RecallOutletNotice> Notices { get; set; } = [];
}

/// <summary>One outlet's part in a recall: told, responded, returned this much.</summary>
public class RecallOutletNotice : BaseEntity
{
    public Guid RecallId { get; set; }
    public ProductRecall? Recall { get; set; }

    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }
    public Guid? PartnerId { get; set; }
    public string? DestinationName { get; set; }

    /// <summary>What this outlet received of the recalled batch, from the trace links.</summary>
    public decimal SuppliedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal Value { get; set; }

    public DateTime? NotifiedAt { get; set; }
    public string? NotificationChannel { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? CollectedAt { get; set; }

    public Guid? ReturnId { get; set; }
    public Guid? VisitId { get; set; }

    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A configurable reason offered wherever the system demands an explanation.
///
/// One registry rather than fifteen hard-coded dropdowns, keyed by the surface it appears on, so
/// a warehouse can add "pallet damaged in transit" to short-pick reasons without a deploy — and
/// so a damage reason never turns up in a no-order list.
/// </summary>
public class ReasonCode : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ReasonSurface Surface { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Demands free text alongside the code — for the "Other" that always gets added.</summary>
    public bool RequiresNote { get; set; }

    /// <summary>Selecting this reason escalates the transaction for approval.</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>Marks the loss as recoverable from the person responsible rather than written off.</summary>
    public bool IsRecoverable { get; set; }

    /// <summary>Counts against the outlet or rep in the exception reports.</summary>
    public bool IsNegative { get; set; }

    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public bool IsSystem { get; set; }
}

/// <summary>
/// Every threshold, tolerance and enforcement mode in one place, one row per tenant.
///
/// A distribution business is a set of tolerances — how far outside a geofence is acceptable, how
/// short a van can be before someone signs, how many days of expiry left is "near". Scattering
/// those across code is how two branches end up running different rules without anyone deciding to.
/// </summary>
public class DistributionSettings : BaseEntity
{
    // ── Field discipline ─────────────────────────────────────────────────────
    public int DefaultGeofenceRadiusMetres { get; set; } = 150;
    public bool RequireGeoOnCheckIn { get; set; } = true;
    public bool AllowOutOfFenceCheckIn { get; set; } = true;
    public bool RequireReasonOnNoOrder { get; set; } = true;
    public bool RequireStartSelfie { get; set; }
    public int MaxUnplannedVisitsPerDay { get; set; } = 5;
    public bool BlockDayCloseWithUnsynced { get; set; } = true;

    /// <summary>Rep is flagged if the day has not started by this time.</summary>
    public TimeSpan DayStartDeadline { get; set; } = new(11, 0, 0);

    // ── Orders ───────────────────────────────────────────────────────────────
    public decimal MinimumOrderValue { get; set; }
    public decimal DiscountApprovalThreshold { get; set; }
    public decimal MarginFloorPercent { get; set; }
    public bool AllowBackorders { get; set; } = true;
    public int OrderEditWindowMinutes { get; set; } = 30;
    public AllocationStrategy DefaultAllocationStrategy { get; set; } = AllocationStrategy.Fefo;
    public int SoftAllocationHoldHours { get; set; } = 24;

    // ── Credit ───────────────────────────────────────────────────────────────
    public CreditEnforcement CreditEnforcementAtOrder { get; set; } = CreditEnforcement.Warn;
    public CreditEnforcement CreditEnforcementAtDispatch { get; set; } = CreditEnforcement.Block;
    public CreditEnforcement CreditEnforcementAtVanSale { get; set; } = CreditEnforcement.Block;
    public int AgeingBucket1Days { get; set; } = 30;
    public int AgeingBucket2Days { get; set; } = 60;
    public int AgeingBucket3Days { get; set; } = 90;
    public bool AutoBlockOnBouncedCheque { get; set; } = true;
    public decimal ChequeBounceCharge { get; set; }

    // ── Stock & expiry ───────────────────────────────────────────────────────
    public bool EnforceFefo { get; set; } = true;
    public bool AllowFefoOverride { get; set; } = true;
    public int NearExpiryWarningDays { get; set; } = 90;
    public int NearExpiryCriticalDays { get; set; } = 30;

    /// <summary>Refuse to despatch stock with less than this percentage of shelf life remaining.</summary>
    public decimal MinimumShelfLifePercentOnDespatch { get; set; } = 50;

    public bool AutoQuarantineExpired { get; set; } = true;

    // ── Settlement tolerances ────────────────────────────────────────────────
    public decimal CashVarianceTolerance { get; set; }
    public decimal StockVarianceTolerancePercent { get; set; } = 1;
    public bool BlockSettlementOnUnexplainedVariance { get; set; } = true;
    public decimal VarianceApprovalThreshold { get; set; }

    /// <summary>Route is flagged unsettled after this hour.</summary>
    public TimeSpan SettlementCutOff { get; set; } = new(20, 0, 0);

    // ── Schemes ──────────────────────────────────────────────────────────────
    public bool AutoApplySchemes { get; set; } = true;
    public bool ShowNextSlabPrompt { get; set; } = true;
    public bool StopSchemeOnBudgetExhausted { get; set; } = true;

    // ── Claims ───────────────────────────────────────────────────────────────
    public int ClaimSubmissionWindowDays { get; set; } = 45;
    public int ClaimSettlementSlaDays { get; set; } = 15;
    public bool AutoGenerateDeferredSchemeClaims { get; set; } = true;

    // ── Secondary sales ──────────────────────────────────────────────────────
    public int SecondaryUploadDueDayOfMonth { get; set; } = 5;
    public decimal MinimumMappingAccuracyPercent { get; set; } = 90;
    public decimal ReconciliationTolerancePercent { get; set; } = 2;

    // ── Documents & currency ─────────────────────────────────────────────────
    public string BaseCurrencyCode { get; set; } = "USD";
    public bool PrintThermalInvoices { get; set; } = true;
    public string? InvoiceFooter { get; set; }
    public bool SendDigitalReceipts { get; set; } = true;
}

/// <summary>
/// An alert raised for a person.
///
/// Subscriptions and quiet hours are on the record because a system that pages everyone about
/// everything gets muted within a week, and after that the critical ones do not arrive either.
/// </summary>
public class DistributionNotification : BaseEntity
{
    public DistributionAlertKind Kind { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }

    public Guid? TargetUserId { get; set; }
    public Guid? TargetFieldRepId { get; set; }
    public Guid? TargetPartnerId { get; set; }

    /// <summary>What it is about — an order, a claim, a route, a batch.</summary>
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ActionRoute { get; set; }

    public DateTime RaisedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? ActionedAt { get; set; }

    /// <summary>Comma-separated channels this went out on: InApp, Email, Sms, Push, Webhook.</summary>
    public string? DeliveredChannels { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryError { get; set; }
}
