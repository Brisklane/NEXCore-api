using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A period's primary sale to a partner, denormalised for comparison.
///
/// The invoices already exist. This is the aggregated half of a pair, kept next to the secondary
/// fact so sell-in vs sell-out is a join on two rows rather than a report that re-aggregates the
/// whole ledger every time someone opens a dashboard.
/// </summary>
public class PrimarySaleFact : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal Value { get; set; }
    public decimal FreeQuantity { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal ReturnValue { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public DateTime ComputedAt { get; set; }
}

/// <summary>
/// A secondary sale — distributor to retail outlet.
///
/// This is the layer that makes the module a DMS rather than a sales system. Almost every number
/// that matters to a consumer-goods business lives here: real demand, stock cover, scheme
/// effectiveness, market returns. A company that only sees its primary sales is watching how much
/// it pushed into the channel and calling it growth.
/// </summary>
public class SecondarySale : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid? OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    /// <summary>Kept as text too, because uploaded files name outlets we have not mapped yet.</summary>
    public string? OutletNameRaw { get; set; }

    public SecondaryCaptureMode CaptureMode { get; set; } = SecondaryCaptureMode.Declared;

    /// <summary>The upload this row arrived on, for uploaded data.</summary>
    public Guid? UploadBatchId { get; set; }

    /// <summary>The transaction that produced it, when the partner runs Distribution themselves.</summary>
    public Guid? SourceOrderId { get; set; }
    public Guid? SourceInvoiceId { get; set; }

    public DateTime SaleDate { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FieldRepId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReturnAmount { get; set; }

    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }

    /// <summary>False when the outlet or an item could not be resolved; sits in the exception queue.</summary>
    public bool IsMapped { get; set; }
    public string? MappingNote { get; set; }

    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }

    public ICollection<SecondarySaleLine> Lines { get; set; } = [];
}

/// <summary>One SKU on a secondary sale, with the partner's own code kept alongside ours.</summary>
public class SecondarySaleLine : BaseEntity
{
    public Guid SecondarySaleId { get; set; }
    public SecondarySale? SecondarySale { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }

    /// <summary>The partner's SKU code as submitted, before mapping. Kept so a failure is diagnosable.</summary>
    public string? ItemCodeRaw { get; set; }

    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal FreeQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public bool IsMapped { get; set; }
    public string? MappingNote { get; set; }
}

/// <summary>
/// One submission of secondary data from a partner.
///
/// The batch is the unit of accountability. "Which of my distributors filed on time, and how much
/// of what they filed actually mapped" is a data-quality question with commercial consequences,
/// and it needs a record per submission to answer.
/// </summary>
public class SecondaryUploadBatch : BaseEntity
{
    public string BatchNumber { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DueOn { get; set; }

    /// <summary>Days late. Feeds the partner's data-quality score.</summary>
    public int LatenessDays { get; set; }

    public UploadBatchStatus Status { get; set; } = UploadBatchStatus.Received;

    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public Guid? MappingProfileId { get; set; }

    public int TotalRows { get; set; }
    public int MappedRows { get; set; }
    public int UnmappedRows { get; set; }
    public int RejectedRows { get; set; }

    public decimal TotalValue { get; set; }
    public decimal MappedValue { get; set; }

    /// <summary>Mapped rows as a percentage. Below a threshold the batch is worth rejecting.</summary>
    public decimal MappingAccuracyPercent { get; set; }

    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string? ValidationSummary { get; set; }
}

/// <summary>
/// How one partner's codes translate to ours.
///
/// Every distributor names things their own way and none of them will change. The mapping profile
/// is what turns that from a monthly argument into a configuration, and the per-row entries are
/// built up from the exception queue as unmapped values are resolved once.
/// </summary>
public class SecondaryMappingProfile : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    /// <summary>"Csv", "Excel", "Json" — the shape of what they send.</summary>
    public string FileFormat { get; set; } = "Csv";

