using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Temperature ──────────────────────────────────────────────────────────────

public class TemperatureCheckpointDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemperatureCheckpointKind Kind { get; set; }
    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }
    public int CheckIntervalHours { get; set; }
    public int DisplayOrder { get; set; }
    public string? Location { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    /// <summary>Latest reading, so the list doubles as the live board.</summary>
    public decimal? LastReadingCelsius { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public bool LastReadingOutOfRange { get; set; }

    /// <summary>True when the interval has elapsed since the last reading.</summary>
    public bool IsDue { get; set; }
    public int OpenBreachCount { get; set; }
}

public class SaveTemperatureCheckpointDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TemperatureCheckpointKind Kind { get; set; } = TemperatureCheckpointKind.Refrigerator;
    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }
    public int CheckIntervalHours { get; set; } = 12;
    public int DisplayOrder { get; set; }
    public string? Location { get; set; }
    public Guid? StationId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class TemperatureLogDto
{
    public Guid Id { get; set; }
    public Guid CheckpointId { get; set; }
    public string? CheckpointName { get; set; }
    public TemperatureCheckpointKind CheckpointKind { get; set; }
    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }
    public Guid OutletId { get; set; }
    public decimal ReadingCelsius { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public bool IsOutOfRange { get; set; }
    public string? CorrectiveAction { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Note { get; set; }
}

public class RecordTemperatureDto
{
    public Guid CheckpointId { get; set; }
    public decimal ReadingCelsius { get; set; }
    public Guid? StaffId { get; set; }

    /// <summary>Required when the reading falls outside the checkpoint's safe range.</summary>
    public string? CorrectiveAction { get; set; }
    public string? Note { get; set; }
}

public class ResolveBreachDto
{
    public Guid LogId { get; set; }
    public string CorrectiveAction { get; set; } = string.Empty;
    public Guid? StaffId { get; set; }
}

// ── Checklists ───────────────────────────────────────────────────────────────

public class ChecklistDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChecklistFrequency Frequency { get; set; }
    public string? ActiveDays { get; set; }
    public TimeSpan? DueAt { get; set; }
    public StaffRole? AssignedRole { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = [];

    /// <summary>Today's run, when there is one — what the task board reads.</summary>
    public Guid? TodayRunId { get; set; }
    public bool IsDueToday { get; set; }
    public bool IsCompletedToday { get; set; }
    public bool HasOpenCriticalFailure { get; set; }
}

public class SaveChecklistDto
{
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChecklistFrequency Frequency { get; set; } = ChecklistFrequency.Daily;
    public string? ActiveDays { get; set; }
    public TimeSpan? DueAt { get; set; }
    public StaffRole? AssignedRole { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = [];
}

public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string Text { get; set; } = string.Empty;
    public ChecklistAnswerType AnswerType { get; set; } = ChecklistAnswerType.YesNo;
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }
    public bool IsCritical { get; set; }
    public int DisplayOrder { get; set; }
    public string? Guidance { get; set; }
}

public class ChecklistRunDto
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string? ChecklistName { get; set; }
    public ChecklistFrequency Frequency { get; set; }
    public Guid OutletId { get; set; }
    public DateTime DueOn { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByStaffId { get; set; }
    public string? CompletedByStaffName { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public bool HasCriticalFailure { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? Note { get; set; }
    public List<ChecklistAnswerDto> Answers { get; set; } = [];
}

public class ChecklistAnswerDto
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid checklistItemId { get; set; }
    public string ItemText { get; set; } = string.Empty;
    public ChecklistAnswerType AnswerType { get; set; }
    public bool? YesNoValue { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool IsPass { get; set; }
    public bool IsCritical { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTime AnsweredAt { get; set; }
    public int DisplayOrder { get; set; }
}

public class SubmitChecklistRunDto
{
    public Guid ChecklistId { get; set; }
    public Guid? RunId { get; set; }
    public Guid OutletId { get; set; }
    public Guid? StaffId { get; set; }
    public string? Note { get; set; }
    public string? CorrectiveAction { get; set; }
    public List<ChecklistAnswerDto> Answers { get; set; } = [];
}

/// <summary>What is due, overdue or breached right now — the compliance home screen.</summary>
public class ComplianceBoardDto
{
    public Guid OutletId { get; set; }
    public DateTime GeneratedAt { get; set; }

    public int ChecksDue { get; set; }
    public int ChecksOverdue { get; set; }
    public int TemperatureChecksDue { get; set; }
    public int OpenBreaches { get; set; }
    public int BatchesExpiringSoon { get; set; }
    public int BatchesExpired { get; set; }

    /// <summary>Share of today's due checks that have been completed.</summary>
    public decimal CompliancePercent { get; set; }

    public List<TemperatureCheckpointDto> Checkpoints { get; set; } = [];
    public List<ChecklistDto> Checklists { get; set; } = [];
    public List<TemperatureLogDto> OpenBreachLogs { get; set; } = [];
    public List<PrepBatchDto> ExpiringBatches { get; set; } = [];
}

// ── Prep batches ─────────────────────────────────────────────────────────────

public class PrepBatchDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public Guid? RecipeId { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";
    public DateTime PreparedAt { get; set; }
    public DateTime UseByAt { get; set; }
    public Guid? PreparedByStaffId { get; set; }
    public string? PreparedByStaffName { get; set; }
    public string? SupplierBatchRefs { get; set; }
    public bool IsDiscarded { get; set; }
    public DateTime? DiscardedAt { get; set; }
    public string? DiscardReason { get; set; }
    public string? StorageLocation { get; set; }
    public string? Note { get; set; }

    /// <summary>Negative once the use-by has passed — the label turns red.</summary>
    public int HoursRemaining { get; set; }
    public bool IsExpired { get; set; }
}

public class SavePrepBatchDto
{
    public Guid OutletId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? RecipeId { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";
    public DateTime? PreparedAt { get; set; }

    /// <summary>Shelf life. Either this or an explicit use-by must be supplied.</summary>
    public int? ShelfLifeHours { get; set; }
    public DateTime? UseByAt { get; set; }

    public Guid? PreparedByStaffId { get; set; }
    public string? SupplierBatchRefs { get; set; }
    public string? StorageLocation { get; set; }
    public string? Note { get; set; }
}

// ── Delivery zones ───────────────────────────────────────────────────────────

public class DeliveryZoneDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DeliveryFee { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public decimal RadiusKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public decimal FreeDeliveryThreshold { get; set; }
    public string? CoveredAreas { get; set; }
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

/// <summary>Answers "can we deliver there, and what does it cost" before an order is taken.</summary>
public class DeliveryQuoteDto
{
    public bool CanDeliver { get; set; }
    public string? Reason { get; set; }
    public Guid? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal MinimumOrderValue { get; set; }
    public int EstimatedMinutes { get; set; }
    public decimal DistanceKm { get; set; }
    public bool QualifiesForFreeDelivery { get; set; }
}

public class DeliveryQuoteRequestDto
{
    public Guid OutletId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Postcode or area name, matched against each zone's covered areas.</summary>
    public string? Area { get; set; }

    /// <summary>Basket value so far, so the minimum and the free-delivery threshold can be judged.</summary>
    public decimal OrderValue { get; set; }
}
