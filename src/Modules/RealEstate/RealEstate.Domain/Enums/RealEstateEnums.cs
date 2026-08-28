namespace RealEstate.Domain.Enums;

// =====================================================================================
// Real Estate module vocabulary.
//
// One file, because these are read together: a reader working out what a Booking is has to
// know what a BookingStatus, a SourcingChannel and an AllocationOrder are, and hunting them
// across thirty files teaches nothing.
//
// Every enum is explicitly numbered. The numbers are persisted, so a value may be added at the
// end of a block but never renumbered and never removed — a stored 4 must mean the same thing
// in five years as it does today.
// =====================================================================================

// ── Shape of the business ────────────────────────────────────────────────────────────

/// <summary>
/// Which of the four businesses a company runs. Most firms run two or three at once, so this is
/// a set of switches rather than a single choice — see <c>LineOfBusinessConfig</c>.
/// </summary>
public enum LineOfBusiness
{
    /// <summary>Sells and lets other people's property for a fee.</summary>
    Brokerage = 1,

    /// <summary>Builds and sells its own stock — societies, towers, plot schemes, marts.</summary>
    Development = 2,

    /// <summary>Builds on someone else's land for a contract price.</summary>
    Contracting = 3,

    /// <summary>Runs standing stock forever — tenancies, service charges, society dues.</summary>
    EstateManagement = 4,
}

public enum OfficeType
{
    HeadOffice = 1,
    Branch = 2,
    /// <summary>The cabin on the project site where bookings are actually written.</summary>
    ProjectSalesOffice = 3,
    SiteOffice = 4,
    Franchise = 5,
}

/// <summary>
/// How area is spoken about. The canonical stored value is always square feet; this is what the
/// operator reads and types. A Lahore office says "10 marla" and a Dubai office says "2,722 sq ft"
/// about the same plot, and both must be right.
/// </summary>
public enum AreaUnit
{
    SquareFeet = 1,
    SquareMetre = 2,
    SquareYard = 3,
    Marla = 4,
    Kanal = 5,
    Acre = 6,
    Hectare = 7,
    Bigha = 8,
    Cent = 9,
    Guntha = 10,
}

// ── Property ─────────────────────────────────────────────────────────────────────────

/// <summary>Top level of the property taxonomy. The second level is <see cref="PropertySubType"/>.</summary>
public enum PropertyCategory
{
    Residential = 1,
    Commercial = 2,
    Land = 3,
    Industrial = 4,
    Other = 5,
}

public enum PropertySubType
{
    // Residential
    Apartment = 1,
    Studio = 2,
    Penthouse = 3,
    Duplex = 4,
    Villa = 5,
    House = 6,
    Townhouse = 7,
    Farmhouse = 8,
    Room = 9,
    ServantQuarter = 10,

    // Commercial
    Shop = 30,
    Showroom = 31,
    Office = 32,
    Floor = 33,
    Building = 34,
    FoodCourtUnit = 35,
    Kiosk = 36,
    MartUnit = 37,
    Warehouse = 38,
    ColdStore = 39,
    Plaza = 40,

    // Land
    ResidentialPlot = 60,
    CommercialPlot = 61,
    IndustrialPlot = 62,
    AgriculturalLand = 63,
    FarmLand = 64,
    Orchard = 65,
    /// <summary>A right to a plot that has not been allotted a number yet. Sold before the ballot.</summary>
    PlotFile = 66,

    // Industrial
    Factory = 80,
    Godown = 81,
    IndustrialShed = 82,

    // Other
    ParkingSpace = 100,
    Storage = 101,
    SignageSpace = 102,
    CommonArea = 103,
    AmenitySpace = 104,
    RoofRights = 105,
    Basement = 106,
}

/// <summary>
/// The single status vocabulary every board colours by. Deliberately one enum across sale,
/// letting and development: a unit that is <see cref="Held"/> is held whichever screen you are on.
/// </summary>
public enum PropertyStatus
{
    Draft = 1,
    Available = 2,
    /// <summary>Held for a named lead with an expiry clock. Not a sale, and it auto-releases.</summary>
    Held = 3,
    /// <summary>A token or EOI has been taken against it.</summary>
    Reserved = 4,
    Booked = 5,
    Sold = 6,
    /// <summary>Deed registered / mutation done.</summary>
    Registered = 7,
    Possessed = 8,
    Let = 9,
    UnderOffer = 10,
    UnderConstruction = 11,
    /// <summary>Deliberately off-market — owner quota, landowner share, pledged to a lender.</summary>
    Blocked = 12,
    Litigation = 13,
    Withdrawn = 14,
    NotForSale = 15,
}

public enum OccupancyState
{
    Vacant = 1,
    OwnerOccupied = 2,
    Tenanted = 3,
    UnderRenovation = 4,
    UnlawfullyOccupied = 5,
    NotBuilt = 6,
}

public enum Tenure
{
    Freehold = 1,
    Leasehold = 2,
    ShareOfFreehold = 3,
    Licence = 4,
    /// <summary>Allotment from a development authority — common across South Asia.</summary>
    Allotment = 5,
    LeaseToOwn = 6,
}

public enum FurnishingState
{
    Unfurnished = 1,
    SemiFurnished = 2,
    Furnished = 3,
    FullyFitted = 4,
    ShellAndCore = 5,
}

public enum Facing
{
    North = 1,
    NorthEast = 2,
    East = 3,
    SouthEast = 4,
    South = 5,
    SouthWest = 6,
    West = 7,
    NorthWest = 8,
}

public enum PropertyCondition
{
    NewBuild = 1,
    Excellent = 2,
    Good = 3,
    Fair = 4,
    NeedsRenovation = 5,
    Derelict = 6,
    UnderConstruction = 7,
}

public enum MediaKind
{
    Photo = 1,
    FloorPlan = 2,
    SitePlan = 3,
    Brochure = 4,
    Video = 5,
    VirtualTour = 6,
    Drone = 7,
    Document = 8,
}

