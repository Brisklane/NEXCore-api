using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// One periodic raising of rent across the book. Batched like the demand run, with a preview and
/// an exception list, because a rent run that silently skips six tenancies is found at month end.
/// </summary>
public class RentRun : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? OfficeId { get; set; }

    public DateOnly RunDate { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public bool IsDryRun { get; set; }
    public int CandidateCount { get; set; }
    public int ChargedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public Guid? RunByUserId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? ErrorSummary { get; set; }
    public bool IsCompleted { get; set; }

    public ICollection<RentRunLine> Lines { get; set; } = [];
}

public class RentRunLine : BaseEntity
{
    public Guid RentRunId { get; set; }
    public RentRun? Run { get; set; }

    public Guid TenancyId { get; set; }
    public Guid? RentChargeId { get; set; }
    public decimal Amount { get; set; }

    public bool WasSkipped { get; set; }

    /// <summary>"RentFree", "OnHold", "Disputed", "Terminated", "AlreadyCharged", "NoSchedule".</summary>
    public string? SkipReason { get; set; }

    public bool Failed { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>A tenant behind on rent, and the chase. Distinct from a developer's dunning case only
/// in what it is chasing, so it reuses the same ladder.</summary>
public class ArrearsCase : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid TenancyId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? DunningCaseId { get; set; }

    public DateOnly OpenedOn { get; set; }
    public decimal ArrearsAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public int DaysInArrears { get; set; }

    /// <summary>Arrears expressed in months of rent — how a landlord actually judges severity.</summary>
    public decimal MonthsInArrears { get; set; }

    public Guid? AssignedToUserId { get; set; }
    public Guid? ActivePromiseId { get; set; }
    public Guid? PaymentPlanNote { get; set; }
    public bool LandlordNotified { get; set; }
    public Guid? NoticeId { get; set; }
    public bool ReferredToLegal { get; set; }

    public bool IsClosed { get; set; }
    public DateOnly? ClosedOn { get; set; }
    public string? Outcome { get; set; }
}

/// <summary>
/// The year's estimated running costs for a building, by head. Billed on account through the year
/// and trued up against actuals at the end — the part of estate management that mid-market tools
/// simply cannot do.
/// </summary>
public class ServiceChargeBudget : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }

    public int FinancialYear { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal ManagementFeePercent { get; set; }

    /// <summary>Total lettable area, the denominator for pro-rata apportionment.</summary>
    public decimal TotalGrossLettableAreaSqFt { get; set; }

    /// <summary>Occupied area. The difference from GLA is what gross-up corrects for.</summary>
    public decimal OccupiedAreaSqFt { get; set; }

    // ── Lease-negotiated protections ─────────────────────────────────────────

    /// <summary>The year against which caps and increases are measured.</summary>
    public int? BaseYear { get; set; }

    /// <summary>Variable costs are grossed up to a notional occupancy so a half-empty building does not
    /// under-recover from the tenants who are there.</summary>
    public bool GrossUpEnabled { get; set; }
    public decimal GrossUpToOccupancyPercent { get; set; } = 95m;

    public decimal? AnnualCapPercent { get; set; }
    public decimal? CumulativeCapPercent { get; set; }

    public bool IsApproved { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public DateOnly? ApprovedOn { get; set; }
    public bool IsReconciled { get; set; }
    public string? DocumentUrl { get; set; }

    public ICollection<ServiceChargeBudgetLine> Lines { get; set; } = [];
}

public class ServiceChargeBudgetLine : BaseEntity
{
    public Guid ServiceChargeBudgetId { get; set; }
    public ServiceChargeBudget? Budget { get; set; }

    public ServiceChargeHead Head { get; set; }
    public string Label { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal VarianceAmount { get; set; }

    public ApportionmentBasis Basis { get; set; } = ApportionmentBasis.ProRataByArea;

    /// <summary>Varies with occupancy, so it is the part that gets grossed up.</summary>
    public bool IsVariableCost { get; set; }

    /// <summary>Capital items and the landlord's own costs are excluded from recovery by most leases.</summary>
    public bool IsExcludedFromRecovery { get; set; }

    public string? ExclusionReason { get; set; }
    public bool IsCapped { get; set; }
    public decimal? CapAmount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>How a building's costs are divided between its units.</summary>
public class ApportionmentSchedule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ServiceChargeBudgetId { get; set; }

    public ApportionmentBasis Basis { get; set; } = ApportionmentBasis.ProRataByArea;

    /// <summary>Set when this schedule applies to one cost head only — lift costs to upper floors, say.</summary>
    public ServiceChargeHead? AppliesToHead { get; set; }

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<ApportionmentLine> Lines { get; set; } = [];
}

public class ApportionmentLine : BaseEntity
{
    public Guid ApportionmentScheduleId { get; set; }
    public ApportionmentSchedule? Schedule { get; set; }

    public Guid? UnitId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? TenancyId { get; set; }

    public decimal AreaSqFt { get; set; }

    /// <summary>Share of the total. Lines in one schedule sum to 100.</summary>
    public decimal SharePercent { get; set; }

    public decimal FixedAmount { get; set; }
    public bool IsExempt { get; set; }
    public string? ExemptionReason { get; set; }
}

/// <summary>An on-account or balancing service-charge bill to one tenant.</summary>
public class ServiceChargeInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid? TenancyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PartyId { get; set; }
    public Guid ServiceChargeBudgetId { get; set; }

    /// <summary>"OnAccount", "Balancing", "AdHoc".</summary>
    public string InvoiceType { get; set; } = "OnAccount";

    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }

    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }

    public decimal SharePercent { get; set; }
    public decimal AreaSqFt { get; set; }

    public Guid? DocumentId { get; set; }
    public InstalmentStatus Status { get; set; } = InstalmentStatus.NotDue;
    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }
}

