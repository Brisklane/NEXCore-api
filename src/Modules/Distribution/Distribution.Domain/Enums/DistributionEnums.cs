namespace Distribution.Domain.Enums;

// ── Network & channel ────────────────────────────────────────────────────────

/// <summary>
/// What kind of trading partner this is. The tier matters: a super-stockist owns
/// sub-distributors, and roll-ups walk that tree rather than summing a flat list.
/// </summary>
public enum PartnerType
{
    Distributor = 1,
    SubDistributor = 2,
    SuperStockist = 3,
    Wholesaler = 4,
    /// <summary>Carry &amp; forward agent — holds our stock, never owns it.</summary>
    CarryAndForward = 5,
    DirectRetailer = 6,
    ModernTradeAccount = 7,
    Institutional = 8,
    ExportBuyer = 9,
    Franchisee = 10,
}

/// <summary>How goods physically reach the partner's customers.</summary>
public enum ServicingModel
{
    /// <summary>Stock travels on the van and is sold at the door.</summary>
    VanSales = 1,
    /// <summary>Order taken today, delivered from the warehouse tomorrow.</summary>
    PreSalesAndDelivery = 2,
    DirectDispatch = 3,
    CrossDock = 4,
    /// <summary>Supplier ships straight to the end customer.</summary>
    DropShip = 5,
    /// <summary>Our stock sits at their premises; they are billed on consumption.</summary>
    Consignment = 6,
}

/// <summary>Where a partner is in its commercial life. Backward moves always carry a reason.</summary>
public enum PartnerStatus
{
    Lead = 1,
    KycSubmitted = 2,
    DocumentsVerified = 3,
    Approved = 4,
    Active = 5,
    Suspended = 6,
    Terminated = 7,
}

/// <summary>Trade channel an outlet belongs to. Drives pricing, schemes and survey assignment.</summary>
public enum OutletChannel
{
    GeneralTrade = 1,
    ModernTrade = 2,
    /// <summary>Hotels, restaurants and cafes.</summary>
    HoReCa = 3,
    Institutional = 4,
    ECommerceDarkStore = 5,
    Pharmacy = 6,
    Chemist = 7,
    Wholesale = 8,
    KioskPanShop = 9,
}

/// <summary>
/// Potential-based grade, not history-based. A new outlet in a busy market is an A from day one,
/// which is exactly why it deserves a weekly visit before it has ever bought anything.
/// </summary>
public enum OutletGrade
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
}

public enum OutletStatus
{
    /// <summary>Onboarded in the field, not yet approved — cannot be invoiced.</summary>
    PendingApproval = 1,
    Prospect = 2,
    Active = 3,
    TemporarilyClosed = 4,
    PermanentlyClosed = 5,
    /// <summary>Blocked by credit control; reversible.</summary>
    CreditBlocked = 6,
    Blacklisted = 7,
}

/// <summary>Equipment we place at an outlet and remain responsible for.</summary>
public enum OutletAssetKind
{
    Cooler = 1,
    Freezer = 2,
    DisplayRack = 3,
    Signage = 4,
    VisiCooler = 5,
    Dispenser = 6,
    Shelf = 7,
    Other = 99,
}

public enum AssetCondition
{
    Working = 1,
    NeedsService = 2,
    Faulty = 3,
    Missing = 4,
    Retrieved = 5,
}

// ── Routes & journey planning ────────────────────────────────────────────────

/// <summary>What the route is for. A collection-only route visits outlets it will not sell to.</summary>
public enum RouteKind
{
    VanSales = 1,
    PreSales = 2,
    Delivery = 3,
    Merchandiser = 4,
    CollectionOnly = 5,
}

/// <summary>How often a route repeats. Custom is expressed by the week-of-month mask on the route.</summary>
public enum VisitFrequency
{
    Daily = 1,
    AlternateDay = 2,
    Weekly = 3,
    Fortnightly = 4,
    Monthly = 5,
    Custom = 6,
}

public enum JourneyPlanDayStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    /// <summary>Not run — holiday, leave, or an explicit skip. Always carries a reason.</summary>
    Skipped = 4,
    Reassigned = 5,
}

// ── Field force ──────────────────────────────────────────────────────────────

public enum FieldRole
{
    SalesRep = 1,
    VanSalesman = 2,
    DeliveryDriver = 3,
    Merchandiser = 4,
    TeamLeader = 5,
    AreaSalesManager = 6,
    RegionalManager = 7,
}