    /// <summary>Column header for each field we need, as the partner names it.</summary>
    public string? OutletColumn { get; set; }
    public string? ItemColumn { get; set; }
    public string? QuantityColumn { get; set; }
    public string? ValueColumn { get; set; }
    public string? DateColumn { get; set; }
    public string? UomColumn { get; set; }
    public string? BatchColumn { get; set; }
    public string? InvoiceColumn { get; set; }

    public string? DateFormat { get; set; }
    public int HeaderRowIndex { get; set; }

    /// <summary>JSON dictionary of their code to our id, grown from resolved exceptions.</summary>
    public string? ItemCodeMap { get; set; }
    public string? OutletCodeMap { get; set; }
    public string? UomMap { get; set; }

    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// What a partner says is sitting in their godown at period end.
///
/// The declaration plus primary minus secondary is the identity that either balances or does not,
/// and the ageing breakdown is what turns "we have three weeks of cover" into "we have three weeks
/// of cover and a fifth of it expires next month".
/// </summary>
public class DistributorStockDeclaration : BaseEntity
{
    public string DeclarationNumber { get; set; } = string.Empty;

    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public DateTime AsOfDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DueOn { get; set; }
    public int LatenessDays { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalValue { get; set; }
    public decimal NearExpiryValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public decimal DamagedValue { get; set; }

    public int LineCount { get; set; }

    /// <summary>Total stock ÷ average daily secondary sales. The number a supply planner lives on.</summary>
    public decimal DaysOfCover { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? Note { get; set; }

    public ICollection<DistributorStockLine> Lines { get; set; } = [];
}

/// <summary>One SKU on a stock declaration, with the ageing split that makes it useful.</summary>
public class DistributorStockLine : BaseEntity
{
    public Guid DeclarationId { get; set; }
    public DistributorStockDeclaration? Declaration { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemCodeRaw { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }

    // ── Ageing, in days since the partner received it ────────────────────────
    public decimal Age0To30 { get; set; }
    public decimal Age31To60 { get; set; }
    public decimal Age61To90 { get; set; }
    public decimal Age90Plus { get; set; }

    public decimal NearExpiryQuantity { get; set; }
    public decimal ExpiredQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }

    /// <summary>Days of cover for this SKU at this partner, against its own offtake.</summary>
    public decimal DaysOfCover { get; set; }

    public bool IsMapped { get; set; }
}

/// <summary>
/// The target stock a partner should hold for a SKU — the norm.
///
/// Under-stock costs a sale, over-stock becomes an expiry claim. The norm is what turns both into
/// an exception someone can act on before either happens.
/// </summary>
public class StockNorm : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;

    public string Uom { get; set; } = "PCS";

    /// <summary>Target days of cover. The primary expression of a norm.</summary>
    public decimal TargetDaysOfCover { get; set; }

    public decimal MinQuantity { get; set; }
    public decimal MaxQuantity { get; set; }
    public decimal ReorderQuantity { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    // ── Live position, refreshed against the latest declaration ──────────────
    public decimal CurrentQuantity { get; set; }
    public decimal CurrentDaysOfCover { get; set; }
    public bool IsUnderStocked { get; set; }
    public bool IsOverStocked { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}

/// <summary>
/// The sell-in / sell-out identity for one partner, one SKU, one period.
///
/// Opening + primary − secondary − returns = closing. When it does not balance, the difference is
/// surfaced as a named exception rather than buried in a report, because an unexplained gap in the
/// channel is either unrecorded sales, unrecorded stock, or leakage — and all three are urgent.
/// </summary>
public class SellInSellOutReconciliation : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }

    public string Uom { get; set; } = "PCS";

    public decimal OpeningQuantity { get; set; }
    public decimal PrimaryQuantity { get; set; }
    public decimal SecondaryQuantity { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal DeclaredClosingQuantity { get; set; }

    /// <summary>Opening + primary − secondary − returns.</summary>
    public decimal ComputedClosingQuantity { get; set; }

    public decimal VarianceQuantity { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal VarianceValue { get; set; }

    public ReconciliationOutcome Outcome { get; set; } = ReconciliationOutcome.Balanced;

    public bool IsExplained { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ExplanationNote { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public DateTime ComputedAt { get; set; }
}