public enum PropertyRelationKind
{
    /// <summary>This property physically contains the other (a tower contains a floor).</summary>
    Contains = 1,
    /// <summary>This property was created by splitting the other.</summary>
    SubdividedFrom = 2,
    /// <summary>This property was created by merging the others.</summary>
    AmalgamatedFrom = 3,
    /// <summary>Attached to the other and sold with it — a parking bay to an apartment.</summary>
    AttachedTo = 4,
}

// ── Land, title & acquisition ────────────────────────────────────────────────────────

public enum TitleInstrument
{
    SaleDeed = 1,
    GiftDeed = 2,
    Inheritance = 3,
    Partition = 4,
    CourtDecree = 5,
    AllotmentLetter = 6,
    Exchange = 7,
    LeaseDeed = 8,
    PowerOfAttorney = 9,
    Mutation = 10,
}

public enum EncumbranceKind
{
    Mortgage = 1,
    Charge = 2,
    Lien = 3,
    Lease = 4,
    Easement = 5,
    RightOfWay = 6,
    Tenancy = 7,
    Litigation = 8,
    Attachment = 9,
    AcquisitionNotice = 10,
}

public enum EncumbranceStatus
{
    Active = 1,
    UnderClearance = 2,
    Cleared = 3,
    Disputed = 4,
}

public enum VerificationVerdict
{
    Pending = 1,
    Passed = 2,
    Failed = 3,
    /// <summary>Passed subject to a condition that must be satisfied before completion.</summary>
    Conditional = 4,
    NotApplicable = 5,
}

public enum AcquisitionStageKind
{
    Identified = 1,
    UnderNegotiation = 2,
    TermSheet = 3,
    DueDiligence = 4,
    AgreementToSell = 5,
    AdvancePaid = 6,
    Registration = 7,
    Mutation = 8,
    PossessionTaken = 9,
    Aborted = 10,
}

// ── Projects & inventory ─────────────────────────────────────────────────────────────

public enum ProjectKind
{
    HousingSociety = 1,
    PlotScheme = 2,
    ApartmentTower = 3,
    MixedUse = 4,
    /// <summary>A mart / shopping centre — units let or sold to retailers.</summary>
    ShoppingCentre = 5,
    CommercialPlaza = 6,
    GatedVillaCommunity = 7,
    IndustrialEstate = 8,
    FarmhouseScheme = 9,
    SingleBuilding = 10,
    /// <summary>A turnkey build on the customer's own land. Has no sales inventory.</summary>
    ClientBuild = 11,
}

public enum ProjectStatus
{
    Concept = 1,
    Planning = 2,
    Approved = 3,
    Launched = 4,
    UnderConstruction = 5,
    Completed = 6,
    HandedOver = 7,
    Closed = 8,
    OnHold = 9,
}

/// <summary>A level in the project tree. The same entity models all of them so the tree can be any depth.</summary>
public enum ProjectNodeKind
{
    Phase = 1,
    Block = 2,
    Sector = 3,
    Tower = 4,
    Street = 5,
    Wing = 6,
    Cluster = 7,
    /// <summary>A storey inside a tower. Carries the floor number every unit on it inherits.</summary>
    Floor = 8,
}

public enum MilestoneStatus
{
    NotStarted = 1,
    InProgress = 2,
    /// <summary>Reached, but not yet certified by the engineer. Does not release a demand.</summary>
    Reached = 3,
    /// <summary>Certified. This is the state that raises a customer demand and allows a claim.</summary>
    Certified = 4,
    Skipped = 5,
}

public enum HoldStatus
{
    Active = 1,
    Converted = 2,
    Expired = 3,
    ReleasedManually = 4,
}

public enum BlockReason
{
    OwnerQuota = 1,
    LandownerShare = 2,
    DealerAllocation = 3,
    PledgedToLender = 4,
    Litigation = 5,
    StructuralIssue = 6,
    ManagementHold = 7,
    StaffQuota = 8,
}

/// <summary>What a premium or charge attaches to, which decides how it is computed.</summary>
public enum ChargeBasis
{
    /// <summary>A flat amount per unit.</summary>
    Fixed = 1,
    /// <summary>A rate multiplied by the unit's saleable area.</summary>
    PerAreaUnit = 2,
    /// <summary>A percentage of the base price.</summary>
    PercentOfBase = 3,
    /// <summary>A percentage of the total consideration including other premiums.</summary>
    PercentOfTotal = 4,
    /// <summary>A rate per floor above a datum — the floor-rise premium.</summary>
    PerFloor = 5,
}

public enum ChargeKind
{
    BasePrice = 1,
    FloorRise = 2,
    Corner = 3,
    ParkFacing = 4,
    MainRoadFacing = 5,
    Boulevard = 6,
    View = 7,
    /// <summary>Preferential location charge — the catch-all premium.</summary>
    Plc = 8,
    DevelopmentCharge = 9,
    ClubMembership = 10,
    UtilityConnection = 11,
    Parking = 12,
    MaintenanceAdvance = 13,
    CorpusFund = 14,
    Documentation = 15,
    StampDuty = 16,
    RegistrationFee = 17,
    Tax = 18,
    TransferFee = 19,
    PossessionCharge = 20,
    Other = 99,
}

// ── Listings & marketing ─────────────────────────────────────────────────────────────

public enum ListingKind
{
    ForSale = 1,
    ForRent = 2,
    ForLease = 3,
    ForAuction = 4,
    /// <summary>Off-plan / pre-launch developer stock.</summary>
    OffPlan = 5,
    Resale = 6,
    Exchange = 7,
    /// <summary>A requirement advertised rather than a property — "wanted".</summary>
    Wanted = 8,
}

public enum ListingStatus
{
    Draft = 1,
    PendingApproval = 2,
    Live = 3,
    UnderOffer = 4,
    /// <summary>Sold or let subject to contract — still ours, not yet completed.</summary>
    SubjectToContract = 5,
    Completed = 6,
    Withdrawn = 7,
    Expired = 8,
    Rejected = 9,
}

/// <summary>The agency terms. Decides whether a fee is earned when someone else sells it.</summary>
public enum AgencyBasis
{
    SoleAgency = 1,
    SoleSellingRights = 2,
    JointSole = 3,
    MultipleAgency = 4,
    /// <summary>The firm is the owner. No instruction, no third-party fee.</summary>
    OwnStock = 5,
}

