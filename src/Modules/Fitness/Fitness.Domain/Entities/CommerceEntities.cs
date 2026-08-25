using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// An employer, insurer or scheme that pays for people to be members.
///
/// A real commercial relationship rather than a discount code: it has a contract, an eligibility
/// rule, a rate, an invoicing model and usage reporting the payer expects as part of the deal.
/// </summary>
public class CorporateAccount : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid ClubId { get; set; }

    /// <summary>CRM account, when the company is also a CRM record.</summary>
    public Guid? CrmAccountId { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AddressLine { get; set; }
    public string? TaxRegistrationNumber { get; set; }

    public CorporateBillingModel BillingModel { get; set; } = CorporateBillingModel.EmployeePaysDiscounted;

    /// <summary>The negotiated rate, or the discount off list.</summary>
    public decimal? NegotiatedRate { get; set; }
    public decimal DiscountPercent { get; set; }

    /// <summary>What the employer contributes per member, on a subsidised scheme.</summary>
    public decimal SubsidyPerMember { get; set; }
    public decimal SubsidyPercent { get; set; }

    public Guid? DefaultPlanId { get; set; }

    public DateTime ContractStartsOn { get; set; }
    public DateTime? ContractEndsOn { get; set; }

    /// <summary>Employees the deal covers. Zero is uncapped.</summary>
    public int MaxMembers { get; set; }
    public int CurrentMemberCount { get; set; }

    public int InvoiceDayOfMonth { get; set; } = 1;
    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>The employer receives anonymised usage reporting, which is usually why they bought it.</summary>
    public bool ReceivesUsageReport { get; set; } = true;

    public ICollection<CorporateEligibilityRule> EligibilityRules { get; set; } = [];
    public ICollection<CorporateMember> Members { get; set; } = [];
}

/// <summary>How the club decides whether someone really works there.</summary>
public class CorporateEligibilityRule : BaseEntity
{
    public Guid CorporateAccountId { get; set; }
    public CorporateAccount? CorporateAccount { get; set; }

    public EligibilityProof Proof { get; set; } = EligibilityProof.EmailDomain;

    /// <summary>The domain, the code, or the id pattern, depending on the proof kind.</summary>
    public string? MatchValue { get; set; }

    /// <summary>Staff must approve the join rather than it being automatic.</summary>
    public bool RequiresManualApproval { get; set; }

    /// <summary>How often eligibility is re-checked, so leavers do not keep the rate forever.</summary>
    public int RevalidateEveryDays { get; set; } = 365;
}

/// <summary>One employee on a corporate scheme, and their eligibility state.</summary>
public class CorporateMember : BaseEntity
{
    public Guid CorporateAccountId { get; set; }
    public CorporateAccount? CorporateAccount { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid? AgreementId { get; set; }

    public string? EmployeeReference { get; set; }
    public string? Department { get; set; }

    public DateTime JoinedSchemeOn { get; set; } = DateTime.UtcNow;
    public DateTime? LeftSchemeOn { get; set; }

    public DateTime? EligibilityVerifiedOn { get; set; }
    public DateTime? EligibilityExpiresOn { get; set; }
    public Guid? ProofDocumentId { get; set; }

    /// <summary>What the employer pays for this person this period.</summary>
    public decimal EmployerContribution { get; set; }
    public decimal EmployeeContribution { get; set; }

    public string? LeaveReason { get; set; }
}

/// <summary>
/// A consolidated invoice to an employer, with the per-employee breakdown they will ask for.
/// </summary>
public class CorporateInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid CorporateAccountId { get; set; }
    public CorporateAccount? CorporateAccount { get; set; }

    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime DueOn { get; set; }
    public DateTime? PaidOn { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public int MemberCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>The member-level lines behind the total, kept as its own document.</summary>
    public string? BreakdownUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? PurchaseOrderReference { get; set; }
}

/// <summary>
/// An insurer, council or referral scheme that pays for named individuals under an authorisation.
///
/// Different from a corporate account: the unit is an authorised course of N sessions for one
/// person, not a headcount of employees.
/// </summary>
public class ThirdPartyPayer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid ClubId { get; set; }

