using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A piece of land as the revenue record knows it, which is not the same thing as a property.
///
/// A scheme is assembled from many parcels bought from many owners; one parcel becomes four
/// hundred plots. Keeping the parcel separate from the plots means the title chain, the
/// encumbrances and the acquisition cost stay attached to the thing they actually belong to.
///
/// This is our *view* of the land, not the registry's. The government's record remains the system
/// of record, and every screen built on this says so.
/// </summary>
public class LandParcel : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string? Name { get; set; }

    // ── Revenue-record identity ──────────────────────────────────────────────
    // The vocabulary differs by market; the shape does not.

    public string? SurveyNumber { get; set; }
    public string? KhasraNumber { get; set; }
    public string? KhewatNumber { get; set; }
    public string? KhatuniNumber { get; set; }
    public string? Mouza { get; set; }
    public string? Village { get; set; }
    public string? Tehsil { get; set; }
    public string? District { get; set; }
    public string? RegistrarOffice { get; set; }

    // ── Area ─────────────────────────────────────────────────────────────────

    /// <summary>Area as the revenue record states it.</summary>
    public decimal RecordAreaSqFt { get; set; }

    /// <summary>Area the surveyor actually measured. It differs, and the difference is a real risk.</summary>
    public decimal? SurveyedAreaSqFt { get; set; }

    /// <summary>Surveyed minus record. Negative means we are buying less land than the paper says.</summary>
    public decimal? AreaVarianceSqFt { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public Guid? GeoAreaId { get; set; }

    // ── Status ───────────────────────────────────────────────────────────────

    public AcquisitionStageKind Stage { get; set; } = AcquisitionStageKind.Identified;
    public bool IsAcquired { get; set; }
    public DateOnly? AcquiredOn { get; set; }
    public Guid? ProjectId { get; set; }

    /// <summary>What it may lawfully be used for today.</summary>
    public string? CurrentLandUse { get; set; }

    /// <summary>What we intend it to become, and are applying to convert it to.</summary>
    public string? IntendedLandUse { get; set; }

    public decimal? MaxFloorAreaRatio { get; set; }
    public decimal? MaxCoveragePercent { get; set; }
    public decimal? MaxHeightFt { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal? AgreedPrice { get; set; }
    public decimal TotalAcquisitionCost { get; set; }

    /// <summary>What it costs us per month to hold this while it sits undeveloped.</summary>
    public decimal MonthlyHoldingCost { get; set; }

    public bool HasEncumbrance { get; set; }
    public bool HasLitigation { get; set; }

    public ICollection<TitleChainEntry> TitleChain { get; set; } = [];
    public ICollection<Encumbrance> Encumbrances { get; set; } = [];
    public ICollection<TitleVerificationItem> VerificationItems { get; set; } = [];
}

/// <summary>
/// One link in the chain of who owned this land before us. Chronological, never edited, because
/// a broken chain is the single most expensive defect a developer can discover late.
/// </summary>
public class TitleChainEntry : BaseEntity
{
    public Guid LandParcelId { get; set; }
    public LandParcel? Parcel { get; set; }

    public int SequenceNumber { get; set; }
    public TitleInstrument Instrument { get; set; }
    public DateOnly InstrumentDate { get; set; }
    public string? InstrumentNumber { get; set; }
    public string? RegistrarOffice { get; set; }

    public string? TransferorName { get; set; }
    public string? TransfereeName { get; set; }
    public decimal? Consideration { get; set; }

    public string? DocumentUrl { get; set; }
    public VerificationVerdict Verdict { get; set; } = VerificationVerdict.Pending;
    public string? VerificationNote { get; set; }
}

/// <summary>
/// Anything that limits what we may do with the land. Held against the parcel and mirrored onto
/// units in a scheme so a mortgaged plot cannot be quietly sold twice.
/// </summary>
public class Encumbrance : BaseEntity
{
    public Guid? LandParcelId { get; set; }
    public LandParcel? Parcel { get; set; }

    /// <summary>Set when the charge is against a specific unit rather than the whole parcel.</summary>
    public Guid? PropertyId { get; set; }

    public EncumbranceKind Kind { get; set; }
    public EncumbranceStatus Status { get; set; } = EncumbranceStatus.Active;

    public string? HolderName { get; set; }
    public Guid? HolderPartyId { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? CreatedOn { get; set; }
    public DateOnly? ExpectedClearanceDate { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Note { get; set; }

    /// <summary>Refuse a sale, booking or transfer while this stands, rather than only warning.</summary>
    public bool BlocksTransaction { get; set; } = true;
}

/// <summary>
/// One line of the due-diligence checklist. A parcel is not clean because somebody said so; it is
/// clean because every one of these passed and the evidence is filed.
/// </summary>
public class TitleVerificationItem : BaseEntity
{
    public Guid LandParcelId { get; set; }
    public LandParcel? Parcel { get; set; }

    /// <summary>"TitleSearch", "RevenueExtract", "NonEncumbrance", "MutationStatus",
    /// "LandUseClassification", "MasterPlanCompliance", "LitigationSearch", "PhysicalPossession".</summary>
    public string CheckKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public VerificationVerdict Verdict { get; set; } = VerificationVerdict.Pending;

    public Guid? AssignedToUserId { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? Findings { get; set; }

    /// <summary>Set on a conditional pass: what must happen before completion.</summary>
    public string? Condition { get; set; }

    public bool IsMandatory { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// The pipeline of buying a parcel. Separate from the parcel because a deal can be attempted,
/// abandoned, and attempted again two years later at a different price with a different seller.
/// </summary>
public class LandAcquisition : BaseEntity
{
    public Guid LandParcelId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public Guid? SellerPartyId { get; set; }
    public string? SellerName { get; set; }

    public AcquisitionStageKind Stage { get; set; } = AcquisitionStageKind.Identified;
    public DateOnly? StartedOn { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? OfferedPrice { get; set; }
    public decimal? AgreedPrice { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal TotalPaid { get; set; }

    public Guid? OwnerUserId { get; set; }

    /// <summary>Why it stalled. The hold-out parcel in an assembly is the thing a board asks about.</summary>
    public string? BlockingIssue { get; set; }

    public bool IsAborted { get; set; }
    public Guid? AbortReasonCodeId { get; set; }

    public ICollection<AcquisitionStage> Stages { get; set; } = [];
    public ICollection<AcquisitionCostLine> CostLines { get; set; } = [];
}

public class AcquisitionStage : BaseEntity
{
    public Guid LandAcquisitionId { get; set; }
    public LandAcquisition? Acquisition { get; set; }

    public AcquisitionStageKind Kind { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public Guid? OwnerUserId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// One component of what the land really cost. The headline price is rarely more than
/// three-quarters of it, and unit costing is wrong if the rest is not captured.
/// </summary>
public class AcquisitionCostLine : BaseEntity
{
    public Guid LandAcquisitionId { get; set; }
    public LandAcquisition? Acquisition { get; set; }

    /// <summary>"LandPrice", "StampDuty", "Registration", "Brokerage", "Legal", "Mutation",
    /// "Demarcation", "BoundaryWall", "Security", "Interest", "Compensation".</summary>
    public string CostType { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public Guid? PayeePartyId { get; set; }
    public string? Reference { get; set; }
    public bool IsCapitalised { get; set; } = true;
}

/// <summary>A survey commissioned on a parcel, and what came back.</summary>
public class SurveyInstruction : BaseEntity
{
    public Guid LandParcelId { get; set; }
    public string Reference { get; set; } = string.Empty;

    public Guid? SurveyorPartyId { get; set; }
    public string? SurveyorName { get; set; }
    public DateOnly InstructedOn { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public decimal? MeasuredAreaSqFt { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public string? DrawingUrl { get; set; }
    public string? ReportUrl { get; set; }
    public decimal? Fee { get; set; }

    /// <summary>Encroachment, a boundary that does not match the record, a neighbour's wall on our line.</summary>
    public string? DisputesFound { get; set; }
}

/// <summary>A zoning or land-use application, and where it has got to.</summary>
public class LandUseRecord : BaseEntity
{
    public Guid LandParcelId { get; set; }

    public string? FromUse { get; set; }
    public string? ToUse { get; set; }
    public string? Authority { get; set; }
    public string? ApplicationNumber { get; set; }
    public ApprovalState State { get; set; } = ApprovalState.NotStarted;

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? GrantedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public decimal? ConversionFee { get; set; }
    public string? DocumentUrl { get; set; }
    public string? Conditions { get; set; }
}