public enum FeeBasis
{
    PercentOfPrice = 1,
    FixedAmount = 2,
    Tiered = 3,
    /// <summary>A number of weeks' or months' rent — the letting norm.</summary>
    PeriodsOfRent = 4,
}

public enum PortalPublishState
{
    NotPublished = 1,
    Queued = 2,
    Published = 3,
    UpdatePending = 4,
    WithdrawPending = 5,
    Withdrawn = 6,
    Failed = 7,
    /// <summary>The portal accepted it but flagged content problems.</summary>
    PublishedWithWarnings = 8,
}

// ── CRM ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A party's role is additive: the same person is frequently a buyer on one unit and a landlord
/// of another, and the 360 has to show both.
/// </summary>
public enum PartyRoleKind
{
    Lead = 1,
    Buyer = 2,
    Seller = 3,
    Landlord = 4,
    Tenant = 5,
    Investor = 6,
    Guarantor = 7,
    Nominee = 8,
    CoApplicant = 9,
    Resident = 10,
    ChannelPartner = 11,
    Contractor = 12,
    Supplier = 13,
    Solicitor = 14,
    Lender = 15,
    Landowner = 16,
    Client = 17,
}

public enum PartyKind
{
    Individual = 1,
    Organisation = 2,
    /// <summary>Two or more people buying together as one applicant set.</summary>
    Joint = 3,
    Trust = 4,
    GovernmentBody = 5,
}

public enum IdentityKind
{
    NationalId = 1,
    Passport = 2,
    DrivingLicence = 3,
    TaxNumber = 4,
    CompanyRegistration = 5,
    ResidencePermit = 6,
    Other = 99,
}

public enum KycStatus
{
    NotStarted = 1,
    InProgress = 2,
    PendingVerification = 3,
    Verified = 4,
    Rejected = 5,
    Expired = 6,
    /// <summary>Verified but flagged for enhanced due diligence — PEP, high value, high-risk market.</summary>
    EnhancedReview = 7,
}

public enum RiskRating
{
    Low = 1,
    Medium = 2,
    High = 3,
    Prohibited = 4,
}

public enum EnquiryStage
{
    New = 1,
    Contacted = 2,
    Qualified = 3,
    ViewingBooked = 4,
    Viewed = 5,
    Revisit = 6,
    Negotiation = 7,
    OfferMade = 8,
    /// <summary>Token or EOI taken — developer path.</summary>
    Tokened = 9,
    Agreed = 10,
    Booked = 11,
    Completed = 12,
    Lost = 13,
    /// <summary>Parked in the nurture list rather than lost.</summary>
    Dormant = 14,
}

public enum EnquiryChannel
{
    Website = 1,
    Portal = 2,
    Phone = 3,
    WhatsApp = 4,
    WalkIn = 5,
    Referral = 6,
    Campaign = 7,
    QrCode = 8,
    ChannelPartner = 9,
    Exhibition = 10,
    ColdCall = 11,
    Chatbot = 12,
    Email = 13,
    SocialMedia = 14,
    Manual = 15,
}

public enum BuyingPurpose
{
    OwnUse = 1,
    Investment = 2,
    RentalYield = 3,
    Resale = 4,
    BusinessPremises = 5,
}

public enum FundingKind
{
    Cash = 1,
    Mortgage = 2,
    Instalments = 3,
    SaleOfExisting = 4,
    CompanyFunds = 5,
    Undecided = 6,
}

public enum ActivityKind
{
    Call = 1,
    WhatsApp = 2,
    Email = 3,
    Sms = 4,
    Meeting = 5,
    Viewing = 6,
    SiteVisit = 7,
    Note = 8,
    DocumentSent = 9,
    PortalMessage = 10,
    StatusChange = 11,
    Task = 12,
}

public enum ActivityDirection
{
    Outbound = 1,
    Inbound = 2,
    Internal = 3,
}

public enum TaskState
{
    Open = 1,
    Done = 2,
    Cancelled = 3,
    Overdue = 4,
}

// ── Viewings & visits ────────────────────────────────────────────────────────────────

public enum ViewingStatus
{
    Scheduled = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5,
    Rescheduled = 6,
}

public enum InterestLevel
{
    NotInterested = 1,
    Lukewarm = 2,
    Interested = 3,
    VeryInterested = 4,
    ReadyToProceed = 5,
}

public enum AccessArrangement
{
    AgentHasKeys = 1,
    KeySafe = 2,
    VendorPresent = 3,
    TenantPresent = 4,
    Concierge = 5,
    SiteOffice = 6,
}

public enum TransportArrangement
{
    OwnTransport = 1,
    CompanyCab = 2,
    PickUp = 3,
    Shuttle = 4,
}

// ── Offers, bookings & allotment ─────────────────────────────────────────────────────

public enum OfferStatus
{
    Submitted = 1,
    UnderConsideration = 2,
    Countered = 3,
    Accepted = 4,
    Rejected = 5,
    Withdrawn = 6,
    Lapsed = 7,
}

public enum OfferConditionKind
{
    SubjectToSurvey = 1,
    SubjectToMortgage = 2,
    SubjectToSaleOfOwn = 3,
    SubjectToPlanning = 4,
    ChainFree = 5,
    VacantPossession = 6,
    Other = 99,
}

public enum ReservationStatus
{
    Active = 1,
    ConvertedToBooking = 2,
    Expired = 3,
    Refunded = 4,
    Forfeited = 5,
    Cancelled = 6,
}

public enum BookingStatus
{
    /// <summary>Written but not yet paid for. The unit is held, not sold.</summary>
    Provisional = 1,
    PendingApproval = 2,
    Confirmed = 3,
    AgreementSigned = 4,
    /// <summary>Behind on the plan and inside the dunning ladder.</summary>
    Defaulting = 5,
    UnderCancellation = 6,
    Cancelled = 7,
    Transferred = 8,
    PossessionOffered = 9,
    Possessed = 10,
    Completed = 11,
}

public enum SourcingChannel
{
    /// <summary>Walked in or came through our own marketing. No third-party commission.</summary>
    Direct = 1,
    InHouseAgent = 2,
    ChannelPartner = 3,
    Referral = 4,
    OnlinePortal = 5,
    Campaign = 6,
}

