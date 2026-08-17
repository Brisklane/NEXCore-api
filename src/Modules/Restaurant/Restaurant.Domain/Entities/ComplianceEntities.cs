using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// Something whose temperature has to be logged: a chiller, a freezer, a hot-hold cabinet, or a
/// probe point like "cooked chicken core".
///
/// A restaurant is a regulated food business, and in most markets these records are a legal
/// obligation with an inspector attached. Keeping them here — next to the system that already
/// knows the menu, the staff and the service times — is the difference between a compliance
/// record and a clipboard nobody fills in.
/// </summary>
public class TemperatureCheckpoint : BaseEntity
{
    public Guid OutletId { get; set; }

    public string Name { get; set; } = string.Empty;
    public TemperatureCheckpointKind Kind { get; set; } = TemperatureCheckpointKind.Refrigerator;

    /// <summary>Safe range, in the outlet's unit. A reading outside it demands a corrective action.</summary>
    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }

    /// <summary>How often it must be logged, in hours. 0 means on-demand only.</summary>
    public int CheckIntervalHours { get; set; } = 12;

    public int DisplayOrder { get; set; }
    public string? Location { get; set; }
    public Guid? StationId { get; set; }

    public ICollection<TemperatureLog> Logs { get; set; } = [];
}

/// <summary>One reading. Out-of-range readings cannot be closed without a corrective action.</summary>
public class TemperatureLog : BaseEntity
{
    public Guid CheckpointId { get; set; }
    public TemperatureCheckpoint? Checkpoint { get; set; }

    public Guid OutletId { get; set; }

    public decimal ReadingCelsius { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    /// <summary>Computed on save against the checkpoint's range, so reports never re-derive it.</summary>
    public bool IsOutOfRange { get; set; }

    /// <summary>
    /// What was done about it. An out-of-range reading with no action recorded is precisely what
    /// an inspection looks for, so the API refuses to accept one.
    /// </summary>
    public string? CorrectiveAction { get; set; }

    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByStaffId { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// A recurring set of checks — opening, closing, cleaning, allergen changeover, equipment.
/// Scheduled by weekday and daypart and assigned to a role rather than a person, so it survives
/// the roster changing.
/// </summary>
public class ComplianceChecklist : BaseEntity
{
    public Guid OutletId { get; set; }

    public string Name { get; set; } = string.Empty;
    public ChecklistFrequency Frequency { get; set; } = ChecklistFrequency.Daily;

    /// <summary>Comma-separated weekday numbers (0=Sun). Empty means every day.</summary>
    public string? ActiveDays { get; set; }

    /// <summary>When it becomes due. Null means any time during the day.</summary>
    public TimeSpan? DueAt { get; set; }

    /// <summary>Role responsible. Null means anyone may complete it.</summary>
    public StaffRole? AssignedRole { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<ChecklistItem> Items { get; set; } = [];
    public ICollection<ChecklistRun> Runs { get; set; } = [];
}

/// <summary>One line on a checklist, and what kind of answer it wants.</summary>
public class ChecklistItem : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public ComplianceChecklist? Checklist { get; set; }

    public string Text { get; set; } = string.Empty;
    public ChecklistAnswerType AnswerType { get; set; } = ChecklistAnswerType.YesNo;

    /// <summary>Set for a numeric answer that has to fall inside a range (a temperature, a pH).</summary>
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }

    /// <summary>A failed answer on a critical item blocks the whole run from being signed off.</summary>
    public bool IsCritical { get; set; }

    public int DisplayOrder { get; set; }
    public string? Guidance { get; set; }
}

/// <summary>One completion of a checklist on one day — the signed record.</summary>
public class ChecklistRun : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public ComplianceChecklist? Checklist { get; set; }

    public Guid OutletId { get; set; }

    public DateTime DueOn { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid? CompletedByStaffId { get; set; }
    public string? CompletedByStaffName { get; set; }

    public int PassCount { get; set; }
    public int FailCount { get; set; }

    /// <summary>True when a critical item failed — the run stays open until it is resolved.</summary>
    public bool HasCriticalFailure { get; set; }

    public string? CorrectiveAction { get; set; }
    public string? Note { get; set; }

    public ICollection<ChecklistAnswer> Answers { get; set; } = [];
}

/// <summary>One answer within a run.</summary>
public class ChecklistAnswer : BaseEntity
{
    public Guid RunId { get; set; }
    public ChecklistRun? Run { get; set; }

    public Guid ChecklistItemId { get; set; }
    public string ItemText { get; set; } = string.Empty;

    public bool? YesNoValue { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }

    public bool IsPass { get; set; }
    public bool IsCritical { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// A prepped batch and its shelf life. Answers the two questions asked in a recall — what went
/// into this, and what did it go into.
/// </summary>
public class PrepBatch : BaseEntity
{
    public Guid OutletId { get; set; }

    /// <summary>Label printed for the container.</summary>
    public string BatchCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
    public Guid? RecipeId { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }

    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";

    public DateTime PreparedAt { get; set; } = DateTime.UtcNow;
    public DateTime UseByAt { get; set; }

    public Guid? PreparedByStaffId { get; set; }
    public string? PreparedByStaffName { get; set; }

    /// <summary>Supplier lot(s) this batch was made from — the traceability link, upward.</summary>
    public string? SupplierBatchRefs { get; set; }

    public bool IsDiscarded { get; set; }
    public DateTime? DiscardedAt { get; set; }
    public string? DiscardReason { get; set; }

    public string? StorageLocation { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A delivery area. Previously the order only carried a zone *name*, which meant nothing enforced
/// it: an address five miles outside the radius was accepted and discovered by a rider. A zone is
/// a record with a fee, a minimum and a reach.
/// </summary>
public class DeliveryZone : BaseEntity
{
    public Guid OutletId { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public decimal MinimumOrderValue { get; set; }

    /// <summary>Straight-line reach from the outlet, in km. Zero means the zone is name-matched only.</summary>
    public decimal RadiusKm { get; set; }

    public int EstimatedMinutes { get; set; } = 30;

    /// <summary>Free delivery above this order value. Zero disables it.</summary>
    public decimal FreeDeliveryThreshold { get; set; }

    /// <summary>Postcodes or area names this zone covers, comma-separated.</summary>
    public string? CoveredAreas { get; set; }

    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
}