    /// <summary>Insurer, local authority, GP referral scheme, charity, employer of a rehab client.</summary>
    public string? PayerType { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public int PaymentTermsDays { get; set; } = 30;

    /// <summary>The rate the scheme pays, which is rarely the club's list price.</summary>
    public decimal? AgreedRate { get; set; }

    public bool RequiresAuthorisationNumber { get; set; } = true;
}

/// <summary>One authorised course of treatment or membership, and how much of it is left.</summary>
public class PayerAuthorisation : BaseEntity
{
    public Guid ThirdPartyPayerId { get; set; }
    public ThirdPartyPayer? ThirdPartyPayer { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public string AuthorisationNumber { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    /// <summary>Sessions or weeks approved. The club is not paid beyond this.</summary>
    public int ApprovedUnits { get; set; }
    public int UsedUnits { get; set; }

    public decimal RatePerUnit { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal InvoicedValue { get; set; }

    /// <summary>What was authorised — a referral programme, post-operative rehab, a subsidised membership.</summary>
    public string? Purpose { get; set; }

    public string? ReferrerName { get; set; }
    public Guid? DocumentId { get; set; }

    public bool IsExhausted { get; set; }
}

/// <summary>
/// A pro-shop or bar sale.
///
/// Products, stock and cost live in Inventory — this records the transaction and publishes the
/// depletion event, exactly as Point of Sale does. A second item master would be a second source
/// of truth about how many shakers are in the cupboard.
/// </summary>
public class FitnessSale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public DateTime SoldAt { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public Guid? PaymentId { get; set; }
    public Guid? InvoiceId { get; set; }

    /// <summary>Charged to the member's account rather than paid at the counter.</summary>
    public bool IsHouseAccountCharge { get; set; }

    public Guid? CashSessionId { get; set; }
    public Guid? SoldByStaffId { get; set; }

    /// <summary>Set when this sale reverses another.</summary>
    public Guid? ReturnsSaleId { get; set; }
    public bool IsReturn { get; set; }
    public string? ReturnReason { get; set; }

    /// <summary>Stock movement has been published to Inventory.</summary>
    public bool StockDepleted { get; set; }

    public string? DiscountReason { get; set; }
    public Guid? DiscountApprovedByUserId { get; set; }

    public ICollection<FitnessSaleLine> Lines { get; set; } = [];
}

/// <summary>One line on a sale.</summary>
public class FitnessSaleLine : BaseEntity
{
    public Guid SaleId { get; set; }
    public FitnessSale? Sale { get; set; }

    public Guid? InventoryItemId { get; set; }
    public Guid? PlanId { get; set; }

    /// <summary>Kept flat so a receipt reprints correctly after a product is renamed.</summary>
    public string ItemName { get; set; } = string.Empty;
    public string? Barcode { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Cost at the moment of sale, so margin is not recomputed from a price that has since moved.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Scoop, flavour, size — a thin modifier for the smoothie counter.</summary>
    public string? Modifiers { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// A purchase charged to the member's account instead of paid for at the counter.
///
/// The convenience that makes a pro shop work — the member takes the shaker after their workout
/// and it appears on next month's invoice — and the thing that needs a credit limit behind it.
/// </summary>
public class HouseAccountCharge : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? SaleId { get; set; }

    public DateTime ChargedOn { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string ChargeDescription { get; set; } = string.Empty;

    /// <summary>Set when the charge has been rolled onto an invoice.</summary>
    public Guid? SettledInvoiceId { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledOn { get; set; }

    public Guid? AuthorisedByStaffId { get; set; }
}

/// <summary>
/// Vending, towel-service and other machine revenue, entered as a periodic figure.
///
/// Small money, but leaving it out makes revenue-per-member wrong, and revenue per member is the
/// number owners benchmark themselves on.
/// </summary>
public class VendingRevenueEntry : BaseEntity
{
    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>Vending, towel hire, parking, laundry, physio room rent.</summary>
    public string RevenueSource { get; set; } = string.Empty;
    public string? MachineReference { get; set; }

    public decimal GrossRevenue { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal NetRevenue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int? TransactionCount { get; set; }
    public Guid? EnteredByStaffId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A till session. Opened with a float, closed with a count.
///
/// Matches how Point of Sale and Restaurant already do it, so a manager who has closed one drawer
/// in NexCore can close all three without being retrained.
/// </summary>
public class CashSession : BaseEntity
{
    public string SessionNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public Guid? OpenedByStaffId { get; set; }
    public Guid? ClosedByStaffId { get; set; }

    public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }

    public decimal OpeningFloat { get; set; }

    public decimal CashSales { get; set; }
    public decimal CardSales { get; set; }
    public decimal OtherSales { get; set; }
    public decimal Refunds { get; set; }
    public decimal PaidIn { get; set; }
    public decimal PaidOut { get; set; }
    public decimal Drops { get; set; }

    /// <summary>What the drawer should hold.</summary>
    public decimal ExpectedCash { get; set; }

    /// <summary>What it actually held.</summary>
    public decimal CountedCash { get; set; }
    public decimal Variance { get; set; }

    /// <summary>
    /// Counted without the expected figure on screen. It is the only way a variance means
    /// anything — a visible target is a target.
    /// </summary>
    public bool WasBlindCount { get; set; }

    public int TransactionCount { get; set; }
    public string? VarianceNote { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public ICollection<CashMovement> Movements { get; set; } = [];
}

/// <summary>One movement of cash in or out of a drawer.</summary>
public class CashMovement : BaseEntity
{
    public Guid CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }

    public CashMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Positive in, negative out.</summary>
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
    public string? Reference { get; set; }

    public Guid? StaffId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? PaymentId { get; set; }
}