public enum BallotStatus
{
    Draft = 1,
    PoolLocked = 2,
    Drawn = 3,
    Published = 4,
    Cancelled = 5,
}

public enum AllotmentStatus
{
    Issued = 1,
    Reissued = 2,
    Superseded = 3,
    Cancelled = 4,
}

// ── Payment plans & money in ─────────────────────────────────────────────────────────

public enum InstalmentKind
{
    BookingAmount = 1,
    ConfirmationAmount = 2,
    /// <summary>The regular periodic instalment.</summary>
    Periodic = 3,
    /// <summary>The extra half-yearly or annual lump South Asian plans layer on top.</summary>
    Balloon = 4,
    /// <summary>Falls due when a construction milestone is certified, not on a date.</summary>
    MilestoneLinked = 5,
    PossessionBalance = 6,
    PostPossession = 7,
    Charge = 8,
}

public enum InstalmentFrequency
{
    Monthly = 1,
    BiMonthly = 2,
    Quarterly = 3,
    HalfYearly = 4,
    Yearly = 5,
    OneOff = 6,
    Custom = 7,
}

public enum InstalmentStatus
{
    NotDue = 1,
    Due = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    Waived = 6,
    Cancelled = 7,
    /// <summary>Rolled into a restructured plan; kept for history but no longer collectable.</summary>
    Restructured = 8,
}

public enum DemandStatus
{
    Draft = 1,
    Generated = 2,
    Sent = 3,
    Acknowledged = 4,
    Settled = 5,
    Cancelled = 6,
    Failed = 7,
}

/// <summary>How the late-payment surcharge accrues. Getting this wrong is the top dispute source.</summary>
public enum SurchargeBasis
{
    /// <summary>Rate per day on the overdue amount.</summary>
    PerDayOnOverdue = 1,
    /// <summary>Rate per month on the overdue amount, part months counted whole.</summary>
    PerMonthOnOverdue = 2,
    /// <summary>Rate per month on the whole outstanding balance, not just what is late.</summary>
    PerMonthOnOutstanding = 3,
    /// <summary>A flat penalty per missed instalment, regardless of how late.</summary>
    FlatPerInstalment = 4,
}

public enum PaymentInstrument
{
    Cash = 1,
    Cheque = 2,
    BankTransfer = 3,
    Online = 4,
    Card = 5,
    DemandDraft = 6,
    PayOrder = 7,
    /// <summary>Adjusted against a credit the customer already holds.</summary>
    Adjustment = 8,
    Cryptocurrency = 9,
}

public enum ChequeState
{
    Received = 1,
    Deposited = 2,
    Cleared = 3,
    Bounced = 4,
    /// <summary>Handed back to the customer, usually on cancellation.</summary>
    Returned = 5,
    /// <summary>Post-dated and not yet at its maturity date.</summary>
    Pending = 6,
    StopPayment = 7,
}

public enum ReceiptStatus
{
    Draft = 1,
    Posted = 2,
    /// <summary>Money in, but not yet applied to any instalment. Visible, never lost.</summary>
    OnAccount = 3,
    Reversed = 4,
    Cancelled = 5,
}

/// <summary>The order a receipt is consumed in. Configurable, because firms genuinely differ.</summary>
public enum AllocationOrder
{
    /// <summary>Surcharge, then oldest instalment, then charges. The common default.</summary>
    SurchargeFirstThenOldest = 1,
    /// <summary>Oldest instalment first, surcharge last. Customer-friendly.</summary>
    OldestFirstThenSurcharge = 2,
    /// <summary>Principal only; surcharge is chased separately.</summary>
    PrincipalOnly = 3,
    /// <summary>Nothing automatic — a human decides every time.</summary>
    Manual = 4,
}

public enum LedgerEntryKind
{
    Demand = 1,
    Receipt = 2,
    Surcharge = 3,
    SurchargeWaiver = 4,
    Adjustment = 5,
    Refund = 6,
    Forfeiture = 7,
    Discount = 8,
    Tax = 9,
    Reversal = 10,
    OpeningBalance = 11,
    TransferIn = 12,
    TransferOut = 13,
}

public enum DunningAction
{
    None = 1,
    SendReminder = 2,
    CreateCallTask = 3,
    ApplySurcharge = 4,
    IssueNotice = 5,
    IssueFinalNotice = 6,
    SuspendServices = 7,
    ReferToLegal = 8,
    ProposeCancellation = 9,
}

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Sms = 3,
    WhatsApp = 4,
    Push = 5,
    Post = 6,
    Call = 7,
}

public enum PromiseState
{
    Open = 1,
    Kept = 2,
    Broken = 3,
    Cancelled = 4,
}

// ── Cancellation, transfer & possession ──────────────────────────────────────────────

public enum CancellationTrigger
{
    CustomerWithdrawal = 1,
    Default = 2,
    /// <summary>The developer cancelled — project abandoned, plan changed, regulatory.</summary>
    DeveloperInitiated = 3,
    MutualAgreement = 4,
    DeathOfApplicant = 5,
    Fraud = 6,
}

public enum DeductionBasis
{
    ForfeitBookingAmount = 1,
    PercentOfPrice = 2,
    PercentOfPaid = 3,
    /// <summary>A slab that depends on how far into the plan the cancellation falls.</summary>
    SlabByElapsed = 4,
    FlatAmount = 5,
    NoDeduction = 6,
}

public enum RefundStatus
{
    Requested = 1,
    Calculated = 2,
    Approved = 3,
    Scheduled = 4,
    PartiallyPaid = 5,
    Paid = 6,
    Rejected = 7,
    /// <summary>Payable only once the unit resells — a real and common term.</summary>
    AwaitingResale = 8,
}

public enum TransferKind
{
    Sale = 1,
    Gift = 2,
    Inheritance = 3,
    CourtDecree = 4,
    PowerOfAttorney = 5,
    /// <summary>Only part of a share moves; both parties remain owners.</summary>
    PartialShare = 6,
    /// <summary>Same owner, different unit.</summary>
    UnitChange = 7,
    NameCorrection = 8,
}

