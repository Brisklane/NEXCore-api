using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A bid on a property in the brokerage world. Kept as a full thread rather than a current number,
/// because several markets require every offer to be presented to the vendor and provably so.
/// </summary>
public class Offer : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid PropertyId { get; set; }
    public Guid? ListingId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid BuyerPartyId { get; set; }
    public Guid? SellerPartyId { get; set; }
    public Guid? AgentId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public OfferStatus Status { get; set; } = OfferStatus.Submitted;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateOnly? ProposedCompletionDate { get; set; }

    // ── Strength of the offer ────────────────────────────────────────────────
    // What makes a lower offer beat a higher one, so it belongs on the record and not in a note.

    public FundingKind Funding { get; set; } = FundingKind.Cash;
    public decimal? DepositAmount { get; set; }
    public bool ProofOfFundsProvided { get; set; }
    public string? ProofOfFundsUrl { get; set; }
    public bool MortgageInPrinciple { get; set; }
    public bool IsChainFree { get; set; }

    /// <summary>Position in the chain of dependent transactions, where the market has chains.</summary>
    public int? ChainLength { get; set; }

    public Guid? PreviousOfferId { get; set; }
    public int RoundNumber { get; set; } = 1;

    /// <summary>Part of a sealed-bid round. Not revealed until the deadline passes.</summary>
    public bool IsBestAndFinal { get; set; }

    public bool VendorNotified { get; set; }
    public DateTime? VendorNotifiedAt { get; set; }
    public Guid? RejectReasonCodeId { get; set; }
    public string? Note { get; set; }

    public Guid? ResultingDealId { get; set; }

    public ICollection<OfferCondition> Conditions { get; set; } = [];
    public ICollection<OfferCounter> Counters { get; set; } = [];
}

public class OfferCondition : BaseEntity
{
    public Guid OfferId { get; set; }
    public Offer? Offer { get; set; }

    public OfferConditionKind Kind { get; set; }
    public string? Detail { get; set; }
    public DateOnly? SatisfyByDate { get; set; }
    public bool IsSatisfied { get; set; }
    public DateOnly? SatisfiedOn { get; set; }
}

public class OfferCounter : BaseEntity
{
    public Guid OfferId { get; set; }
    public Offer? Offer { get; set; }

    public int RoundNumber { get; set; }

    /// <summary>"Buyer" or "Seller". Who moved this round.</summary>
    public string ProposedBy { get; set; } = "Seller";

    public decimal Amount { get; set; }
    public DateTime ProposedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public OfferStatus Outcome { get; set; } = OfferStatus.Submitted;
    public string? Note { get; set; }
}

/// <summary>
/// A pre-launch registration with a priority number. Refundable, and honoured at balloting — an
/// EOI holder who registered first expects to be drawn from an earlier pool, and will say so.
/// </summary>
public class ExpressionOfInterest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? ChannelPartnerId { get; set; }

    /// <summary>The size class registered for, which decides which ballot they join.</summary>
    public string? CategoryCode { get; set; }

    public decimal Amount { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Allocated in receipt order and never re-used, because it is the fairness guarantee.</summary>
    public int PriorityNumber { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public Guid? ReceiptId { get; set; }
    public Guid? ConvertedBookingId { get; set; }
    public Guid? BallotEntryId { get; set; }
    public DateTime? RefundedAt { get; set; }
    public bool IsRefundable { get; set; } = true;
}

/// <summary>
/// A token taken to hold a unit before the paperwork exists. Expires on its own and releases the
/// unit, which is the only thing that stops a board silting up with dead reservations.
/// </summary>
public class TokenReservation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? SalesExecutiveId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public Guid? ReceiptId { get; set; }
    public Guid? ConvertedBookingId { get; set; }

    /// <summary>Deducted from the price, forfeited on withdrawal, or refundable. All three exist.</summary>
    public bool IsAdjustableAgainstPrice { get; set; } = true;
    public bool IsForfeitableOnWithdrawal { get; set; } = true;

    public DateTime? ExpiredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelReasonCodeId { get; set; }
    public string? AgreementUrl { get; set; }

    /// <summary>Agreed price if it differs from the list — the negotiation this token locks in.</summary>
    public decimal? AgreedPrice { get; set; }
}