/// <summary>
/// The year-end true-up. Keeps the working — budget, actual, gross-up, cap, exclusions — because
/// a balancing charge a tenant cannot interrogate is a balancing charge that will not be paid.
/// </summary>
public class ServiceChargeReconciliation : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid ServiceChargeBudgetId { get; set; }
    public DateOnly ReconciledOn { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal TotalActual { get; set; }
    public decimal TotalGrossedUp { get; set; }
    public decimal TotalExcluded { get; set; }
    public decimal TotalCapAdjustment { get; set; }
    public decimal TotalRecoverable { get; set; }
    public decimal TotalBilledOnAccount { get; set; }
    public decimal NetDifference { get; set; }

    public ReconciliationOutcome Outcome { get; set; } = ReconciliationOutcome.Nil;

    public bool IsAudited { get; set; }
    public string? AuditorName { get; set; }
    public DateOnly? AuditedOn { get; set; }
    public string? StatementUrl { get; set; }
    public bool IsIssuedToTenants { get; set; }
    public bool IsFinalised { get; set; }

    public ICollection<ReconciliationLine> Lines { get; set; } = [];
}

public class ReconciliationLine : BaseEntity
{
    public Guid ServiceChargeReconciliationId { get; set; }
    public ServiceChargeReconciliation? Reconciliation { get; set; }

    public Guid? TenancyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PartyId { get; set; }

    public decimal SharePercent { get; set; }
    public decimal RecoverableShare { get; set; }
    public decimal BilledOnAccount { get; set; }

    /// <summary>Positive is owed by the tenant, negative is credited to them.</summary>
    public decimal Difference { get; set; }

    public ReconciliationOutcome Outcome { get; set; } = ReconciliationOutcome.Nil;
    public Guid? BalancingInvoiceId { get; set; }
    public Guid? CreditNoteId { get; set; }

    /// <summary>The cap bit and this tenant was protected from part of the increase.</summary>
    public decimal CapAdjustment { get; set; }

    public bool IsDisputed { get; set; }
}

