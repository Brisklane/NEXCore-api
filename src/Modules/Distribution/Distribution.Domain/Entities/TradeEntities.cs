using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A channel price list: a set of prices that applies to a scope.
///
/// Distinct from the Sales price list because the scope axis is different. Sales prices by
/// customer group; distribution prices by channel × territory × partner tier, and the same SKU
/// legitimately has four live prices at once. The <see cref="PriceScope"/> is what resolution
/// walks, most-specific-first.
/// </summary>
public class ChannelPriceList : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public PriceScope Scope { get; set; } = PriceScope.Company;

    // ── Scope keys. Exactly the ones the scope needs are set. ────────────────
    public OutletChannel? Channel { get; set; }
    public Guid? TerritoryId { get; set; }
    public PartnerType? PartnerTier { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Higher wins when two lists of the same scope both match.</summary>
    public int Priority { get; set; }

    /// <summary>Prices include tax. Affects how the line is decomposed, not what is charged.</summary>
    public bool IsTaxInclusive { get; set; }

    /// <summary>Approved and live. A draft list can be edited; a live one is versioned instead.</summary>
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>The list this one supersedes, so a price history is a chain rather than an edit log.</summary>
    public Guid? SupersedesPriceListId { get; set; }

    public ICollection<ChannelPriceListLine> Lines { get; set; } = [];
}

/// <summary>
/// One SKU's price on a list, per unit of measure.
///
/// The UoM is part of the key on purpose. A case price is not the piece price times twenty-four;
/// dividing it is how distributors quietly lose their case margin.
/// </summary>
public class ChannelPriceListLine : BaseEntity
{
    public Guid PriceListId { get; set; }
    public ChannelPriceList? PriceList { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;

    public decimal UnitPrice { get; set; }
    public decimal Mrp { get; set; }

    /// <summary>Floor below which a discount needs approval, whatever the rep's authority.</summary>
    public decimal MinimumPrice { get; set; }

    public decimal MaxDiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public ICollection<PriceSlab> Slabs { get; set; } = [];
}

/// <summary>A quantity break on a price line — buy more of this SKU, pay less per unit.</summary>
public class PriceSlab : BaseEntity
{
    public Guid PriceListLineId { get; set; }
    public ChannelPriceListLine? PriceListLine { get; set; }

    public decimal FromQuantity { get; set; }

    /// <summary>Null for the open-ended top slab.</summary>
    public decimal? ToQuantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// The margin at each tier: our cost, the price to the distributor, their price to the retailer,
/// and the shelf MRP.
///
/// Held as one record per SKU per tier chain because "what does my distributor actually make on
/// this?" is a question asked in every appointment negotiation, and reconstructing it from four
/// price lists at the meeting is not viable.
/// </summary>
public class MarginLadder : BaseEntity
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public Guid? TerritoryId { get; set; }
    public Guid? PartnerId { get; set; }
    public OutletChannel? Channel { get; set; }

    public string Uom { get; set; } = "PCS";
    public string CurrencyCode { get; set; } = "USD";

    public decimal LandedCost { get; set; }
    public decimal PriceToDistributor { get; set; }
    public decimal PriceToWholesaler { get; set; }
    public decimal PriceToRetailer { get; set; }
    public decimal Mrp { get; set; }

