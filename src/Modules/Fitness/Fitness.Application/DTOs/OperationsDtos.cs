using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Incidents ────────────────────────────────────────────────────────────────

public class IncidentDto
{
    public Guid Id { get; set; }
    public string IncidentNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }
    public Guid? EquipmentAssetId { get; set; }
    public string? EquipmentName { get; set; }

    public IncidentKind Kind { get; set; }
    public IncidentSeverity Severity { get; set; }
    public IncidentStatus Status { get; set; }

    public DateTime OccurredAt { get; set; }
    public DateTime ReportedAt { get; set; }

    /// <summary>Gap between the two. A long one is itself a finding.</summary>
    public int ReportingDelayMinutes { get; set; }

    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? InvolvedPersonName { get; set; }
    public string? InvolvedPersonPhone { get; set; }
    public Guid? ReportedByStaffId { get; set; }
    public string? ReportedByName { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? WitnessNames { get; set; }
    public string? WitnessStatements { get; set; }

    public bool FirstAidGiven { get; set; }
    public string? FirstAiderName { get; set; }
    public bool AedUsed { get; set; }
    public bool AmbulanceCalled { get; set; }
    public bool HospitalAttended { get; set; }
    public string? ImmediateAction { get; set; }
    public List<string> PhotoUrls { get; set; } = [];

    public Guid? OwnerStaffId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime? ReviewDueOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public string? RootCause { get; set; }
    public string? PreventiveAction { get; set; }

    public bool IsReportable { get; set; }
    public bool WasReported { get; set; }
    public DateTime? ReportedToAuthorityOn { get; set; }
    public string? AuthorityReference { get; set; }
    public bool InsurerNotified { get; set; }
    public string? InsurerReference { get; set; }
    public decimal? EstimatedCost { get; set; }

    public List<IncidentActionDto> Actions { get; set; } = [];
    public int OpenActions { get; set; }
    public bool IsOverdue { get; set; }
}

public class SaveIncidentDto
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? EquipmentAssetId { get; set; }

    public IncidentKind Kind { get; set; }
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Minor;
    public DateTime OccurredAt { get; set; }

    public Guid? MemberId { get; set; }
    public string? InvolvedPersonName { get; set; }
    public string? InvolvedPersonPhone { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? WitnessNames { get; set; }
    public string? WitnessStatements { get; set; }

    public bool FirstAidGiven { get; set; }
    public string? FirstAiderName { get; set; }
    public bool AedUsed { get; set; }
    public bool AmbulanceCalled { get; set; }
    public bool HospitalAttended { get; set; }
    public string? ImmediateAction { get; set; }
    public List<string> PhotoUrls { get; set; } = [];

    public Guid? OwnerStaffId { get; set; }
    public DateTime? ReviewDueOn { get; set; }
    public bool IsReportable { get; set; }
    public bool InsurerNotified { get; set; }
    public decimal? EstimatedCost { get; set; }

    /// <summary>Takes the machine out of service in the same action, for an equipment incident.</summary>
    public bool TakeEquipmentOutOfService { get; set; }
}

public class IncidentActionDto
{
    public Guid Id { get; set; }
    public Guid IncidentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }
    public DateTime RaisedOn { get; set; }
    public DateTime? DueOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public string? CompletionNote { get; set; }
    public bool IsOverdue { get; set; }
}

// ── Complaints & lost property ───────────────────────────────────────────────

public class ComplaintDto
{
    public Guid Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? ComplainantName { get; set; }
    public string? ComplainantContact { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public ComplaintStatus Status { get; set; }
    public int Priority { get; set; }

    public DateTime RaisedOn { get; set; }
    public DateTime? AcknowledgedOn { get; set; }
    public DateTime? TargetResolutionOn { get; set; }
    public DateTime? ResolvedOn { get; set; }
    public int AgeDays { get; set; }
    public bool IsOverdue { get; set; }
    public int? ResolutionDays { get; set; }

    public Guid? OwnerStaffId { get; set; }
    public string? OwnerName { get; set; }
    public string? Resolution { get; set; }
    public decimal? CompensationValue { get; set; }
    public string? CompensationNote { get; set; }
    public bool? ComplainantSatisfied { get; set; }
    public string? Channel { get; set; }
}

public class SaveComplaintDto
{
    public Guid ClubId { get; set; }
    public Guid? MemberId { get; set; }
    public string? ComplainantName { get; set; }
    public string? ComplainantContact { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int Priority { get; set; } = 2;
    public Guid? OwnerStaffId { get; set; }
    public DateTime? TargetResolutionOn { get; set; }
    public string? Channel { get; set; }
}

public class ResolveComplaintDto
{
    public Guid ComplaintId { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public decimal? CompensationValue { get; set; }
    public string? CompensationNote { get; set; }
    public bool? ComplainantSatisfied { get; set; }

    /// <summary>Puts the goodwill on the member's account rather than leaving it as a note.</summary>
    public bool IssueCredit { get; set; }
}

public class LostPropertyItemDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? PhotoUrl { get; set; }

    public DateTime FoundOn { get; set; }
    public string? FoundLocation { get; set; }
    public string? FoundByName { get; set; }
    public string? StorageLocation { get; set; }
    public LostPropertyStatus Status { get; set; }

    public Guid? ClaimedByMemberId { get; set; }
    public string? ClaimedByName { get; set; }
    public DateTime? ClaimedOn { get; set; }
    public string? ReleasedByName { get; set; }