public enum TransferStatus
{
    Requested = 1,
    DuesCheckPending = 2,
    /// <summary>Dues outstanding. Blocked until cleared or an authority overrides.</summary>
    BlockedOnDues = 3,
    DocumentsPending = 4,
    NocIssued = 5,
    FeesPending = 6,
    SessionScheduled = 7,
    Completed = 8,
    Rejected = 9,
    Cancelled = 10,
}

public enum PossessionStatus
{
    NotEligible = 1,
    Eligible = 2,
    Offered = 3,
    AppointmentSet = 4,
    InspectionDone = 5,
    /// <summary>Critical snags found. Handover blocked until they close.</summary>
    SnagsOutstanding = 6,
    HandedOver = 7,
    Declined = 8,
}

public enum SnagSeverity
{
    /// <summary>Must be fixed before handover. Blocks possession.</summary>
    Critical = 1,
    /// <summary>Fixed within the agreed window after handover.</summary>
    Major = 2,
    /// <summary>Cosmetic. Fixed inside the defect liability period.</summary>
    Minor = 3,
}

public enum SnagZone
{
    Structure = 1,
    WallsAndFinishes = 2,
    Floors = 3,
    DoorsAndWindows = 4,
    Electrical = 5,
    PlumbingAndSanitary = 6,
    Hvac = 7,
    ExternalAndCommon = 8,
}

public enum SnagStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Fixed = 4,
    /// <summary>Fixed and re-inspected. Only this state closes it.</summary>
    Verified = 5,
    Rejected = 6,
    Deferred = 7,
}

public enum DefectCategory
{
    Structural = 1,
    Workmanship = 2,
    Services = 3,
    Waterproofing = 4,
    Finishes = 5,
    Equipment = 6,
}

// ── Deals (brokerage) ────────────────────────────────────────────────────────────────

public enum DealStatus
{
    Agreed = 1,
    Progressing = 2,
    /// <summary>Contracts exchanged / agreement registered. Effectively certain.</summary>
    Exchanged = 3,
    Completed = 4,
    FellThrough = 5,
    Cancelled = 6,
    OnHold = 7,
}

public enum DealPartyRole
{
    Buyer = 1,
    Seller = 2,
    BuyerSolicitor = 3,
    SellerSolicitor = 4,
    MortgageBroker = 5,
    Lender = 6,
    Surveyor = 7,
    Notary = 8,
    ListingAgent = 9,
    SellingAgent = 10,
    Registrar = 11,
}

public enum FallThroughCause
{
    BuyerWithdrew = 1,
    SellerWithdrew = 2,
    MortgageDeclined = 3,
    SurveyIssues = 4,
    ChainCollapse = 5,
    TitleProblem = 6,
    Gazumped = 7,
    ValuationShortfall = 8,
    Personal = 9,
    Other = 99,
}

// ── Commission ───────────────────────────────────────────────────────────────────────

public enum CommissionPlanKind
{
    FlatSplit = 1,
    /// <summary>Split improves as the agent crosses thresholds.</summary>
    GraduatedSplit = 2,
    /// <summary>100% to the agent once they have paid the house a capped amount in the year.</summary>
    CappedSplit = 3,
    FixedFeePerDeal = 4,
    SalaryPlusBonus = 5,
    /// <summary>Developer-side: slab on booking value or collection.</summary>
    SlabOnValue = 6,
}

public enum CommissionTrigger
{
    /// <summary>Earned the moment the booking is confirmed. Risky — cancellations claw back.</summary>
    OnBooking = 1,
    /// <summary>Earned in proportion to what the customer has actually paid. The safe default.</summary>
    OnCollection = 2,
    OnAgreementSigned = 3,
    OnCompletion = 4,
    OnPossession = 5,
}

public enum CommissionStatus
{
    Accrued = 1,
    Approved = 2,
    PartiallyPaid = 3,
    Paid = 4,
    /// <summary>Reversed because the booking cancelled or the customer defaulted.</summary>
    ClawedBack = 5,
    Disputed = 6,
    Cancelled = 7,
}

public enum DeductionKind
{
    FranchiseRoyalty = 1,
    BrokerageRetention = 2,
    ReferralFee = 3,
    MentorOverride = 4,
    TeamLeadOverride = 5,
    DeskFee = 6,
    TransactionFee = 7,
    PostCapFee = 8,
    Withholding = 9,
    AdvanceRecovery = 10,
    Other = 99,
}

// ── Channel partners ─────────────────────────────────────────────────────────────────

public enum PartnerStatus
{
    Applied = 1,
    UnderReview = 2,
    Active = 3,
    Suspended = 4,
    Blacklisted = 5,
    Expired = 6,
}

public enum LeadRegistrationStatus
{
    Registered = 1,
    /// <summary>Someone else already had this lead. Rejected immediately, not at payout time.</summary>
    DuplicateRejected = 2,
    Expired = 3,
    Converted = 4,
    Withdrawn = 5,
}

// ── Tenancy & leasing ────────────────────────────────────────────────────────────────

public enum TenancyKind
{
    AssuredShorthold = 1,
    CommercialLease = 2,
    Licence = 3,
    MonthToMonth = 4,
    FixedTerm = 5,
    Periodic = 6,
    Sublease = 7,
    LeaseToOwn = 8,
    ShortStay = 9,
    /// <summary>A day-to-year pitch in a mall concourse.</summary>
    KioskLicence = 10,
}

public enum TenancyStatus
{
    Application = 1,
    Referencing = 2,
    Offered = 3,
    AgreementPending = 4,
    Active = 5,
    NoticeGiven = 6,
    Expiring = 7,
    Renewed = 8,
    Ended = 9,
    Terminated = 10,
    Abandoned = 11,
    InEviction = 12,
}

public enum RentFrequency
{
    Weekly = 1,
    Fortnightly = 2,
    Monthly = 3,
    Quarterly = 4,
    HalfYearly = 5,
    Yearly = 6,
}

public enum EscalationKind
{
    None = 1,
    FixedPercent = 2,
    /// <summary>Tied to a published index, usually with a floor and a cap.</summary>
    IndexLinked = 3,
    SteppedSchedule = 4,
    OpenMarketReview = 5,
    FixedAmount = 6,
}