    public decimal CompanyMarginPercent { get; set; }
    public decimal DistributorMarginPercent { get; set; }
    public decimal WholesalerMarginPercent { get; set; }
    public decimal RetailerMarginPercent { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A change of MRP, held per batch.
///
/// Two MRPs of the same SKU coexist in the trade for months after a revision, because old stock
/// is still on shelves. Pricing at the counter has to know which one the retailer is holding,
/// and a price-protection claim is computed from exactly this record.
/// </summary>
public class MrpRevision : BaseEntity
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal OldMrp { get; set; }
    public decimal NewMrp { get; set; }
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Set when the cut triggers compensation on stock already in the channel.</summary>
    public bool TriggersPriceProtection { get; set; }
    public decimal ProtectionPerUnit { get; set; }
    public DateTime? ProtectionClaimWindowEnds { get; set; }

    public string? Reason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

/// <summary>
/// A trade scheme — the commercial instrument distribution actually runs on.
///
/// This is not a discount. A scheme has a budget that depletes, a scope that decides who earns it,
/// slabs that decide how much, a stacking rule that decides what happens when two of them fire at
/// once, and a settlement mode that decides whether the benefit lands on the invoice or becomes a
/// claim next month. Modelling it as a discount percentage is why most ERPs cannot run a
/// distribution business.
/// </summary>
public class TradeScheme : BaseEntity
{
    public string SchemeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public TradeSchemeKind Kind { get; set; } = TradeSchemeKind.QuantityFreeGoods;
    public SchemeStatus Status { get; set; } = SchemeStatus.Draft;
    public SchemeSettlementMode SettlementMode { get; set; } = SchemeSettlementMode.OnInvoice;
    public SchemeStacking Stacking { get; set; } = SchemeStacking.Combinable;

    /// <summary>Schemes in the same group compete when stacking is <see cref="SchemeStacking.BestOfGroup"/>.</summary>
    public string? StackingGroup { get; set; }

    /// <summary>Higher evaluates first. Ties break on scheme number so evaluation is deterministic.</summary>
    public int Priority { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    /// <summary>Comma-separated weekday numbers for schemes that only run some days.</summary>
    public string? ActiveDays { get; set; }

    // ── Qualification ────────────────────────────────────────────────────────
    /// <summary>Minimum qualifying quantity in base units, for the simple non-slab kinds.</summary>
    public decimal MinQuantity { get; set; }
    public decimal MinValue { get; set; }

    /// <summary>Free units given per qualifying block, for buy-N-get-M.</summary>
    public decimal FreeQuantity { get; set; }

    /// <summary>The SKU given free, when it is not the one bought.</summary>
    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public string? FreeItemUom { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }

    /// <summary>Ceiling on the benefit one order can earn from this scheme.</summary>
    public decimal MaxBenefitPerOrder { get; set; }

    /// <summary>Ceiling per outlet across the whole scheme period.</summary>
    public decimal MaxBenefitPerOutlet { get; set; }

    /// <summary>Repeats the benefit for every whole qualifying block, rather than paying once.</summary>
    public bool IsRecurringPerBlock { get; set; } = true;

    // ── Period schemes ───────────────────────────────────────────────────────
    /// <summary>True for QPS and other schemes evaluated in arrears across the whole period.</summary>
    public bool IsPeriodScheme { get; set; }

    // ── Cash discount ────────────────────────────────────────────────────────
    public int PaymentWithinDays { get; set; }

    // ── Display scheme ───────────────────────────────────────────────────────
    public int DisplayDurationDays { get; set; }
    public decimal DisplayPayout { get; set; }
    public bool RequiresPhotoEvidence { get; set; }

    // ── Budget ───────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal BudgetAmount { get; set; }
    public decimal ConsumedAmount { get; set; }

    /// <summary>Stops applying the moment the budget is gone, rather than trusting anyone to notice.</summary>
    public bool StopWhenBudgetExhausted { get; set; } = true;

    // ── Approval & comms ─────────────────────────────────────────────────────
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>The one-page PDF the field shows the retailer.</summary>
    public string? CommunicationPackUrl { get; set; }

    public string? TermsAndConditions { get; set; }
    public string? Note { get; set; }

    // ── Rolling performance ──────────────────────────────────────────────────
    public int ApplicationCount { get; set; }
    public int BeneficiaryOutletCount { get; set; }
    public decimal QualifyingSalesValue { get; set; }

    public ICollection<TradeSchemeSlab> Slabs { get; set; } = [];
    public ICollection<TradeSchemeProduct> Products { get; set; } = [];
    public ICollection<TradeSchemeScope> Scopes { get; set; } = [];
}

/// <summary>
/// One tier of a slab scheme: buy between X and Y, earn this.
///
/// The "next slab" prompt on the field terminal reads from these — telling a retailer that two
/// more cases unlocks a better rate is the highest-return sentence in trade selling.
/// </summary>
public class TradeSchemeSlab : BaseEntity
{
    public Guid SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }

    public int SlabNumber { get; set; }
    public decimal FromQuantity { get; set; }

    /// <summary>Null on the open-ended top slab.</summary>
    public decimal? ToQuantity { get; set; }

    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }

    // ── The benefit at this tier. Whichever is non-zero applies. ─────────────
    public decimal FreeQuantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal PointsAwarded { get; set; }

    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Which SKUs a scheme applies to, and — for combo schemes — how much of each is needed.
///
/// A row with <see cref="IsQualifying"/> true counts toward earning the benefit; a row with it
/// false is a SKU the benefit is *paid in*. A buy-soap-get-shampoo scheme has both.
/// </summary>
public class TradeSchemeProduct : BaseEntity
{
    public Guid SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }

    /// <summary>Counts toward qualification. False means it is what the benefit is paid in.</summary>
    public bool IsQualifying { get; set; } = true;

    /// <summary>For combo schemes: how much of this SKU the basket must contain.</summary>
    public decimal RequiredQuantity { get; set; }

    public string? Uom { get; set; }
    public decimal UomFactor { get; set; } = 1;

    /// <summary>Excludes a SKU from a brand- or category-wide scheme.</summary>
    public bool IsExcluded { get; set; }
}

/// <summary>
/// Who a scheme applies to. Several rows are OR-ed; an empty set means everyone.
///
/// Modelled as rows rather than columns because a scheme routinely targets "channel = general
/// trade, in these three territories, excluding this one wholesaler", and that is a set.
/// </summary>
public class TradeSchemeScope : BaseEntity
{
    public Guid SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }

    public OutletChannel? Channel { get; set; }
    public OutletGrade? OutletGrade { get; set; }
    public PartnerType? PartnerTier { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }

    /// <summary>Carves an exception out of a broader scope row.</summary>
    public bool IsExcluded { get; set; }
}

/// <summary>
/// A scheme actually firing on an order — what was earned, on what basis.
///
/// This is the evidence trail. When a distributor files a claim six weeks later, the argument is
/// settled by this row rather than by re-running today's scheme rules against a period when they
/// were different.
/// </summary>
public class SchemeApplication : BaseEntity
{
    public Guid SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public TradeSchemeKind SchemeKind { get; set; }

    public Guid? OrderId { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? InvoiceId { get; set; }

    public DateTime AppliedAt { get; set; }

    /// <summary>Volume or value that earned the benefit.</summary>
    public decimal QualifyingQuantity { get; set; }
    public decimal QualifyingValue { get; set; }

    public int? SlabNumber { get; set; }

    // ── What was given ───────────────────────────────────────────────────────
    public decimal FreeQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal PointsAwarded { get; set; }

    /// <summary>Total cost to us, which is what depletes the budget.</summary>
    public decimal BenefitValue { get; set; }

    public SchemeSettlementMode SettlementMode { get; set; }

    /// <summary>Set for deferred schemes once the claim is raised.</summary>
    public Guid? ClaimId { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledAt { get; set; }

    /// <summary>Human-readable explanation shown at the counter and printed on the invoice.</summary>
    public string? BenefitDescription { get; set; }

    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
}

/// <summary>
/// Append-only movements against a scheme's budget.
///
/// A balance alone cannot answer "who spent it", and a scheme that mysteriously ran out three
/// weeks early is a conversation that needs line items.
/// </summary>
public class SchemeBudgetLedger : BaseEntity
{
    public Guid SchemeId { get; set; }
    public TradeScheme? Scheme { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>Signed: positive allocates budget, negative consumes it.</summary>
    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }
    public string? Reason { get; set; }

    public Guid? SchemeApplicationId { get; set; }
    public Guid? ClaimId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ActorUserId { get; set; }
}