    public DateTime? DisposeAfter { get; set; }
    public bool ReadyForDisposal { get; set; }
    public DateTime? DisposedOn { get; set; }
    public string? DisposalNote { get; set; }
    public int DaysHeld { get; set; }
}

public class SaveLostPropertyDto
{
    public Guid ClubId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime? FoundOn { get; set; }
    public string? FoundLocation { get; set; }
    public string? StorageLocation { get; set; }
    public int HoldDays { get; set; } = 30;
}

public class ClaimLostPropertyDto
{
    public Guid ItemId { get; set; }
    public Guid? ClaimedByMemberId { get; set; }
    public string? ClaimedByName { get; set; }
    public string? Note { get; set; }
}

// ── Checks & handover ────────────────────────────────────────────────────────

public class FacilityCheckDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public string? AreaName { get; set; }

    public string Name { get; set; } = string.Empty;
    public FacilityCheckKind Kind { get; set; }
    public int DaysOfWeekMask { get; set; }
    public TimeSpan DueAt { get; set; }
    public int TimesPerDay { get; set; }

    public Guid? DefaultAssigneeRoleId { get; set; }
    public bool AlertOnMissed { get; set; }
    public int MissedAfterMinutes { get; set; }
    public bool RequiresSignature { get; set; }
    public bool IsActive { get; set; }

    public List<FacilityCheckItemDto> Items { get; set; } = [];

    // Today's state, which is what the board shows.
    public bool DueToday { get; set; }
    public bool CompletedToday { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public string? LastCompletedByName { get; set; }
    public int FailedItemCount { get; set; }
}

public class FacilityCheckItemDto
{
    public Guid Id { get; set; }
    public Guid FacilityCheckId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public ScreeningAnswerKind AnswerKind { get; set; }
    public string? Unit { get; set; }
    public decimal? AcceptableLow { get; set; }
    public decimal? AcceptableHigh { get; set; }
    public bool IsCritical { get; set; }
    public bool RequiresPhoto { get; set; }

    public DateTime? LastCompletedAt { get; set; }
    public string? LastCompletedByName { get; set; }
    public bool? LastPassed { get; set; }
    public decimal? LastValue { get; set; }
    public string? LastNote { get; set; }
    public bool OutOfRange { get; set; }
}

public class SubmitFacilityCheckDto
{
    public Guid FacilityCheckId { get; set; }
    public Guid ClubId { get; set; }
    public List<SubmitCheckItemDto> Items { get; set; } = [];
    public string? SignatureUrl { get; set; }
    public string? Note { get; set; }
}

public class SubmitCheckItemDto
{
    public Guid ItemId { get; set; }
    public bool? Passed { get; set; }
    public decimal? Value { get; set; }
    public string? Note { get; set; }
    public string? PhotoUrl { get; set; }
}

public class ShiftHandoverDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public DateTime ShiftEndedAt { get; set; }
    public Guid? FromStaffId { get; set; }
    public string? FromStaffName { get; set; }
    public Guid? ToStaffId { get; set; }
    public string? ToStaffName { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string? OutstandingItems { get; set; }
    public bool HasUrgentItems { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByName { get; set; }
}

// ── Retail ───────────────────────────────────────────────────────────────────

public class FitnessSaleDto
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }

    public DateTime SoldAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public PaymentMethod PaymentMethod { get; set; }
    public bool IsHouseAccountCharge { get; set; }
    public Guid? CashSessionId { get; set; }
    public string? SoldByName { get; set; }

    public bool IsReturn { get; set; }
    public Guid? ReturnsSaleId { get; set; }
    public string? ReturnReason { get; set; }
    public bool StockDepleted { get; set; }

    public string? DiscountReason { get; set; }
    public string? DiscountApprovedByName { get; set; }

    public List<FitnessSaleLineDto> Lines { get; set; } = [];

    /// <summary>Sale value less line cost, which is the number a pro shop is judged on.</summary>
    public decimal GrossMargin { get; set; }
}

public class FitnessSaleLineDto
{
    public Guid Id { get; set; }
    public Guid? InventoryItemId { get; set; }
    public Guid? PlanId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal UnitCost { get; set; }
    public string? Modifiers { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateSaleDto
{
    public Guid ClubId { get; set; }
    public Guid? MemberId { get; set; }
    public List<CreateSaleLineDto> Lines { get; set; } = [];

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal? AmountTendered { get; set; }
    public Guid? CashSessionId { get; set; }

    /// <summary>Puts it on the member's account instead of taking payment now.</summary>
    public bool ChargeToHouseAccount { get; set; }

    public decimal DiscountTotal { get; set; }
    public string? DiscountReason { get; set; }
    public Guid? DiscountApprovedByUserId { get; set; }

    public Guid? SoldByStaffId { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class CreateSaleLineDto
{
    public Guid? InventoryItemId { get; set; }
    public Guid? PlanId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public string? Modifiers { get; set; }
}

public class HouseAccountChargeDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid ClubId { get; set; }
    public Guid? SaleId { get; set; }
    public string? SaleNumber { get; set; }
    public DateTime ChargedOn { get; set; }
    public decimal Amount { get; set; }
    public string ChargeDescription { get; set; } = string.Empty;
    public Guid? SettledInvoiceId { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledOn { get; set; }
    public string? AuthorisedByName { get; set; }
}

public class VendingRevenueEntryDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string RevenueSource { get; set; } = string.Empty;
    public string? MachineReference { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal NetRevenue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int? TransactionCount { get; set; }
    public string? EnteredByName { get; set; }
    public string? Note { get; set; }
}

// ── Audit ────────────────────────────────────────────────────────────────────

public class AuditEntryDto
{
    public Guid Id { get; set; }
    public Guid? ClubId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? ChangeSummary { get; set; }
    public bool IsSensitiveAccess { get; set; }
    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
}