public enum LeaseOptionKind
{
    Break = 1,
    Renewal = 2,
    Expansion = 3,
    Contraction = 4,
    RightOfFirstRefusal = 5,
    Purchase = 6,
}

public enum DepositScheme
{
    /// <summary>Held by us in the client account — no statutory scheme in this market.</summary>
    HeldInClientAccount = 1,
    /// <summary>Handed to a custodial scheme for the term.</summary>
    Custodial = 2,
    /// <summary>Held by us but insured with a scheme.</summary>
    Insured = 3,
    HeldByLandlord = 4,
    NoDeposit = 5,
}

public enum ReferencingOutcome
{
    Pending = 1,
    Pass = 2,
    PassWithGuarantor = 3,
    PassWithConditions = 4,
    Fail = 5,
    Withdrawn = 6,
}

public enum ReferencingCheckKind
{
    Identity = 1,
    RightToRent = 2,
    Employment = 3,
    Income = 4,
    Credit = 5,
    PreviousLandlord = 6,
    Guarantor = 7,
    CompanyCheck = 8,
    Bank = 9,
}

public enum InspectionKind
{
    MoveIn = 1,
    MoveOut = 2,
    Periodic = 3,
    Interim = 4,
    PreHandover = 5,
    CommonArea = 6,
    Safety = 7,
}

public enum ConditionGrade
{
    New = 1,
    Good = 2,
    Fair = 3,
    Poor = 4,
    Damaged = 5,
    Missing = 6,
}

public enum ComplianceCertificateKind
{
    GasSafety = 1,
    Electrical = 2,
    EnergyPerformance = 3,
    FireRiskAssessment = 4,
    Legionella = 5,
    PortableAppliance = 6,
    AlarmTest = 7,
    LiftInspection = 8,
    LicenceToRent = 9,
    BuildingInsurance = 10,
    StructuralSafety = 11,
    Other = 99,
}

// ── Rent roll, service charge & recoveries ───────────────────────────────────────────

public enum ApportionmentBasis
{
    ProRataByArea = 1,
    FixedPercent = 2,
    EqualShare = 3,
    ByUnitCount = 4,
    ByMeteredConsumption = 5,
    BespokeSchedule = 6,
}

public enum ServiceChargeHead
{
    Security = 1,
    Cleaning = 2,
    Landscaping = 3,
    Lifts = 4,
    Hvac = 5,
    CommonElectricity = 6,
    Water = 7,
    GeneratorFuel = 8,
    Insurance = 9,
    ManagementFee = 10,
    Repairs = 11,
    WasteDisposal = 12,
    PestControl = 13,
    SinkingFund = 14,
    MarketingFund = 15,
    Other = 99,
}

public enum ReconciliationOutcome
{
    /// <summary>Actual exceeded what was billed on account — the tenant owes the difference.</summary>
    BalancingCharge = 1,
    /// <summary>Billed more than was spent — the tenant is credited.</summary>
    BalancingCredit = 2,
    Nil = 3,
}

public enum TurnoverRentBasis
{
    /// <summary>Breakpoint derived by dividing base rent by the percentage.</summary>
    NaturalBreakpoint = 1,
    /// <summary>A negotiated sales figure, unrelated to the base rent.</summary>
    ArtificialBreakpoint = 2,
    /// <summary>Percentage of every rupee of sales, with no base rent at all.</summary>
    FromFirstUnit = 3,
    /// <summary>Percentage varies by sales band.</summary>
    SlabByBand = 4,
}

public enum ManagementService
{
    /// <summary>Find the tenant and hand over. No ongoing management.</summary>
    LetOnly = 1,
    RentCollection = 2,
    FullManagement = 3,
    /// <summary>We own it. No landlord, no management fee.</summary>
    OwnPortfolio = 4,
}

// ── Client money ─────────────────────────────────────────────────────────────────────

public enum ClientAccountKind
{
    LandlordFunds = 1,
    TenantDeposit = 2,
    BuyerDeposit = 3,
    ServiceChargeFund = 4,
    SinkingFund = 5,
    SocietyFund = 6,
}

public enum ClientMoneyExceptionKind
{
    /// <summary>A client balance has gone negative — a regulatory breach, not a rounding issue.</summary>
    OverdrawnClientBalance = 1,
    UnreconciledDifference = 2,
    StaleUnallocatedReceipt = 3,
    MissingBankStatement = 4,
    ReconciliationOverdue = 5,
}

// ── Society & community ──────────────────────────────────────────────────────────────

public enum MaintenanceBasis
{
    FlatRatePerUnit = 1,
    PerAreaUnit = 2,
    SlabBySize = 3,
    ByUnitType = 4,
    ByUsage = 5,
    OneTime = 6,
    AdHoc = 7,
}

public enum ResidentKind
{
    Owner = 1,
    Tenant = 2,
    FamilyMember = 3,
    /// <summary>Lives there with the owner's permission but holds no interest.</summary>
    Occupant = 4,
    DomesticStaff = 5,
}

public enum VisitorKind
{
    Guest = 1,
    Delivery = 2,
    Cab = 3,
    ServiceProvider = 4,
    Contractor = 5,
    DomesticStaff = 6,
    Vendor = 7,
    Emergency = 8,
}

public enum GateEntryStatus
{
    Expected = 1,
    AwaitingApproval = 2,
    Approved = 3,
    Denied = 4,
    CheckedIn = 5,
    CheckedOut = 6,
    Expired = 7,
}

public enum AmenityBookingStatus
{
    Requested = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4,
    Completed = 5,
    NoShow = 6,
}

public enum ComplaintCategory
{
    Plumbing = 1,
    Electrical = 2,
    Lift = 3,
    Security = 4,
    Cleanliness = 5,
    Parking = 6,
    Noise = 7,
    Water = 8,
    CommonArea = 9,
    Billing = 10,
    Pest = 11,
    Internet = 12,
    Other = 99,
}

