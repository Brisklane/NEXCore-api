using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// How somebody gets paid for selling. Covers both commission worlds this app serves: a
/// brokerage's graduated-and-capped agent plan, and a developer's slab on booking value or
/// collection.
///
/// Commission is the single most disputed number in this industry — research puts disputes at up
/// to a quarter of transactions in spreadsheet-run brokerages — so every input is on the record
/// and every calculation keeps its working.
/// </summary>
public class CommissionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public CommissionPlanKind Kind { get; set; } = CommissionPlanKind.FlatSplit;
    public CommissionTrigger Trigger { get; set; } = CommissionTrigger.OnCompletion;

    /// <summary>"Agent", "Team", "ChannelPartner", "Referrer".</summary>
    public string AppliesTo { get; set; } = "Agent";

    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    // ── Split ────────────────────────────────────────────────────────────────

    /// <summary>Agent's share of the gross fee before deductions.</summary>
    public decimal AgentSharePercent { get; set; }

    public decimal HouseSharePercent { get; set; }
    public decimal FixedFeePerDeal { get; set; }

    // ── Cap ──────────────────────────────────────────────────────────────────

    /// <summary>Once the house has taken this much in the year, the agent keeps everything.</summary>
    public decimal AnnualCapAmount { get; set; }

    /// <summary>Unused cap carries into the next year. A real term, and agents remember it.</summary>
    public bool CapRollsOver { get; set; }

    /// <summary>Charged per deal after the cap is met, in place of a split.</summary>
    public decimal PostCapFeePerDeal { get; set; }

    public decimal PostCapPercent { get; set; }

    // ── Standing deductions ──────────────────────────────────────────────────

    public decimal FranchiseRoyaltyPercent { get; set; }
    public decimal TransactionFee { get; set; }
    public decimal MonthlyDeskFee { get; set; }

    // ── Developer-side ───────────────────────────────────────────────────────

    /// <summary>Pay in step with what the customer has actually paid, rather than all at booking.</summary>
    public bool PayProRataWithCollection { get; set; }

    /// <summary>Collection percentage that must be reached before the first instalment of commission.</summary>
    public decimal MinimumCollectionPercent { get; set; }

    public decimal WithholdingPercent { get; set; }

    /// <summary>Reverse what was paid when the booking cancels. Otherwise cancelled files cost real money.</summary>
    public bool ClawBackOnCancellation { get; set; } = true;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<CommissionPlanTier> Tiers { get; set; } = [];
}

/// <summary>
/// One band of a graduated or slab plan. Matched on year-to-date volume for an agent, or on the
/// value of the individual booking for a dealer.
/// </summary>
public class CommissionPlanTier : BaseEntity
{
    public Guid CommissionPlanId { get; set; }
    public CommissionPlan? Plan { get; set; }

    public int TierNumber { get; set; }
    public string? Label { get; set; }

    public decimal FromAmount { get; set; }
    public decimal? ToAmount { get; set; }
    public int? FromCount { get; set; }
    public int? ToCount { get; set; }

    public decimal SharePercent { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal FixedAmount { get; set; }

    /// <summary>
    /// Retrospective tiers re-rate everything from the first sale once the band is reached;
    /// prospective ones only apply to what comes next. Both exist and the difference is money.
    /// </summary>
    public bool IsRetrospective { get; set; }
}

/// <summary>An agent, team or partner's live plan, with any negotiated override.</summary>
public class CommissionAgreement : BaseEntity
{
    public Guid CommissionPlanId { get; set; }
    public Guid? AgentProfileId { get; set; }
    public Guid? SalesTeamId { get; set; }
    public Guid? ChannelPartnerId { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>Overrides the plan for this person only. Approved, never silently different.</summary>
    public decimal? OverrideSharePercent { get; set; }
    public decimal? OverrideCapAmount { get; set; }
    public Guid? OverrideApprovalRequestId { get; set; }

    public Guid? MentorAgentId { get; set; }
    public decimal MentorOverridePercent { get; set; }
    public DateOnly? MentorOverrideUntil { get; set; }

    public string? DocumentUrl { get; set; }
}

/// <summary>
/// The commission arising from one transaction, with every deduction itemised. This is what
/// becomes the disbursement authorisation, and what an agent will read line by line.
/// </summary>
public class CommissionCalculation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? ProjectId { get; set; }