public enum FieldDayStatus
{
    NotStarted = 1,
    Started = 2,
    Closed = 3,
    /// <summary>Closed by a supervisor over the rep's head; always logged.</summary>
    ForceClosed = 4,
}

/// <summary>Where one outlet visit is in its cycle. Drives the beat list rendering.</summary>
public enum VisitStatus
{
    Pending = 1,
    CheckedIn = 2,
    InProgress = 3,
    OrderTaken = 4,
    NoOrder = 5,
    CheckedOut = 6,
    Skipped = 7,
}

/// <summary>
/// Why a check-in was accepted despite being outside the outlet's geofence. Blocking the
/// check-in outright just moves the lie somewhere the system cannot see it, so it is recorded.
/// </summary>
public enum GeoValidation
{
    InsideFence = 1,
    OutsideFence = 2,
    /// <summary>Device gave no fix — indoors, airplane mode, permission denied.</summary>
    NoFix = 3,
    /// <summary>Outlet has no stored coordinates to compare against.</summary>
    NoOutletGeo = 4,
}

public enum SurveyQuestionKind
{
    SingleChoice = 1,
    MultiChoice = 2,
    Numeric = 3,
    Text = 4,
    Photo = 5,
    Signature = 6,
    Rating = 7,
    YesNo = 8,
    Date = 9,
}

/// <summary>What a merchandising audit was measuring, so scores stay comparable.</summary>
public enum AuditKind
{
    Planogram = 1,
    ShareOfShelf = 2,
    OnShelfAvailability = 3,
    PriceCompliance = 4,
    PosmPresence = 5,
    PerfectStore = 6,
}

// ── Van sales ────────────────────────────────────────────────────────────────

public enum VanLoadStatus
{
    Draft = 1,
    Requested = 2,
    Approved = 3,
    Picked = 4,
    Loaded = 5,
    Rejected = 6,
    Cancelled = 7,
}

/// <summary>
/// Which compartment of the van stock sits in. Returned goods must never fall back into
/// sellable stock by accident, so the compartment is part of the balance key.
/// </summary>
public enum VanCompartment
{
    Sellable = 1,
    SaleableReturn = 2,
    Damaged = 3,
    Expired = 4,
    /// <summary>Point-of-sale material carried for placement, never sold.</summary>
    Posm = 5,
    /// <summary>Free goods and samples issued at zero value.</summary>
    FreeIssue = 6,
}

public enum VanMovementKind
{
    LoadOut = 1,
    Sale = 2,
    FreeIssue = 3,
    CustomerReturn = 4,
    TransferIn = 5,
    TransferOut = 6,
    LoadIn = 7,
    CountAdjustment = 8,
    Damage = 9,
}

// ── Orders & fulfilment ──────────────────────────────────────────────────────

/// <summary>Where the demand came in from. Used for channel mix reporting and for SLAs.</summary>
public enum OrderSource
{
    FieldTerminal = 1,
    VanSale = 2,
    Telesales = 3,
    DistributorPortal = 4,
    BackOffice = 5,
    ApiInbound = 6,
    Marketplace = 7,
}

public enum DistributionOrderKind
{
    Standard = 1,
    Urgent = 2,
    SchemeDriven = 3,
    Sample = 4,
    FreeIssue = 5,
    Replacement = 6,
    ConsignmentFill = 7,
    DropShip = 8,
}

public enum DistributionOrderStatus
{
    Draft = 1,
    Submitted = 2,
    PendingApproval = 3,
    Approved = 4,
    Allocated = 5,
    Picking = 6,
    Picked = 7,
    Packed = 8,
    Loaded = 9,
    Dispatched = 10,
    PartiallyDelivered = 11,
    Delivered = 12,
    Invoiced = 13,
    Closed = 14,
    OnHold = 15,
    Rejected = 16,
    Cancelled = 17,
}

/// <summary>Which lot the allocator should reach for. FEFO is the default for anything dated.</summary>
public enum AllocationStrategy
{
    /// <summary>First expired, first out — the only correct default for dated goods.</summary>
    Fefo = 1,
    Fifo = 2,
    BatchSpecific = 3,
    WarehousePriority = 4,
    CustomerReserved = 5,
}

public enum PickStrategy
{
    /// <summary>One order, one picker, one pass.</summary>
    Discrete = 1,
    /// <summary>Many orders picked together, sorted afterwards.</summary>
    Batch = 2,
    /// <summary>A picker owns an aisle range and never leaves it.</summary>
    Zone = 3,
    /// <summary>Multi-order trolley, sorted at the point of pick.</summary>
    Cluster = 4,
    /// <summary>Released together against a carrier or route cut-off.</summary>
    Wave = 5,
}