/// <summary>
/// The long-term fund for major works. Kept apart from running costs because spending it on
/// day-to-day cleaning is the classic failure of building management, and the ledger should
/// prevent it rather than reveal it later.
/// </summary>
public class SinkingFund : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? PropertyId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? SocietyId { get; set; }

    public decimal TargetAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal AnnualContribution { get; set; }
    public Guid? BankAccountId { get; set; }

    /// <summary>Spending from the fund needs the committee's or landlord's approval, always.</summary>
    public bool WithdrawalRequiresApproval { get; set; } = true;

    public string? PlannedWorks { get; set; }
}

public class SinkingFundEntry : BaseEntity
{
    public Guid SinkingFundId { get; set; }
    public SinkingFund? Fund { get; set; }

    public DateOnly EntryDate { get; set; }

    /// <summary>"Contribution", "Interest", "Withdrawal", "Transfer", "Adjustment".</summary>
    public string EntryType { get; set; } = "Contribution";

    public decimal CreditAmount { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal RunningBalance { get; set; }

    public Guid? UnitId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid? JournalEntryId { get; set; }
}

/// <summary>
/// Percentage rent on a retail unit: a base rent, a breakpoint, and a share of everything above
/// it. How a mart actually charges its shops, and the reason a shopping centre needs more than a
/// rent roll.
/// </summary>
public class TurnoverRentTerm : BaseEntity
{
    public Guid TenancyId { get; set; }

    public TurnoverRentBasis Basis { get; set; } = TurnoverRentBasis.NaturalBreakpoint;

    /// <summary>Sales above which the percentage bites. Derived from base rent for a natural breakpoint.</summary>
    public decimal BreakpointAmount { get; set; }

    public decimal Percent { get; set; }

    /// <summary>"Monthly", "Quarterly", "Annual". Annual is fairer to seasonal traders and is common.</summary>
    public string CalculationPeriod { get; set; } = "Annual";

    public DateOnly PeriodStartMonth { get; set; }

    /// <summary>Categories excluded from the sales figure — internet orders, gift cards, staff sales.</summary>
    public string? ExcludedSalesCategories { get; set; }

    public bool RequiresAuditedFigures { get; set; } = true;
    public int DeclarationDueDays { get; set; } = 15;

    /// <summary>Deduct the base rent already paid before charging overage. The usual construction.</summary>
    public bool OffsetBaseRent { get; set; } = true;

    public ICollection<TurnoverRentSlab> Slabs { get; set; } = [];
}

public class TurnoverRentSlab : BaseEntity
{
    public Guid TurnoverRentTermId { get; set; }
    public TurnoverRentTerm? Term { get; set; }

    public decimal FromSales { get; set; }
    public decimal? ToSales { get; set; }
    public decimal Percent { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Sales a retail tenant declares, and the verification of them.</summary>
public class TenantSalesDeclaration : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Guid? TurnoverRentTermId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueOn { get; set; }
    public DateOnly? DeclaredOn { get; set; }

    public decimal GrossSales { get; set; }
    public decimal ExcludedSales { get; set; }
    public decimal NetSales { get; set; }
    public int? TransactionCount { get; set; }
    public decimal? FootfallCount { get; set; }

    /// <summary>"Portal", "Upload", "Manual", "PosIntegration".</summary>
    public string DeclarationSource { get; set; } = "Portal";

    public bool IsAudited { get; set; }
    public decimal? AuditedSales { get; set; }
    public DateOnly? AuditedOn { get; set; }

    /// <summary>Audited less declared. A persistent gap is a lease-enforcement conversation.</summary>
    public decimal? Variance { get; set; }

    public bool IsLate { get; set; }
    public decimal? LatePenalty { get; set; }
    public bool IsEstimated { get; set; }
    public string? SupportingDocumentUrl { get; set; }
}

/// <summary>The bill for rent above the breakpoint.</summary>
public class OverageInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid TenancyId { get; set; }
    public Guid PartyId { get; set; }
    public Guid? TurnoverRentTermId { get; set; }

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal DeclaredSales { get; set; }
    public decimal BreakpointApplied { get; set; }
    public decimal SalesAboveBreakpoint { get; set; }
    public decimal PercentApplied { get; set; }
    public decimal GrossOverage { get; set; }
    public decimal BaseRentOffset { get; set; }
    public decimal NetOverage { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public InstalmentStatus Status { get; set; } = InstalmentStatus.Due;
    public Guid? DocumentId { get; set; }

    /// <summary>Provisional on declared sales, to be re-issued when audited figures arrive.</summary>
    public bool IsProvisional { get; set; }

    public Guid? SupersededByInvoiceId { get; set; }
}

/// <summary>
/// Anything else billed on to a tenant — property tax, insurance, marketing fund, signage, parking,
/// after-hours air conditioning, fit-out supervision. One table because they behave identically.
/// </summary>
public class RecoveryCharge : BaseEntity
{
    public Guid TenancyId { get; set; }
    public Guid? PropertyId { get; set; }