    public CommissionTrigger Trigger { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Accrued;

    public decimal TransactionValue { get; set; }
    public decimal GrossFee { get; set; }
    public decimal TaxOnFee { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetDistributable { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    public DateOnly CalculatedOn { get; set; }
    public DateOnly? EarnedOn { get; set; }
    public DateOnly? DueOn { get; set; }

    /// <summary>Collection percentage at the moment of calculation, for a pay-on-collection plan.</summary>
    public decimal CollectionPercentAtCalculation { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public Guid? DisbursementId { get; set; }
    public Guid? JournalEntryId { get; set; }

    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }
    public string? CalculationTrace { get; set; }

    public ICollection<CommissionSplit> Splits { get; set; } = [];
}

/// <summary>
/// One person's share of one transaction's fee. Several per calculation: the listing agent, the
/// selling agent, the team leader's override, the mentor, the referrer and the house.
/// </summary>
public class CommissionSplit : BaseEntity
{
    public Guid CommissionCalculationId { get; set; }
    public CommissionCalculation? Calculation { get; set; }

    public Guid? AgentProfileId { get; set; }
    public Guid? SalesTeamId { get; set; }
    public Guid? ChannelPartnerId { get; set; }
    public Guid? ReferrerPartyId { get; set; }

    /// <summary>"ListingAgent", "SellingAgent", "TeamLeader", "Mentor", "Referrer", "House", "Franchise".</summary>
    public string Role { get; set; } = "SellingAgent";

    public Guid? CommissionAgreementId { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DeductionTotal { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public CommissionStatus Status { get; set; } = CommissionStatus.Accrued;

    /// <summary>Which tier fired, so an agent can see why they were paid at 60% and not 70%.</summary>
    public int? TierApplied { get; set; }
    public bool CapReached { get; set; }
    public decimal CapContribution { get; set; }

    public ICollection<CommissionDeduction> Deductions { get; set; } = [];
}

public class CommissionDeduction : BaseEntity
{
    public Guid CommissionSplitId { get; set; }
    public CommissionSplit? Split { get; set; }

    public DeductionKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public Guid? PayableToPartyId { get; set; }
    public Guid? PayableToAgentId { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The document that says exactly who gets what from this transaction. Approved, then paid — a
/// payout without one is how a brokerage ends up in front of a regulator.
/// </summary>
public class CommissionDisbursement : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid CommissionCalculationId { get; set; }
    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }

    public DateOnly IssuedOn { get; set; }
    public decimal GrossFee { get; set; }
    public decimal TotalDisbursed { get; set; }
    public decimal HouseRetained { get; set; }

    public Guid? PreparedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Guid? GeneratedDocumentId { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>The fee sat in the client account until completion, and is released from it here.</summary>
    public bool FromClientAccount { get; set; }
    public Guid? ClientAccountId { get; set; }
}

/// <summary>A batch payment of commission, per period.</summary>
public class CommissionPayout : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? AgentProfileId { get; set; }
    public Guid? ChannelPartnerId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly PaidOn { get; set; }

    public decimal GrossAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal WithholdingAmount { get; set; }
    public decimal AdvanceRecovered { get; set; }
    public decimal ClawbackAmount { get; set; }
    public decimal NetAmount { get; set; }

    public PaymentInstrument Instrument { get; set; } = PaymentInstrument.BankTransfer;
    public string? PaymentReference { get; set; }
    public Guid? JournalEntryId { get; set; }

    /// <summary>Employed agents are paid through payroll rather than as a supplier.</summary>
    public bool PaidViaPayroll { get; set; }
    public Guid? PayrollRunId { get; set; }

    public Guid? StatementDocumentId { get; set; }
    public bool IsPaid { get; set; }

    public ICollection<CommissionPayoutLine> Lines { get; set; } = [];
}

public class CommissionPayoutLine : BaseEntity
{
    public Guid CommissionPayoutId { get; set; }
    public CommissionPayout? Payout { get; set; }

    public Guid? CommissionSplitId { get; set; }
    public Guid? CommissionCalculationId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>A negative line reversing commission on a booking that has since cancelled.</summary>
    public bool IsClawback { get; set; }
}

/// <summary>A fee paid to somebody who introduced the business, in or out.</summary>
public class ReferralFee : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? DealId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? CommissionCalculationId { get; set; }

    /// <summary>"Inbound" — somebody sent us business; "Outbound" — we sent business away.</summary>
    public string Direction { get; set; } = "Outbound";

    public Guid? ReferrerPartyId { get; set; }
    public string? ReferrerName { get; set; }
    public string? ReferrerOrganisation { get; set; }

    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public CommissionStatus Status { get; set; } = CommissionStatus.Accrued;
    public DateOnly? PaidOn { get; set; }
    public string? AgreementUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// An agent's running position against their cap, per year. Kept as its own row rather than summed
/// on demand, because the agent checks it constantly and it must agree with the payout every time.
/// </summary>
public class AgentCapLedger : BaseEntity
{
    public Guid AgentProfileId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal CapAmount { get; set; }

    /// <summary>What the house has taken so far this cap year.</summary>
    public decimal ContributedAmount { get; set; }

    public decimal RemainingToCap { get; set; }
    public bool CapReached { get; set; }
    public DateOnly? CapReachedOn { get; set; }

    /// <summary>Unused cap brought forward, where the plan allows it.</summary>
    public decimal RolledOverAmount { get; set; }

    public decimal GrossCommissionEarned { get; set; }
    public decimal NetCommissionEarned { get; set; }
    public int DealCount { get; set; }
    public decimal TransactionVolume { get; set; }
}
