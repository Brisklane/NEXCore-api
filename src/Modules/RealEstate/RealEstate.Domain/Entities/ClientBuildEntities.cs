using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// The customer owns the land, gives us money, and we build on it.
///
/// Neither a unit sale nor a subcontract, and modelling it as either breaks. It is not a unit sale
/// because there is no inventory — the asset is already the customer's and we never own it. It is
/// not a subcontract because the customer is the client rather than the main contractor, they
/// choose the finishes, they change their mind, and they take handover with a defect liability.
///
/// The dominant South Asian shape is a rate per covered square foot at a specification grade, with
/// milestone payments and a running variation register — so that is what this is built around.
/// </summary>
public class ClientBuildContract : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid ClientPartyId { get; set; }
    public Guid? CoClientPartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? EstimateId { get; set; }
    public Guid? OfficeId { get; set; }

    // ── The site ─────────────────────────────────────────────────────────────

    public Guid? PropertyId { get; set; }
    public Guid? LandParcelId { get; set; }

    /// <summary>The client bought the plot through us as well, so the two records are chained.</summary>
    public Guid? SourceBookingId { get; set; }

    public string? SiteAddress { get; set; }
    public decimal PlotAreaSqFt { get; set; }

    // ── Commercials ──────────────────────────────────────────────────────────

    public ContractKind Kind { get; set; } = ContractKind.PerAreaUnitRate;
    public SpecificationGrade Grade { get; set; } = SpecificationGrade.Standard;
    public Guid? SpecificationScheduleId { get; set; }

    /// <summary>Covered area the contract is priced on. Re-measures when the drawing changes.</summary>
    public decimal CoveredAreaSqFt { get; set; }

    public decimal RatePerSqFt { get; set; }
    public decimal ContractValue { get; set; }
    public decimal ApprovedVariations { get; set; }
    public decimal RevisedContractValue { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Set for cost-plus, where the fee rather than the price is the agreed number.</summary>
    public decimal? FeePercent { get; set; }
    public decimal? FixedFee { get; set; }
    public decimal? GuaranteedMaximumPrice { get; set; }

    // ── Status ───────────────────────────────────────────────────────────────

    /// <summary>"Draft", "Signed", "Design", "InProgress", "Suspended", "Completed",
    /// "HandedOver", "InDefectsPeriod", "Closed", "Terminated".</summary>
    public string Status { get; set; } = "Draft";

    public DateOnly? SignedOn { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? ForecastCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public int ExtensionDaysGranted { get; set; }

    public decimal LiquidatedDamagesPerDay { get; set; }
    public decimal LiquidatedDamagesCapPercent { get; set; }

    // ── Money in ─────────────────────────────────────────────────────────────

    public Guid? PaymentPlanId { get; set; }
    public decimal TotalDemanded { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal Outstanding { get; set; }
    public decimal AdvanceReceived { get; set; }

    /// <summary>The client holds retention on our certificates, exactly as we would on a subcontractor's.</summary>
    public decimal RetentionPercent { get; set; }
    public decimal RetentionHeldByClient { get; set; }
    public decimal RetentionReleased { get; set; }

    // ── Cost & margin ────────────────────────────────────────────────────────

    public Guid? ConstructionProjectId { get; set; }
    public decimal BudgetCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal ForecastFinalCost { get; set; }
    public decimal ForecastMargin { get; set; }
    public decimal ProgressPercent { get; set; }

    // ── Handover ─────────────────────────────────────────────────────────────

    public int DefectsPeriodMonths { get; set; } = 12;
    public Guid? HandoverId { get; set; }
    public Guid? SnagInspectionId { get; set; }

    public Guid? ProjectManagerUserId { get; set; }
    public Guid? ArchitectPartyId { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? SignatureSessionId { get; set; }

    public bool ClientPortalEnabled { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<ContractScopeItem> ScopeItems { get; set; } = [];
    public ICollection<ClientSuppliedMaterial> ClientSuppliedMaterials { get; set; } = [];
}

/// <summary>
/// What is and is not in the price. Exclusions matter as much as inclusions here — a client who
/// assumed the boundary wall was included and finds it was not is the classic dispute.
/// </summary>
public class ContractScopeItem : BaseEntity
{
    public Guid ClientBuildContractId { get; set; }
    public ClientBuildContract? Contract { get; set; }

    /// <summary>"Structure", "Finishes", "Electrical", "Plumbing", "Hvac", "External",
    /// "Landscaping", "BoundaryWall", "Approvals", "Utilities", "Furniture".</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>False makes it an explicit exclusion, which is what stops the argument.</summary>
    public bool IsIncluded { get; set; } = true;

    public decimal? Value { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Material the client buys themselves — steel, cement, sanitary ware. Excluded from our rate, and
/// logged at the gate when it arrives, so a shortfall is theirs and not ours.
/// </summary>
public class ClientSuppliedMaterial : BaseEntity
{
    public Guid ClientBuildContractId { get; set; }
    public ClientBuildContract? Contract { get; set; }

    public Guid? ItemId { get; set; }
    public string Uom { get; set; } = string.Empty;
    public decimal AgreedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal? EstimatedValue { get; set; }

    /// <summary>The amount taken out of our rate because the client is buying it.</summary>
    public decimal RateExclusionAmount { get; set; }

    public DateOnly? ExpectedBy { get; set; }
    public DateOnly? LastReceivedOn { get; set; }

    /// <summary>Client material that has not arrived, holding up the programme. A delay event.</summary>
    public bool IsDelayingWork { get; set; }
    public Guid? DelayEventId { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// The customer changing their mind: better marble, an extra room, a wall moved.
///
/// The number one cause of disputes on a client build, and the number one thing to make
/// undeniable — quoted, approved in writing, priced, and reflected in the next demand.
/// </summary>
public class ClientVariation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ClientBuildContractId { get; set; }
    public Guid? VariationOrderId { get; set; }
    public Guid? SpecificationItemId { get; set; }

    public string Title { get; set; } = string.Empty;
    public VariationOrigin Origin { get; set; } = VariationOrigin.ClientRequest;
    public VariationStatus Status { get; set; } = VariationStatus.Proposed;

    public DateOnly RequestedOn { get; set; }
    public Guid? RequestedByPartyId { get; set; }

    /// <summary>Came in through the client portal rather than a site conversation. Traceable by construction.</summary>
    public bool RaisedViaPortal { get; set; }

    // ── Price ────────────────────────────────────────────────────────────────

    public decimal QuotedAmount { get; set; }
    public decimal? OmissionCredit { get; set; }
    public decimal NetAmount { get; set; }
    public int TimeImpactDays { get; set; }
    public DateOnly? QuotedOn { get; set; }
    public DateOnly? QuoteValidUntil { get; set; }

    // ── Approval — the part that makes it collectable ────────────────────────

    public bool ClientApproved { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public Guid? SignatureSessionId { get; set; }
    public string? ApprovalEvidenceUrl { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>Instructed and built. Only now can it be billed.</summary>
    public bool IsExecuted { get; set; }
    public DateOnly? ExecutedOn { get; set; }

    public bool IsBilled { get; set; }
    public Guid? DemandId { get; set; }
    public string? BeforeAfterPhotoUrls { get; set; }
}

/// <summary>The drawings register, versioned, because building to revision C when D exists is expensive.</summary>
public class DrawingRegister : BaseEntity
{
    public string DrawingNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public Guid? ClientBuildContractId { get; set; }
    public Guid? ConstructionProjectId { get; set; }
    public Guid? ProjectId { get; set; }

    /// <summary>"Architectural", "Structural", "Electrical", "Plumbing", "Hvac", "Landscape", "AsBuilt".</summary>
    public string Discipline { get; set; } = "Architectural";

    public string? Scale { get; set; }
    public Guid? PreparedByPartyId { get; set; }

    public string CurrentRevision { get; set; } = "A";
    public DateOnly? CurrentRevisionDate { get; set; }

    /// <summary>"Draft", "ForReview", "ForApproval", "Approved", "ForConstruction", "Superseded".</summary>
    public string Status { get; set; } = "Draft";

    public bool ClientApproved { get; set; }
    public DateOnly? ClientApprovedOn { get; set; }

    /// <summary>Scope is frozen at this point. Everything after it is a variation.</summary>
    public bool IsFrozen { get; set; }

    public string? CurrentFileUrl { get; set; }
    public bool IsIssuedToSite { get; set; }
    public DateOnly? IssuedToSiteOn { get; set; }

    public ICollection<DrawingRevision> Revisions { get; set; } = [];
}

public class DrawingRevision : BaseEntity
{
    public Guid DrawingRegisterId { get; set; }
    public DrawingRegister? Drawing { get; set; }

    public string Revision { get; set; } = string.Empty;
    public DateOnly RevisionDate { get; set; }
    public string? ChangeDescription { get; set; }
    public string? FileUrl { get; set; }

    public Guid? IssuedByUserId { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public bool IsSuperseded { get; set; }

    /// <summary>The change carried a cost, so the variation is raised rather than absorbed.</summary>
    public bool HasCostImpact { get; set; }
    public Guid? VariationOrderId { get; set; }
}

/// <summary>
/// Budget, committed, actual, forecast and margin for one contract. The screen a project manager
/// on a client build opens first, and the one that tells them if the job has gone wrong.
/// </summary>
public class ContractCostSheet : BaseEntity
{
    public Guid ClientBuildContractId { get; set; }
    public Guid? ConstructionProjectId { get; set; }

    public DateOnly AsOfDate { get; set; }

    public decimal ContractValue { get; set; }
    public decimal VariationsApproved { get; set; }
    public decimal RevisedValue { get; set; }

    public decimal BudgetCost { get; set; }
    public decimal CommittedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal CostToComplete { get; set; }
    public decimal ForecastFinalCost { get; set; }

    public decimal BudgetMargin { get; set; }
    public decimal ForecastMargin { get; set; }
    public decimal MarginPercent { get; set; }

    /// <summary>Budget margin less forecast margin. The number that has to be explained by cause.</summary>
    public decimal MarginErosion { get; set; }

    // Where the erosion went. Guessing at this is how the same mistake repeats on the next job.
    public decimal ErosionFromVariationsAbsorbed { get; set; }
    public decimal ErosionFromWastage { get; set; }
    public decimal ErosionFromRework { get; set; }
    public decimal ErosionFromDelay { get; set; }
    public decimal ErosionFromRateIncrease { get; set; }
    public decimal ErosionOther { get; set; }

    public decimal ProgressPercent { get; set; }
    public decimal CertifiedValue { get; set; }
    public decimal CollectedValue { get; set; }

    /// <summary>Collected less cost incurred. Whether the job is funding itself or we are.</summary>
    public decimal CashPosition { get; set; }

    public Guid? PreparedByUserId { get; set; }
    public string? Commentary { get; set; }

    public ICollection<ContractCostLine> Lines { get; set; } = [];
}

public class ContractCostLine : BaseEntity
{
    public Guid ContractCostSheetId { get; set; }
    public ContractCostSheet? CostSheet { get; set; }

    public Guid? WbsNodeId { get; set; }
    public string CostHead { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }

    public string? VarianceExplanation { get; set; }
    public int SortOrder { get; set; }
}