/// <summary>
/// The moment inventory becomes a customer's. Everything downstream — the plan, the demands, the
/// receipts, the transfer, the possession — hangs off this row.
/// </summary>
public class Booking : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ProjectId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PlotFileId { get; set; }
    public Guid? PropertyId { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Provisional;
    public DateOnly BookingDate { get; set; }

    // ── Who ──────────────────────────────────────────────────────────────────

    public Guid PrimaryApplicantPartyId { get; set; }
    public Guid? NomineePartyId { get; set; }

    // ── Where it came from ───────────────────────────────────────────────────

    public SourcingChannel SourcingChannel { get; set; } = SourcingChannel.Direct;
    public Guid? ChannelPartnerId { get; set; }
    public Guid? LeadRegistrationId { get; set; }
    public Guid? SalesExecutiveId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? SiteVisitId { get; set; }
    public Guid? TokenReservationId { get; set; }
    public Guid? ExpressionOfInterestId { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public Guid? PriceListId { get; set; }
    public decimal ListPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountPercent { get; set; }

    /// <summary>List less discount plus every premium inside the sale price.</summary>
    public decimal NetSalePrice { get; set; }

    /// <summary>Sale price plus charges billed separately — stamp duty, registration, corpus.</summary>
    public decimal TotalConsideration { get; set; }

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;
    public decimal AreaSqFt { get; set; }
    public decimal RatePerSqFt { get; set; }

    public decimal TotalDemanded { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalSurcharge { get; set; }
    public decimal TotalWaived { get; set; }
    public decimal Outstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public int DaysOverdue { get; set; }

    /// <summary>Paid over total consideration. Drives commission on collection and escrow release.</summary>
    public decimal CollectionPercent { get; set; }

    public Guid? PaymentPlanId { get; set; }
    public DateOnly? NextDueDate { get; set; }
    public decimal NextDueAmount { get; set; }

    // ── Documents & lifecycle ────────────────────────────────────────────────

    public Guid? AllotmentId { get; set; }
    public Guid? SaleAgreementId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? DiscountApprovalRequestId { get; set; }

    public DateTime? ConfirmedAt { get; set; }
    public DateOnly? AgreementSignedOn { get; set; }
    public DateOnly? PossessionOfferedOn { get; set; }
    public DateOnly? PossessionTakenOn { get; set; }
    public DateOnly? RegisteredOn { get; set; }

    public Guid? CancellationId { get; set; }
    public Guid? TransferRequestId { get; set; }

    /// <summary>Set when this booking replaced another through transfer or a unit change.</summary>
    public Guid? PreviousBookingId { get; set; }

    public Guid? DunningCaseId { get; set; }
    public bool IsUnderLitigation { get; set; }
    public Guid? CustomerMortgageId { get; set; }

    public string? Notes { get; set; }

    public ICollection<BookingApplicant> Applicants { get; set; } = [];
    public ICollection<BookingChargeLine> ChargeLines { get; set; } = [];
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = [];
}

/// <summary>
/// An applicant on the booking. Several, because joint applications are the norm and each one's
/// KYC has to be complete before completion is allowed.
/// </summary>
public class BookingApplicant : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }
    public Guid PartyId { get; set; }

    public int SequenceNumber { get; set; }
    public bool IsPrimary { get; set; }
    public decimal SharePercent { get; set; }

    /// <summary>"Applicant", "CoApplicant", "Guardian", "Attorney".</summary>
    public string Role { get; set; } = "Applicant";

    public KycStatus KycStatus { get; set; } = KycStatus.NotStarted;
    public DateOnly AddedOn { get; set; }
    public DateOnly? RemovedOn { get; set; }
    public Guid? AmendmentId { get; set; }
}