    /// <summary>"PropertyTax", "Insurance", "MarketingFund", "Signage", "Parking", "Storage",
    /// "AfterHoursHvac", "FitOutSupervision", "Utilities".</summary>
    public string ChargeType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; } = ChargeBasis.Fixed;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public RentFrequency Frequency { get; set; } = RentFrequency.Monthly;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }

    /// <summary>Raised alongside rent by the rent run rather than invoiced separately.</summary>
    public bool IncludeInRentRun { get; set; } = true;

}

/// <summary>What kind of trader occupies a unit, so a centre can manage its mix rather than just let space.</summary>
public class TenantCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }

    /// <summary>What proportion of the centre this category should occupy.</summary>
    public decimal TargetMixPercent { get; set; }

    public decimal CurrentMixPercent { get; set; }
    public string? ColourHex { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>An empty unit, and what the emptiness is costing.</summary>
public class VoidRecord : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid? UnitId { get; set; }

    public DateOnly VacantFrom { get; set; }
    public DateOnly? LetFrom { get; set; }
    public int DaysVoid { get; set; }

    public Guid? PreviousTenancyId { get; set; }
    public Guid? NewTenancyId { get; set; }
    public decimal AskingRent { get; set; }

    /// <summary>Rent not earned while it sat empty. The number that justifies a rent reduction.</summary>
    public decimal LostRent { get; set; }

    /// <summary>Rates, service charge and security the owner pays on an empty unit.</summary>
    public decimal HoldingCost { get; set; }

    public int EnquiryCount { get; set; }
    public int ViewingCount { get; set; }
    public Guid? VoidReasonCodeId { get; set; }
    public bool RefurbishmentRequired { get; set; }
    public Guid? RefurbishmentWorkOrderId { get; set; }
    public bool IsClosed { get; set; }
}

/// <summary>
/// A rent-free period, fit-out period or capital contribution given to win a letting. Amortised
/// across the term rather than dropped into one month, so the effective rent is honest.
/// </summary>
public class LeaseConcession : BaseEntity
{
    public Guid TenancyId { get; set; }

    /// <summary>"RentFree", "FitOutPeriod", "CapitalContribution", "SteppedStart", "HalfRent".</summary>
    public string ConcessionType { get; set; } = "RentFree";

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal Amount { get; set; }
    public decimal? DiscountPercent { get; set; }

    /// <summary>Spread across the term for reporting, so effective rent is not overstated.</summary>
    public bool IsAmortised { get; set; } = true;
    public int AmortisationMonths { get; set; }
    public decimal MonthlyAmortisation { get; set; }

    /// <summary>Repayable if the tenant breaks early. A standard clawback that is routinely forgotten.</summary>
    public bool ClawbackOnEarlyBreak { get; set; }

    public Guid? ApprovalRequestId { get; set; }
    public string? Note { get; set; }
}