public enum PickTaskStatus
{
    Released = 1,
    Assigned = 2,
    InProgress = 3,
    Picked = 4,
    ShortPicked = 5,
    Cancelled = 6,
}

public enum PackageKind
{
    Carton = 1,
    Pallet = 2,
    Crate = 3,
    Bag = 4,
    Loose = 5,
}

// ── Logistics ────────────────────────────────────────────────────────────────

public enum VehicleKind
{
    Van = 1,
    Truck = 2,
    ThreeWheeler = 3,
    Motorcycle = 4,
    /// <summary>Refrigerated — carries a cold-chain range and excursion log.</summary>
    Reefer = 5,
    Pickup = 6,
}

public enum VehicleOwnership
{
    Owned = 1,
    Leased = 2,
    Contracted = 3,
    ThirdParty = 4,
}

/// <summary>Statutory paperwork a vehicle must carry, each with its own expiry.</summary>
public enum VehicleComplianceKind
{
    Insurance = 1,
    Fitness = 2,
    Permit = 3,
    PollutionCertificate = 4,
    RoadTax = 5,
    Registration = 6,
}

public enum TripStatus
{
    Planned = 1,
    Loaded = 2,
    Departed = 3,
    InProgress = 4,
    Returned = 5,
    Settled = 6,
    Cancelled = 7,
}

public enum TripStopStatus
{
    Pending = 1,
    Arrived = 2,
    Delivered = 3,
    PartiallyDelivered = 4,
    Refused = 5,
    Rescheduled = 6,
    Failed = 7,
}

public enum TripExpenseKind
{
    Fuel = 1,
    Toll = 2,
    Parking = 3,
    LoadingLabour = 4,
    DriverAllowance = 5,
    Repair = 6,
    Fine = 7,
    Other = 99,
}

/// <summary>Line-level outcome at the door. A blanket "delivered" hides the argument that follows.</summary>
public enum PodLineOutcome
{
    Accepted = 1,
    ShortReceived = 2,
    Damaged = 3,
    Rejected = 4,
}

// ── Returns ──────────────────────────────────────────────────────────────────

public enum ReturnKind
{
    SaleableMarketReturn = 1,
    Damaged = 2,
    Expired = 3,
    NearExpiryBuyback = 4,
    WrongSupply = 5,
    QualityComplaint = 6,
    RecallReturn = 7,
    SalesReturnAgainstInvoice = 8,
    UnbilledPickup = 9,
}

public enum ReturnStatus
{
    Requested = 1,
    Approved = 2,
    Rejected = 3,
    Collected = 4,
    Received = 5,
    Inspected = 6,
    Credited = 7,
    Closed = 8,
    Cancelled = 9,
}

/// <summary>What happened to the goods after inspection. Drives the stock move and the write-off.</summary>
public enum ReturnDispositionKind
{
    Restock = 1,
    Repack = 2,
    DiscountAndSell = 3,
    Scrap = 4,
    ReturnToSupplier = 5,
    InsuranceClaim = 6,
}

public enum ReturnValuationBasis
{
    OriginalInvoicePrice = 1,
    CurrentPrice = 2,
    PolicyPercentage = 3,
}

// ── Pricing & trade schemes ──────────────────────────────────────────────────

/// <summary>
/// Which axis a price list is keyed on. Resolution walks these most-specific-first, and the
/// winning rule is recorded on the line so "why this price" has an answer.
/// </summary>
public enum PriceScope
{
    Company = 1,
    Channel = 2,
    Territory = 3,
    PartnerTier = 4,
    Partner = 5,
    Outlet = 6,
    Contract = 7,
}

public enum TradeSchemeKind
{
    /// <summary>Buy N get M free — same SKU or another.</summary>
    QuantityFreeGoods = 1,
    /// <summary>Quantity purchase scheme: tiered benefit by volume over a period.</summary>
    QuantitySlab = 2,
    ValueSlab = 3,
    PercentageDiscount = 4,
    FlatAmountOff = 5,
    /// <summary>Qualify by buying across a defined basket.</summary>
    ComboAssortment = 6,
    /// <summary>Payout for maintaining a display, evidenced by photo.</summary>
    Display = 7,
    /// <summary>Discount for settling inside N days.</summary>
    CashDiscount = 8,
    LoyaltyPoints = 9,
    SamplingFreeIssue = 10,
    /// <summary>Auto-targeted at near-expiry batches.</summary>
    Liquidation = 11,
    TradeOfferBundle = 12,
}