/// <summary>
/// One line of the cost sheet, frozen at booking. Copied from the unit's premiums rather than
/// referenced, so re-pricing the project never rewrites a signed cost sheet.
/// </summary>
public class BookingChargeLine : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public ChargeKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; } = ChargeBasis.Fixed;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }

    public bool IsPartOfSalePrice { get; set; } = true;
    public bool IsOptional { get; set; }
    public bool IsAccepted { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>A discount and the authority behind it. Never a number typed into a total.</summary>
public class BookingDiscount : BaseEntity
{
    public Guid BookingId { get; set; }

    /// <summary>"Negotiated", "Scheme", "EarlyPayment", "Loyalty", "Staff", "Bulk", "Launch".</summary>
    public string DiscountType { get; set; } = "Negotiated";

    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }

    public Guid RequestedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome ApprovalOutcome { get; set; } = ApprovalOutcome.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class BookingStatusHistory : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public BookingStatus FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A controlled change to a booking after it is written — adding a co-applicant, correcting a
/// name, changing the correspondence address, moving to a different unit. Fee-bearing at most
/// developers, and always approved.
/// </summary>
public class BookingAmendment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string Reference { get; set; } = string.Empty;

    /// <summary>"AddApplicant", "RemoveApplicant", "ChangeNominee", "NameCorrection",
    /// "AddressChange", "UnitChange", "PlanChange", "AreaRevision".</summary>
    public string AmendmentType { get; set; } = string.Empty;

    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string? Reason { get; set; }

    public decimal Fee { get; set; }
    public Guid? FeeReceiptId { get; set; }

    public Guid RequestedByUserId { get; set; }
    public DateOnly RequestedOn { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>Set on a unit change: the plan is rebuilt and the price difference settled.</summary>
    public Guid? NewUnitId { get; set; }
    public decimal? PriceDifference { get; set; }
    public Guid? NewPaymentPlanId { get; set; }
}

/// <summary>
/// The allotment letter — the developer's formal statement that this unit is this customer's.
/// Re-issuable, with the superseded copy kept, because a reissued letter with no trail is a
/// forgery risk.
/// </summary>
public class Allotment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string AllotmentNumber { get; set; } = string.Empty;

    public AllotmentStatus Status { get; set; } = AllotmentStatus.Issued;
    public DateOnly IssuedOn { get; set; }
    public Guid? IssuedByUserId { get; set; }

    public Guid? TemplateVersionId { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    public DateOnly? PossessionTargetDate { get; set; }
    public string? Terms { get; set; }

    public int Version { get; set; } = 1;
    public Guid? SupersedesAllotmentId { get; set; }
    public string? ReissueReason { get; set; }

    /// <summary>Handed to the customer, as opposed to generated and sitting in the file.</summary>
    public bool IsDelivered { get; set; }
    public DateOnly? DeliveredOn { get; set; }
    public string? DeliveryReference { get; set; }
}

/// <summary>
/// A public draw that assigns plots to file holders.
///
/// The seed, the input list and the output are all kept so the draw is reproducible and provable.
/// A ballot that cannot be re-run in front of a challenger is not a ballot, it is an assertion.
/// </summary>
public class Ballot : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public BallotStatus Status { get; set; } = BallotStatus.Draft;
    public DateOnly? ScheduledOn { get; set; }
    public DateTime? DrawnAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    /// <summary>The random seed. Kept so the exact same draw can be reproduced on demand.</summary>
    public string? RandomSeed { get; set; }

    /// <summary>Hash of the locked entry list, so nobody can claim the pool changed after the draw.</summary>
    public string? PoolHash { get; set; }

    public int EntryCount { get; set; }
    public int PrizeCount { get; set; }
    public int AllocatedCount { get; set; }

    public Guid? ConductedByUserId { get; set; }
    public string? WitnessNames { get; set; }
    public string? VideoUrl { get; set; }
    public string? ResultDocumentUrl { get; set; }

    /// <summary>A second draw for plots the first round's winners did not claim.</summary>
    public bool IsSupplementary { get; set; }
    public Guid? ParentBallotId { get; set; }

    public ICollection<BallotCategory> Categories { get; set; } = [];
    public ICollection<BallotEntry> Entries { get; set; } = [];
}

