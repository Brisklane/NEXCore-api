using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A brokerage transaction from agreed price to completion.
///
/// The agency equivalent of a Booking: everything between "they said yes" and "we got paid" —
/// which in a sale market is three months of chasing solicitors, surveyors and lenders, and is
/// where agencies lose fees they had already counted.
/// </summary>
public class Deal : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? InstructionId { get; set; }

    public ListingKind Kind { get; set; } = ListingKind.ForSale;
    public DealStatus Status { get; set; } = DealStatus.Agreed;

    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }
    public Guid? ListingAgentId { get; set; }
    public Guid? SellingAgentId { get; set; }
    public Guid? OfficeId { get; set; }

    public decimal AgreedPrice { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public decimal DepositAmount { get; set; }
    public bool DepositReceived { get; set; }
    public Guid? DepositReceiptId { get; set; }

    // ── Fee ──────────────────────────────────────────────────────────────────

    public decimal GrossFee { get; set; }
    public decimal FeePercent { get; set; }
    public decimal FeeTax { get; set; }

    /// <summary>Split between the two sides where both are ours, or where a co-broke applies.</summary>
    public decimal ListingSideFee { get; set; }
    public decimal SellingSideFee { get; set; }

    public bool FeeInvoiced { get; set; }
    public Guid? FeeInvoiceId { get; set; }
    public bool FeeReceived { get; set; }
    public Guid? CommissionCalculationId { get; set; }

    // ── Dates ────────────────────────────────────────────────────────────────

    public DateOnly AgreedOn { get; set; }
    public DateOnly? TargetExchangeDate { get; set; }
    public DateOnly? ActualExchangeDate { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }

    /// <summary>Days since agreement with nothing moving. The stalled-deal board sorts on it.</summary>
    public int DaysInProgress { get; set; }
    public int? DaysSinceLastMilestone { get; set; }

    public Guid? ChainId { get; set; }
    public int? ChainPosition { get; set; }
    public Guid? FallThroughRecordId { get; set; }
    public Guid? ConveyancingId { get; set; }
    public Guid? ResultingTenancyId { get; set; }

    public string? Notes { get; set; }

    public ICollection<DealParty> Parties { get; set; } = [];
    public ICollection<DealChecklistItem> Checklist { get; set; } = [];
    public ICollection<DealMilestone> Milestones { get; set; } = [];
}

/// <summary>
/// Everyone involved in one transaction, with a contact. The "who do I chase today" list, and the
/// reason a progression clerk stops keeping a private spreadsheet.
/// </summary>
public class DealParty : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal? Deal { get; set; }

    public DealPartyRole Role { get; set; }
    public Guid? PartyId { get; set; }
    public string? OrganisationName { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Reference { get; set; }

    public DateTime? LastContactedAt { get; set; }
    public bool IsResponsive { get; set; } = true;
    public string? Note { get; set; }
}

/// <summary>One step of the progression, with an owner and a due date so it can be chased.</summary>
public class DealChecklistItem : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal? Deal { get; set; }

    /// <summary>"MemorandumIssued", "SolicitorsInstructed", "IdChecks", "SearchesOrdered",
    /// "SurveyBooked", "SurveyReceived", "MortgageApplied", "ValuationDone", "MortgageOffer",
    /// "EnquiriesRaised", "EnquiriesAnswered", "ContractsApproved", "DepositReceived",
    /// "Exchange", "Completion", "KeysReleased", "RegistrationFiled", "FeeInvoiced", "FeeReceived".</summary>
    public string StepKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public Guid? OwnerUserId { get; set; }
    public DealPartyRole? ResponsibleParty { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsMandatory { get; set; } = true;

    /// <summary>Nothing after this can start until it is done, so a delay here moves the whole chain.</summary>
    public bool IsBlocking { get; set; }

    public string? Note { get; set; }
}

public class DealMilestone : BaseEntity
{
    public Guid DealId { get; set; }
    public Deal? Deal { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateOnly? TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }

    /// <summary>Actual minus target. Negative is early, and worth knowing about too.</summary>
    public int? VarianceDays { get; set; }

    public int SortOrder { get; set; }
    public string? DelayReason { get; set; }
}

/// <summary>
/// A chain of dependent transactions, each waiting on the one below it. A market with chains
/// cannot be modelled without this, and a chain break has to be visible the day it happens.
/// </summary>
public class SalesChain : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public int LinkCount { get; set; }
    public bool IsComplete { get; set; }
    public bool IsBroken { get; set; }
    public DateOnly? BrokenOn { get; set; }
    public int? BrokenAtPosition { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }

    public ICollection<SalesChainLink> Links { get; set; } = [];
}

public class SalesChainLink : BaseEntity
{
    public Guid SalesChainId { get; set; }
    public SalesChain? Chain { get; set; }

    public int Position { get; set; }
    public Guid? DealId { get; set; }

    /// <summary>Set when the link is somebody else's transaction that we can only observe.</summary>
    public string? ExternalDescription { get; set; }
    public string? ExternalAgentName { get; set; }
    public string? ExternalAgentPhone { get; set; }

    public DealStatus Status { get; set; } = DealStatus.Progressing;
    public string? SolicitorName { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public bool IsHoldingUpChain { get; set; }
    public string? HoldUpReason { get; set; }
}

/// <summary>
/// A deal that died, and why. Fall-through rate by cause is the number every agency principal
/// wants and almost no system produces, because nobody records the cause at the moment it is known.
/// </summary>
public class FallThroughRecord : BaseEntity
{
    public Guid DealId { get; set; }
    public Guid PropertyId { get; set; }

    public FallThroughCause Cause { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Detail { get; set; }

    public DateOnly OccurredOn { get; set; }

    /// <summary>How far it got before dying. A deal lost at exchange costs far more than one lost at survey.</summary>
    public string? StageReached { get; set; }

    public int DaysInProgress { get; set; }

    /// <summary>Marketing, legal and survey spend that will not now be recovered.</summary>
    public decimal CostIncurred { get; set; }

    public decimal LostFee { get; set; }

    public bool RelistedImmediately { get; set; }
    public Guid? NewListingId { get; set; }

    /// <summary>Previous viewers told it is back on. The fastest re-sale an agency ever gets.</summary>
    public bool PreviousViewersNotified { get; set; }
}

/// <summary>
/// The legal transfer itself — deed, stamp duty, registration and mutation. Shared by brokerage
/// completions and developer sale deeds, because it is the same set of facts in both.
/// </summary>
public class Conveyancing : BaseEntity
{
    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PropertyId { get; set; }

    public Guid? BuyerSolicitorPartyId { get; set; }
    public Guid? SellerSolicitorPartyId { get; set; }

    public DateOnly? DraftDeedOn { get; set; }
    public DateOnly? DeedApprovedOn { get; set; }

    // ── Stamp duty & registration ────────────────────────────────────────────

    public decimal ConsiderationValue { get; set; }

    /// <summary>The authority's own valuation, which duty is charged on when it exceeds the price.</summary>
    public decimal? GovernmentValue { get; set; }

    public decimal StampDutyRate { get; set; }
    public decimal StampDutyAmount { get; set; }
    public decimal RegistrationFee { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal OtherLevies { get; set; }
    public decimal TotalTransactionCost { get; set; }

    public DateOnly? RegistrationAppointmentOn { get; set; }
    public string? TokenNumber { get; set; }
    public string? RegistrarOffice { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? DeedNumber { get; set; }
    public string? DeedUrl { get; set; }

    // ── Mutation ─────────────────────────────────────────────────────────────

    public DateOnly? MutationAppliedOn { get; set; }
    public string? MutationNumber { get; set; }
    public DateOnly? MutationCompletedOn { get; set; }
    public bool IsMutationComplete { get; set; }

    public string? Note { get; set; }
}