/// <summary>When the benefit lands. Deferred schemes become a claim rather than an invoice line.</summary>
public enum SchemeSettlementMode
{
    OnInvoice = 1,
    /// <summary>Evaluated in arrears over a period, settled as a claim.</summary>
    Deferred = 2,
    FreeGoodsIssue = 3,
    CreditNote = 4,
}

/// <summary>How a scheme behaves when another one also qualifies.</summary>
public enum SchemeStacking
{
    /// <summary>Wins alone; nothing else applies.</summary>
    Exclusive = 1,
    Combinable = 2,
    /// <summary>Only the single best-value scheme in the group applies.</summary>
    BestOfGroup = 3,
}

public enum SchemeStatus
{
    Draft = 1,
    PendingApproval = 2,
    Approved = 3,
    Active = 4,
    Paused = 5,
    /// <summary>Budget consumed — stops applying without anyone having to remember.</summary>
    Exhausted = 6,
    Expired = 7,
    Cancelled = 8,
}

// ── Claims & money ───────────────────────────────────────────────────────────

public enum ClaimKind
{
    Scheme = 1,
    Damage = 2,
    Expiry = 3,
    Freight = 4,
    Display = 5,
    MarketReturn = 6,
    /// <summary>A price cut on stock already sitting in the channel.</summary>
    PriceProtection = 7,
    Chargeback = 8,
    Manual = 99,
}

public enum ClaimStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    QueryRaised = 4,
    Resubmitted = 5,
    Approved = 6,
    PartiallyApproved = 7,
    Rejected = 8,
    Settled = 9,
    Cancelled = 10,
}

public enum ClaimSettlementMode
{
    CreditNote = 1,
    CashPayment = 2,
    AdjustAgainstNextInvoice = 3,
    OffsetOutstanding = 4,
}

public enum PaymentTender
{
    Cash = 1,
    Cheque = 2,
    BankTransfer = 3,
    Upi = 4,
    Wallet = 5,
    Card = 6,
    /// <summary>Settled by adjusting a credit note rather than by money moving.</summary>
    CreditAdjustment = 7,
}

public enum ChequeStatus
{
    Received = 1,
    Deposited = 2,
    Cleared = 3,
    Bounced = 4,
    Cancelled = 5,
    /// <summary>Post-dated: held until its date comes round.</summary>
    Held = 6,
}

public enum CreditEnforcement
{
    /// <summary>Limit is informational only.</summary>
    Off = 1,
    /// <summary>Shows a warning, lets the user continue.</summary>
    Warn = 2,
    /// <summary>Refuses the transaction until an override is granted.</summary>
    Block = 3,
}

public enum SettlementStatus
{
    Open = 1,
    Submitted = 2,
    /// <summary>Variances outside tolerance are waiting on an approver.</summary>
    PendingApproval = 3,
    Approved = 4,
    Closed = 5,
    Reversed = 6,
}

/// <summary>What kind of gap a settlement found. Every one needs a reason before the day closes.</summary>
public enum VarianceKind
{
    CashShort = 1,
    CashOver = 2,
    StockShort = 3,
    StockExcess = 4,
    UnbilledReturn = 5,
    UnexplainedDiscount = 6,
}

// ── Secondary sales & DMS ────────────────────────────────────────────────────

/// <summary>
/// How a distributor's secondary sales reach us. Partners sit at very different levels of
/// maturity and forcing all three onto one path is how DMS rollouts stall.
/// </summary>
public enum SecondaryCaptureMode
{
    /// <summary>The partner runs Distribution; every secondary invoice is already a record here.</summary>
    Transactional = 1,
    /// <summary>Periodic file upload, translated through a per-partner mapping profile.</summary>
    Uploaded = 2,
    /// <summary>A simple portal form: SKU, quantity, value.</summary>
    Declared = 3,
}

public enum UploadBatchStatus
{
    Received = 1,
    Validating = 2,
    /// <summary>Some rows did not map; they are sitting in the exception queue.</summary>
    PartiallyMapped = 3,
    Mapped = 4,
    Posted = 5,
    Rejected = 6,
}