public enum TicketStatus
{
    Open = 1,
    Acknowledged = 2,
    Assigned = 3,
    InProgress = 4,
    OnHold = 5,
    Resolved = 6,
    Closed = 7,
    Reopened = 8,
    /// <summary>Past its SLA and escalated to the next level.</summary>
    Escalated = 9,
}

public enum TicketPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    /// <summary>A lift with somebody in it, a burst main, a fire panel. Minutes, not days.</summary>
    Emergency = 4,
}

public enum BuildingApplicationStatus
{
    Submitted = 1,
    UnderScrutiny = 2,
    QueryRaised = 3,
    Approved = 4,
    ApprovedWithConditions = 5,
    Rejected = 6,
    Expired = 7,
    /// <summary>Building without approval, or beyond it. Stop-work served.</summary>
    ViolationNoticed = 8,
}

// ── Facilities & assets ──────────────────────────────────────────────────────────────

public enum WorkOrderSource
{
    TenantRequest = 1,
    ResidentComplaint = 2,
    OwnerRequest = 3,
    Inspection = 4,
    Snag = 5,
    DefectClaim = 6,
    PlannedMaintenance = 7,
    MeterAlarm = 8,
    Internal = 9,
}

public enum WorkOrderStatus
{
    Raised = 1,
    AwaitingAuthorisation = 2,
    Authorised = 3,
    Assigned = 4,
    AppointmentSet = 5,
    InProgress = 6,
    AwaitingParts = 7,
    Completed = 8,
    /// <summary>Signed off by the occupier. Only this state allows invoicing.</summary>
    SignedOff = 9,
    Cancelled = 10,
    Rejected = 11,
}

public enum CostBearer
{
    Landlord = 1,
    Tenant = 2,
    Society = 3,
    /// <summary>A defect inside the liability period. Ours, not the customer's.</summary>
    Developer = 4,
    Contractor = 5,
    Insurance = 6,
    Shared = 7,
}

public enum AssetKind
{
    Lift = 1,
    Generator = 2,
    Transformer = 3,
    WaterPump = 4,
    Chiller = 5,
    FirePanel = 6,
    Cctv = 7,
    SewageTreatment = 8,
    SolarArray = 9,
    Gate = 10,
    Boiler = 11,
    Hvac = 12,
    Other = 99,
}

public enum MeterKind
{
    Electricity = 1,
    Water = 2,
    Gas = 3,
    Heat = 4,
    /// <summary>Reads the whole building; unit sub-meters hang under it.</summary>
    BulkSupply = 5,
}

public enum ReadingSource
{
    Manual = 1,
    PhotoVerified = 2,
    AutomaticMeterReading = 3,
    Estimated = 4,
    CustomerSubmitted = 5,
}

// ── Construction & contracting ───────────────────────────────────────────────────────

public enum WbsKind
{
    Project = 1,
    Package = 2,
    Activity = 3,
    Task = 4,
}

public enum BoqLineKind
{
    Measured = 1,
    /// <summary>An allowance for work not yet designed. Adjusted when it is.</summary>
    ProvisionalSum = 2,
    /// <summary>An allowance for goods to be selected later — the marble the client picks.</summary>
    PrimeCostSum = 3,
    DayWork = 4,
    Contingency = 5,
    Preliminaries = 6,
}

public enum ProgressMethod
{
    ByQuantity = 1,
    ByCost = 2,
    ByMilestone = 3,
    /// <summary>The engineer's judgement, recorded with a reason.</summary>
    PhysicalAssessment = 4,
}

public enum ContractKind
{
    LumpSum = 1,
    CostPlusPercent = 2,
    CostPlusFixedFee = 3,
    /// <summary>The dominant South Asian turnkey shape: a rate per covered square foot.</summary>
    PerAreaUnitRate = 4,
    GuaranteedMaximumPrice = 5,
    /// <summary>We are the client's agent, paid a fee to manage others.</summary>
    ManagementContract = 6,
    Measured = 7,
}

public enum SpecificationGrade
{
    Economy = 1,
    Standard = 2,
    Premium = 3,
    Luxury = 4,
    Bespoke = 5,
}

public enum CertificateStatus
{
    Draft = 1,
    SubmittedForCertification = 2,
    Certified = 3,
    Approved = 4,
    Paid = 5,
    Rejected = 6,
    Cancelled = 7,
}

public enum VariationOrigin
{
    ClientRequest = 1,
    DesignChange = 2,
    SiteCondition = 3,
    StatutoryRequirement = 4,
    ErrorCorrection = 5,
    ValueEngineering = 6,
}

public enum VariationStatus
{
    Proposed = 1,
    Quoted = 2,
    /// <summary>Priced and accepted. Only now does the contract value move.</summary>
    Approved = 3,
    Instructed = 4,
    Measured = 5,
    Rejected = 6,
    Withdrawn = 7,
}

public enum TenderStatus
{
    Draft = 1,
    Invited = 2,
    BidsOpen = 3,
    UnderEvaluation = 4,
    Negotiating = 5,
    Awarded = 6,
    Cancelled = 7,
}

public enum SubcontractStatus
{
    Draft = 1,
    Awarded = 2,
    Active = 3,
    Suspended = 4,
    Completed = 5,
    Terminated = 6,
    /// <summary>Work done, retention still held. Not finished until it is released.</summary>
    InDefectsPeriod = 7,
    Closed = 8,
}

public enum ContraChargeKind
{
    MaterialIssued = 1,
    PlantHire = 2,
    Utilities = 3,
    Damages = 4,
    Rework = 5,
    Cleaning = 6,
    SafetyPenalty = 7,
    DelayPenalty = 8,
    Other = 99,
}

public enum RetentionMovement
{
    Held = 1,
    ReleasedAtCompletion = 2,
    ReleasedAtDefectsEnd = 3,
    ForfeitedForDefects = 4,
    ReleasedEarlyAgainstBond = 5,
}

public enum SafetySeverity
{
    NearMiss = 1,
    FirstAid = 2,
    MedicalTreatment = 3,
    LostTime = 4,
    Major = 5,
    Fatal = 6,
}

// ── JV, investors & project finance ──────────────────────────────────────────────────