/// <summary>
/// A pool inside a ballot — a size class, or a reserved quota. Quotas for overseas buyers, staff
/// and serving officers are normal and are drawn separately.
/// </summary>
public class BallotCategory : BaseEntity
{
    public Guid BallotId { get; set; }
    public Ballot? Ballot { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal NominalAreaSqFt { get; set; }

    /// <summary>"General", "Overseas", "Staff", "Serving", "Martyrs", "Disabled".</summary>
    public string QuotaType { get; set; } = "General";

    public int ReservedCount { get; set; }
    public int EntryCount { get; set; }
    public int PrizeCount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>One file in the pool, and what it drew.</summary>
public class BallotEntry : BaseEntity
{
    public Guid BallotId { get; set; }
    public Ballot? Ballot { get; set; }
    public Guid? BallotCategoryId { get; set; }

    public Guid? PlotFileId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid PartyId { get; set; }
    public string? FileNumber { get; set; }

    public int SequenceInPool { get; set; }

    /// <summary>Priority carried from an EOI, where earlier registration earns an earlier pool.</summary>
    public int? PriorityNumber { get; set; }

    public bool IsEligible { get; set; } = true;
    public string? IneligibilityReason { get; set; }

    public Guid? AllottedUnitId { get; set; }
    public string? AllottedPlotNumber { get; set; }
    public int? DrawOrder { get; set; }

    /// <summary>Set when a human overrode the draw. Always recorded with who and why.</summary>
    public bool WasManuallyAssigned { get; set; }
    public Guid? OverrideByUserId { get; set; }
    public string? OverrideReason { get; set; }
}

public class BallotResult : BaseEntity
{
    public Guid BallotId { get; set; }
    public Guid BallotEntryId { get; set; }
    public Guid UnitId { get; set; }

    public int DrawOrder { get; set; }
    public DateTime DrawnAt { get; set; } = DateTime.UtcNow;
    public bool IsClaimed { get; set; }
    public DateOnly? ClaimedOn { get; set; }
    public DateOnly? ClaimDeadline { get; set; }
    public bool IsForfeited { get; set; }
}

/// <summary>
/// The sale contract. Generated from a versioned template so the exact wording a customer signed
/// can be reproduced years later, clause for clause.
/// </summary>
public class SaleAgreement : BaseEntity
{
    public Guid BookingId { get; set; }
    public string AgreementNumber { get; set; } = string.Empty;

    /// <summary>"AgreementToSell", "SaleDeed", "AllotmentAgreement", "BuilderBuyerAgreement".</summary>
    public string AgreementType { get; set; } = "AgreementToSell";

    public Guid? TemplateVersionId { get; set; }
    public Guid? GeneratedDocumentId { get; set; }
    public Guid? SignatureSessionId { get; set; }

    public DateOnly? ExecutedOn { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? RegistrarOffice { get; set; }

    public decimal StampDuty { get; set; }
    public decimal RegistrationFee { get; set; }

    public string LanguageCode { get; set; } = "en";
    public string? DocumentUrl { get; set; }
    public bool IsSigned { get; set; }
    public bool IsSuperseded { get; set; }
    public Guid? SupersededByAgreementId { get; set; }

    public ICollection<AgreementClause> Clauses { get; set; } = [];
}

/// <summary>
/// A clause as it actually appeared in one agreement. Snapshotted rather than referenced, because
/// the clause library moves on and the signed contract must not.
/// </summary>
public class AgreementClause : BaseEntity
{
    public Guid SaleAgreementId { get; set; }
    public SaleAgreement? Agreement { get; set; }

    public Guid? ClauseLibraryItemId { get; set; }
    public string ClauseKey { get; set; } = string.Empty;
    public string? Heading { get; set; }
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Negotiated for this customer only. Flagged so legal can find every one of them.</summary>
    public bool IsNonStandard { get; set; }
    public Guid? ApprovalRequestId { get; set; }
}