/// <summary>Which side of the sell-in / sell-out identity failed to balance.</summary>
public enum ReconciliationOutcome
{
    Balanced = 1,
    ShortDeclared = 2,
    OverDeclared = 3,
    /// <summary>No declaration arrived for the period at all.</summary>
    Missing = 4,
}

// ── Targets & performance ────────────────────────────────────────────────────

/// <summary>What a target measures. Mixing these on one row is what makes targets unauditable.</summary>
public enum TargetMetric
{
    SalesValue = 1,
    SalesVolume = 2,
    Collection = 3,
    Coverage = 4,
    ProductiveCalls = 5,
    NewOutlets = 6,
    MustSellCompliance = 7,
    LinesPerCall = 8,
    RangeSelling = 9,
}

public enum TargetPeriod
{
    Monthly = 1,
    Quarterly = 2,
    Annual = 3,
    Weekly = 4,
}

/// <summary>Who or what the target is set against.</summary>
public enum TargetScope
{
    Company = 1,
    Territory = 2,
    Route = 3,
    FieldRep = 4,
    Partner = 5,
    Outlet = 6,
}

public enum IncentiveBasis
{
    /// <summary>Pays a rate per slab reached.</summary>
    Slab = 1,
    /// <summary>Pays proportionally from the first unit.</summary>
    Linear = 2,
    /// <summary>Pays nothing until a gate (usually coverage) is cleared.</summary>
    Gated = 3,
    Team = 4,
    /// <summary>A short bonus on a focus SKU.</summary>
    Spiff = 5,
}

// ── Planning ─────────────────────────────────────────────────────────────────

public enum ForecastBasis
{
    /// <summary>Forecasting on secondary sales — real demand.</summary>
    SecondarySalesHistory = 1,
    /// <summary>Forecasting on primary sales forecasts your own pipeline stuffing. Available, not advised.</summary>
    PrimarySalesHistory = 2,
    Manual = 3,
    /// <summary>Seasonally adjusted moving average over the chosen history.</summary>
    SeasonalAdjusted = 4,
}

public enum ReplenishmentTargetKind
{
    Distributor = 1,
    Van = 2,
    Warehouse = 3,
}

public enum TransferRequestStatus
{
    Draft = 1,
    Requested = 2,
    Approved = 3,
    InTransit = 4,
    Received = 5,
    Rejected = 6,
    Cancelled = 7,
}

// ── Traceability & compliance ────────────────────────────────────────────────

public enum ColdChainPointKind
{
    ColdRoom = 1,
    Freezer = 2,
    ReeferVehicle = 3,
    OutletCooler = 4,
    ReceivingDock = 5,
}

public enum RecallStatus
{
    Draft = 1,
    Announced = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}

public enum RecallSeverity
{
    /// <summary>Reasonable probability of serious health consequence.</summary>
    ClassI = 1,
    /// <summary>Temporary or medically reversible consequence.</summary>
    ClassII = 2,
    /// <summary>Unlikely to cause harm — labelling, packaging.</summary>
    ClassIII = 3,
    /// <summary>Voluntary withdrawal, not a safety recall.</summary>
    Withdrawal = 4,
}

/// <summary>
/// Where a reason code is offered. Keeping the surface on the code means one registry serves
/// every dropdown without a deploy, and a "damaged" reason never shows up in a no-order list.
/// </summary>
public enum ReasonSurface
{
    NoOrder = 1,
    VisitSkipped = 2,
    OutOfFenceCheckIn = 3,
    OrderCancellation = 4,
    OrderRejection = 5,
    ShortPick = 6,
    DeliveryFailure = 7,
    Return = 8,
    StockVariance = 9,
    CashVariance = 10,
    CreditOverride = 11,
    FefoOverride = 12,
    ClaimRejection = 13,
    PriceOverride = 14,
    Wastage = 15,
}

// ── Notifications ────────────────────────────────────────────────────────────

public enum DistributionAlertKind
{
    OrderApproved = 1,
    OrderRejected = 2,
    CreditLimitBreached = 3,
    ChequeBounced = 4,
    DeliveryFailed = 5,
    RouteUnsettled = 6,
    StockVarianceHigh = 7,
    ClaimApproved = 8,
    ClaimRejected = 9,
    ClaimQueried = 10,
    SchemeBudgetExhausted = 11,
    NearExpiryThreshold = 12,
    LicenceExpiring = 13,
    TargetMilestone = 14,
    DayNotStarted = 15,
    RecallAnnounced = 16,
    ColdChainExcursion = 17,
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}