public enum JvShareBasis
{
    /// <summary>A percentage of collections or net sales revenue.</summary>
    RevenueShare = 1,
    /// <summary>Specific units handed to the landowner, excluded from our saleable stock.</summary>
    BuiltUpAreaShare = 2,
    /// <summary>A percentage of saleable area, allocated to units at an agreed stage.</summary>
    SaleableAreaShare = 3,
    /// <summary>We manage, they own. A fee on cost or on revenue.</summary>
    DevelopmentFee = 4,
    ProfitShare = 5,
}

public enum ProjectAccountKind
{
    /// <summary>The regulated account. Withdrawals require certified progress.</summary>
    Escrow = 1,
    /// <summary>The unrestricted balance of collections.</summary>
    Free = 2,
    Operating = 3,
    LoanDisbursement = 4,
}

public enum EscrowMovementKind
{
    CollectionCredit = 1,
    Withdrawal = 2,
    InterestCredit = 3,
    BankCharge = 4,
    TransferToFree = 5,
    Adjustment = 6,
}

public enum GuaranteeKind
{
    Performance = 1,
    AdvancePayment = 2,
    Retention = 3,
    Mobilisation = 4,
    Bid = 5,
    Maintenance = 6,
}

public enum LoanStatus
{
    Applied = 1,
    Sanctioned = 2,
    PartiallyDrawn = 3,
    FullyDrawn = 4,
    Repaying = 5,
    Closed = 6,
    Defaulted = 7,
}

// ── Revenue & project accounting ─────────────────────────────────────────────────────

/// <summary>
/// The IFRIC 15 determination, made per contract and recorded with its reasoning because the
/// auditor will ask. It changes the P&amp;L completely.
/// </summary>
public enum RecognitionBasis
{
    /// <summary>Sale of a completed product. Everything collected is a liability until handover.</summary>
    PointInTime = 1,
    /// <summary>Sale of a construction service. Recognised as the building goes up.</summary>
    OverTime = 2,
}

public enum CostAllocationBasis
{
    ByArea = 1,
    ByValue = 2,
    ByUnitCount = 3,
    Direct = 4,
}

public enum WithholdingKind
{
    OnCommission = 1,
    OnRent = 2,
    OnContractorPayment = 3,
    OnProfessionalFee = 4,
    OnPropertySale = 5,
}

// ── Compliance & documents ───────────────────────────────────────────────────────────

public enum ApprovalKind
{
    LayoutPlan = 1,
    BuildingPlan = 2,
    EnvironmentalClearance = 3,
    FireNoc = 4,
    LiftNoc = 5,
    HeightClearance = 6,
    WaterAndSewerage = 7,
    ElectricityLoad = 8,
    GasNoc = 9,
    RoadCut = 10,
    TreeCutting = 11,
    LabourRegistration = 12,
    CommencementCertificate = 13,
    CompletionCertificate = 14,
    OccupancyCertificate = 15,
    ProjectRegistration = 16,
    LandUseConversion = 17,
    Other = 99,
}

public enum ApprovalState
{
    NotStarted = 1,
    Preparing = 2,
    Submitted = 3,
    UnderReview = 4,
    QueryRaised = 5,
    Granted = 6,
    GrantedWithConditions = 7,
    Rejected = 8,
    Expired = 9,
    RenewalDue = 10,
}

/// <summary>The NOCs we issue outward, as opposed to the approvals we seek inward.</summary>
public enum NocKind
{
    Transfer = 1,
    Construction = 2,
    Mortgage = 3,
    UtilityConnection = 4,
    Possession = 5,
    Demolition = 6,
    Renovation = 7,
    Sale = 8,
    Occupancy = 9,
}

public enum NocStatus
{
    Requested = 1,
    DuesCheckPending = 2,
    Approved = 3,
    Issued = 4,
    Expired = 5,
    Revoked = 6,
    Rejected = 7,
}

public enum DocumentState
{
    Required = 1,
    Received = 2,
    Verified = 3,
    Rejected = 4,
    Expired = 5,
    Waived = 6,
}

public enum SignatureMethod
{
    Electronic = 1,
    WetSignature = 2,
    /// <summary>Still the legal norm across much of South Asia.</summary>
    ThumbImpression = 3,
    DigitalCertificate = 4,
}

public enum SignatureState
{
    Pending = 1,
    Sent = 2,
    Viewed = 3,
    Signed = 4,
    Declined = 5,
    Expired = 6,
    Cancelled = 7,
}

public enum LegalCaseStatus
{
    Filed = 1,
    Pending = 2,
    Hearing = 3,
    Reserved = 4,
    Decided = 5,
    Appealed = 6,
    Settled = 7,
    Withdrawn = 8,
    Dismissed = 9,
}

public enum PhysicalFileState
{
    InRecordRoom = 1,
    IssuedToStaff = 2,
    WithLegal = 3,
    WithAuditor = 4,
    /// <summary>Handed to the owner at possession. No longer ours to produce.</summary>
    ReleasedToOwner = 5,
    Missing = 6,
    Destroyed = 7,
    Archived = 8,
}

// ── Platform ─────────────────────────────────────────────────────────────────────────

public enum ApprovalOutcome
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    /// <summary>Sent back for correction rather than refused outright.</summary>
    Returned = 4,
    Escalated = 5,
    Withdrawn = 6,
    /// <summary>Auto-approved because it fell under the threshold.</summary>
    AutoApproved = 7,
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
}

public enum ImportEntityKind
{
    Properties = 1,
    Units = 2,
    PriceList = 3,
    Parties = 4,
    Bookings = 5,
    PaymentHistory = 6,
    Tenancies = 7,
    Residents = 8,
    Meters = 9,
    Listings = 10,
    ChannelPartners = 11,
    BoqLines = 12,
}

public enum ImportBatchStatus
{
    Uploaded = 1,
    Validating = 2,
    /// <summary>Validated and previewed, but nothing written yet. The user still has to commit.</summary>
    DryRunComplete = 3,
    Importing = 4,
    Completed = 5,
    CompletedWithErrors = 6,
    Failed = 7,
    Cancelled = 8,
}

public enum PortalAudience
{
    Customer = 1,
    Tenant = 2,
    Owner = 3,
    Partner = 4,
    Resident = 5,
}
