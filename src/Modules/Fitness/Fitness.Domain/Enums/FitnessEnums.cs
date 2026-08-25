namespace Fitness.Domain.Enums;

// ── Club ─────────────────────────────────────────────────────────────────────

/// <summary>
/// What kind of club this is. Drives which screens are worth showing: a PT studio has no class
/// timetable worth speaking of, and a box lives on the programming screen a big-box gym never
/// opens.
/// </summary>
public enum ClubType
{
    Gym = 1,
    BoutiqueStudio = 2,
    CrossFitBox = 3,
    MartialArtsAcademy = 4,
    PersonalTrainingStudio = 5,
    LeisureCentre = 6,
    HotelClub = 7,
    YogaPilatesStudio = 8,
    SwimSchool = 9,
    ClimbingGym = 10,
}

/// <summary>A bookable or access-controlled part of a club.</summary>
public enum AreaKind
{
    GymFloor = 1,
    Studio = 2,
    Pool = 3,
    Spa = 4,
    Sauna = 5,
    Court = 6,
    FunctionalZone = 7,
    Creche = 8,
    ChangingRoom = 9,
    Reception = 10,
    Office = 11,
    Cafe = 12,
    Parking = 13,
}

// ── Member ───────────────────────────────────────────────────────────────────

/// <summary>
/// Where a person is in their relationship with the club.
///
/// <see cref="Frozen"/> and <see cref="Suspended"/> are deliberately different states: a freeze
/// is something the member asked for and the club agreed to, a suspension is something the club
/// imposed. Collapsing them would hide the churn signal, because a suspended member is much
/// closer to leaving than a frozen one.
/// </summary>
public enum MemberStatus
{
    Lead = 1,
    Trial = 2,
    Active = 3,
    Frozen = 4,
    PastDue = 5,
    Suspended = 6,
    Cancelled = 7,
    Expired = 8,
    /// <summary>Cancelled and later rejoined — tracked separately because win-back is a different funnel.</summary>
    WonBack = 9,
}

public enum Gender
{
    Unspecified = 0,
    Female = 1,
    Male = 2,
    Other = 3,
    PreferNotToSay = 4,
}

/// <summary>What kind of thing a note or timeline entry on a member record is.</summary>
public enum InteractionKind
{
    Note = 1,
    Call = 2,
    Email = 3,
    Sms = 4,
    WhatsApp = 5,
    InPerson = 6,
    Complaint = 7,
    Compliment = 8,
    SystemEvent = 9,
}

/// <summary>Severity of a flag raised on a member record.</summary>
public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Blocking = 3,
}

/// <summary>Why a member alert exists. Drives its icon, its colour, and whether it blocks entry.</summary>
public enum MemberAlertKind
{
    OutstandingBalance = 1,
    WaiverMissing = 2,
    WaiverExpired = 3,
    MedicalClearanceRequired = 4,
    CardExpiring = 5,
    Birthday = 6,
    FirstVisit = 7,
    VisitMilestone = 8,
    NoShowStreak = 9,
    Banned = 10,
    ContractEnding = 11,
    CreditsExhausted = 12,
    Custom = 13,
}

/// <summary>How a member is related to the household they belong to.</summary>
public enum HouseholdRole
{
    Primary = 1,
    Partner = 2,
    Child = 3,
    Dependent = 4,
    Other = 5,
}

// ── Catalogue ────────────────────────────────────────────────────────────────

/// <summary>
/// The six shapes of thing a club sells. These are genuinely different — a recurring membership
/// bills forever, a pack is consumed, a pass expires — and modelling them as one "product" with
/// flags is how these systems end up unable to answer "what do we owe in unused sessions?".
/// </summary>
public enum PlanKind
{
    /// <summary>Bills every period until someone cancels it.</summary>
    RecurringMembership = 1,
    /// <summary>Fixed length with a defined end; paid up front or in instalments.</summary>
    TermMembership = 2,
    /// <summary>N credits, optionally expiring.</summary>
    SessionPack = 3,
    /// <summary>Day, week, holiday, guest or trial pass.</summary>
    TimePass = 4,
    /// <summary>A single deliverable: a PT session, an assessment, a locker term.</summary>
    Service = 5,
    /// <summary>A stocked item owned by Inventory and sold through the pro shop.</summary>
    RetailProduct = 6,
}

/// <summary>How often a recurring plan bills.</summary>
public enum BillingPeriod
{
    Weekly = 1,
    Fortnightly = 2,
    FourWeekly = 3,
    Monthly = 4,
    Quarterly = 5,
    SemiAnnual = 6,
    Annual = 7,
    /// <summary>Charged once — packs, passes, services and products.</summary>
    OneOff = 8,
}

/// <summary>When in the period the charge is raised.</summary>
public enum BillingAnchor
{
    /// <summary>Bills on the same day of the month the member joined.</summary>
    JoinAnniversary = 1,
    /// <summary>Bills on a fixed calendar day for everyone, which is what large clubs run.</summary>
    FixedDayOfMonth = 2,
}

/// <summary>How a part-period is charged when a member joins, upgrades, freezes or leaves mid-cycle.</summary>
public enum ProrationRule
{
    /// <summary>Charge for the days actually served.</summary>
    Daily = 1,
    /// <summary>Charge a whole period regardless.</summary>
    FullPeriod = 2,
    /// <summary>Charge nothing now; start billing at the next period.</summary>
    None = 3,
    /// <summary>Roll the part-period into the first full period's invoice.</summary>
    AddToFirstPeriod = 4,
}

/// <summary>What a member may do with their entitlement, expressed as a limit shape.</summary>
public enum EntitlementKind
{
    ClubAccess = 1,
    AreaAccess = 2,
    ClassBooking = 3,
    PersonalTraining = 4,
    ResourceBooking = 5,
    GuestPass = 6,
    Locker = 7,
    TowelService = 8,
    Creche = 9,
    Other = 10,
}

/// <summary>How an entitlement is capped.</summary>
public enum EntitlementLimit
{
    Unlimited = 1,
    PerDay = 2,
    PerWeek = 3,
    PerMonth = 4,
    /// <summary>A total across the life of the agreement — the "12 PT sessions included" case.</summary>
    PerAgreement = 5,
    NotIncluded = 6,
}

/// <summary>How a promotion changes the price.</summary>
public enum DiscountKind
{
    Percentage = 1,
    FixedAmount = 2,
    /// <summary>An explicit price that replaces the plan price for N periods.</summary>
    OverridePrice = 3,
    /// <summary>N free periods before normal billing starts.</summary>
    FreePeriods = 4,
    WaiveJoiningFee = 5,
}

// ── Agreement ────────────────────────────────────────────────────────────────

public enum AgreementStatus
{
    Draft = 1,
    /// <summary>Signed but the start date has not arrived.</summary>
    Pending = 2,
    Active = 3,
    Frozen = 4,
    /// <summary>Cancellation requested; still running until the effective date.</summary>
    NoticeGiven = 5,
    Cancelled = 6,
    Expired = 7,
    /// <summary>Ended and replaced by an upgrade/downgrade agreement.</summary>
    Superseded = 8,
}

public enum AgreementChangeKind
{
    Upgrade = 1,
    Downgrade = 2,
    AddOn = 3,
    PriceChange = 4,
    HolderTransfer = 5,
    TermExtension = 6,
    PaymentMethodChange = 7,
    Other = 8,
}

public enum FreezeReason
{
    Travel = 1,
    Injury = 2,
    Medical = 3,
    Financial = 4,
    Pregnancy = 5,
    Work = 6,
    Seasonal = 7,
    Other = 8,
}

public enum SuspensionReason
{
    UnpaidBalance = 1,
    Conduct = 2,
    MissingWaiver = 3,
    MissingMedicalClearance = 4,
    ExpiredDocument = 5,
    Administrative = 6,
    Other = 7,
}

/// <summary>Why someone left. The single most valuable field in the whole app.</summary>
public enum LeaveReason
{
    TooExpensive = 1,
    MovedAway = 2,
    NotUsingIt = 3,
    Injury = 4,
    Medical = 5,
    Pregnancy = 6,
    ChangedJob = 7,
    UnhappyWithFacility = 8,
    UnhappyWithStaff = 9,
    TooBusy = 10,
    WentToCompetitor = 11,
    ClassesNotSuitable = 12,
    TemporaryBreak = 13,
    Deceased = 14,
    Other = 15,
}

/// <summary>What was offered to keep a leaving member, and whether it worked.</summary>
public enum SaveOfferKind
{
    FreezeInstead = 1,
    DowngradeInstead = 2,
    FreeMonth = 3,
    DiscountedPeriods = 4,
    FreePtSession = 5,
    PlanChange = 6,
    Other = 7,
}

// ── Money ────────────────────────────────────────────────────────────────────

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Overdue = 5,
    /// <summary>Collection failed and the dunning ladder is running.</summary>
    InDunning = 6,
    WrittenOff = 7,
    Cancelled = 8,
    Refunded = 9,
}

/// <summary>What a line on an invoice is for. Drives revenue reporting and GL mapping.</summary>
public enum ChargeKind
{
    MembershipDues = 1,
    Instalment = 2,
    JoiningFee = 3,
    AdminFee = 4,
    AnnualMaintenanceFee = 5,
    ProRata = 6,
    FreezeFee = 7,
    LateFee = 8,
    NoShowFee = 9,
    LateCancelFee = 10,
    SessionPackage = 11,
    PersonalTraining = 12,
    ClassDropIn = 13,
    DayPass = 14,
    GuestFee = 15,
    LockerRental = 16,
    ResourceBooking = 17,
    Retail = 18,
    CrossClubVisit = 19,
    EarlyTerminationFee = 20,
    CardReplacement = 21,
    Course = 22,
    Adjustment = 23,
    Other = 24,
}

/// <summary>
/// How money arrived. Recorded, never processed — see the PCI note in the module README: no PAN,
/// no CVV, no track data is accepted or stored anywhere in this model.
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    Card = 2,
    DirectDebit = 3,
    BankTransfer = 4,
    StandingOrder = 5,
    Wallet = 6,
    GiftCard = 7,
    MemberCredit = 8,
    Cheque = 9,
    CorporateAccount = 10,
    ThirdPartyPayer = 11,
    Marketplace = 12,
    Other = 13,
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5,
    ChargedBack = 6,
    Cancelled = 7,
}

/// <summary>Why a collection attempt failed. Drives which dunning ladder step is appropriate.</summary>
public enum PaymentFailureReason
{
    InsufficientFunds = 1,
    CardExpired = 2,
    CardDeclined = 3,
    MandateCancelled = 4,
    AccountClosed = 5,
    Disputed = 6,
    TechnicalError = 7,
    NoPaymentMethod = 8,
    Other = 9,
}

/// <summary>What a dunning step actually does when it fires.</summary>
public enum DunningAction
{
    Retry = 1,
    SendEmail = 2,
    SendSms = 3,
    SendPush = 4,
    /// <summary>Ask the member to update a card that is failing or about to.</summary>
    RequestCardUpdate = 5,
    /// <summary>Raise a task for a human to phone them.</summary>
    CreateCallTask = 6,
    AddLateFee = 7,
    SuspendAccess = 8,
    CancelAgreement = 9,
    WriteOff = 10,
    /// <summary>Hand to an external collections agency; recorded, not performed.</summary>
    ReferToCollections = 11,
}

public enum DunningCaseStatus
{
    Open = 1,
    Recovered = 2,
    Suspended = 3,
    WrittenOff = 4,
    Cancelled = 5,
    Escalated = 6,
}

public enum BillingRunStatus
{
    Draft = 1,
    Previewing = 2,
    Running = 3,
    Completed = 4,
    CompletedWithErrors = 5,
    Failed = 6,
    Cancelled = 7,
}

/// <summary>Direction of a member ledger movement, so a running balance never guesses.</summary>
public enum LedgerEntryKind
{
    Charge = 1,
    Payment = 2,
    Refund = 3,
    CreditNote = 4,
    WriteOff = 5,
    Adjustment = 6,
    CreditApplied = 7,
    CreditIssued = 8,
}

/// <summary>Why deferred revenue was released — time passing, or a credit being consumed.</summary>
public enum RevenueRecognitionBasis
{
    /// <summary>Earned evenly across the service period.</summary>
    StraightLine = 1,
    /// <summary>Earned when a session or class credit is redeemed.</summary>
    OnConsumption = 2,
    /// <summary>Earned immediately — a joining fee, a retail sale.</summary>
    Immediate = 3,
}

// ── Access & attendance ──────────────────────────────────────────────────────

/// <summary>How a person identified themselves at the door or the desk.</summary>
public enum CredentialType
{
    RfidFob = 1,
    BarcodeKeyTag = 2,
    MembershipQr = 3,
    MobileCredential = 4,
    Pin = 5,
    FacialRecognition = 6,
    Fingerprint = 7,
    /// <summary>Staff looked them up and let them in by hand.</summary>
    ManualLookup = 8,
    TemporaryPass = 9,
}

public enum CredentialStatus
{
    Active = 1,
    Lost = 2,
    Stolen = 3,
    Replaced = 4,
    Deactivated = 5,
    Expired = 6,
}

/// <summary>Which way a reader faces. In/out is what makes anti-passback and occupancy possible.</summary>
public enum ReaderDirection
{
    In = 1,
    Out = 2,
    /// <summary>A single reader used for both, where the hardware cannot tell.</summary>
    Bidirectional = 3,
}

public enum AccessDecision
{
    Granted = 1,
    Denied = 2,
    /// <summary>Let through, but something was wrong and staff should know.</summary>
    GrantedWithWarning = 3,
    /// <summary>Staff opened the door by hand, overriding the decision.</summary>
    ManualOverride = 4,
}

/// <summary>
/// Why entry was refused. Every one of these maps to a sentence a member can act on — "your
/// membership is frozen until 3 March" beats "access denied" every time.
/// </summary>
public enum AccessDenialReason
{
    None = 0,
    NoActiveMembership = 1,
    MembershipFrozen = 2,
    MembershipSuspended = 3,
    MembershipExpired = 4,
    MembershipCancelled = 5,
    OutstandingBalance = 6,
    WaiverNotSigned = 7,
    MedicalClearanceRequired = 8,
    OutsideAccessHours = 9,
    ClubClosed = 10,
    ClubNotPermitted = 11,
    AreaNotPermitted = 12,
    VisitAllowanceExhausted = 13,
    AntiPassback = 14,
    OccupancyFull = 15,
    Banned = 16,
    CredentialInactive = 17,
    CredentialUnknown = 18,
    NoClassBooked = 19,
    UnderAge = 20,
    GuardianRequired = 21,
}

/// <summary>How strictly a credential must exit before it may enter again.</summary>
public enum AntiPassbackMode
{
    Off = 1,
    /// <summary>Refuse a second entry with no exit in between.</summary>
    Hard = 2,
    /// <summary>Allow it, but flag it for the manager's report.</summary>
    Soft = 3,
    /// <summary>Refuse only within a window (the classic "not twice in ten minutes").</summary>
    Timed = 4,
}

/// <summary>What a controller does when it cannot reach the server.</summary>
public enum OfflineAccessPolicy
{
    /// <summary>Let anyone with a known active credential in. The usual choice for a staffed club.</summary>
    AllowKnownActive = 1,
    /// <summary>Refuse everything. Correct for high-security or unstaffed sites.</summary>
    DenyAll = 2,
    /// <summary>Honour the last decision cached for that credential.</summary>
    LastKnownDecision = 3,
    /// <summary>Let everyone in and sort it out later — a fire-safety or grand-opening setting.</summary>
    AllowAll = 4,
}

public enum VisitKind
{
    Member = 1,
    Guest = 2,
    DayPass = 3,
    Trial = 4,
    Staff = 5,
    Contractor = 6,
    Marketplace = 7,
}

// ── Classes & booking ────────────────────────────────────────────────────────

public enum ClassOccurrenceStatus
{
    Scheduled = 1,
    /// <summary>Published and open for booking.</summary>
    Open = 2,
    Full = 3,
    /// <summary>Booking closed but the class has not started.</summary>
    Locked = 4,
    InProgress = 5,
    Completed = 6,
    Cancelled = 7,
}

public enum BookingStatus
{
    Booked = 1,
    Waitlisted = 2,
    CheckedIn = 3,
    Attended = 4,
    NoShow = 5,
    /// <summary>Cancelled inside the free window — nothing charged, credit returned.</summary>
    Cancelled = 6,
    /// <summary>Cancelled after the window — credit forfeited or a fee raised.</summary>
    LateCancelled = 7,
    /// <summary>The club cancelled the class; credits are always returned.</summary>
    ClassCancelled = 8,
}

/// <summary>How a booking was paid for, which decides what happens when it is cancelled late.</summary>
public enum BookingPaymentKind
{
    Entitlement = 1,
    PackCredit = 2,
    DropInPayment = 3,
    CoursePlace = 4,
    Marketplace = 5,
    Complimentary = 6,
}

/// <summary>What the club does when someone cancels too late or does not turn up.</summary>
public enum PolicyOutcome
{
    Nothing = 1,
    ForfeitCredit = 2,
    ChargeFee = 3,
    ForfeitCreditAndFee = 4,
    /// <summary>Counts against a strike threshold that eventually suspends booking rights.</summary>
    Strike = 5,
}

public enum BookingChannel
{
    FrontDesk = 1,
    MemberApp = 2,
    WebPortal = 3,
    Kiosk = 4,
    Phone = 5,
    Marketplace = 6,
    Instructor = 7,
}

// ── Appointments & PT ────────────────────────────────────────────────────────

public enum AppointmentStatus
{
    Requested = 1,
    Confirmed = 2,
    CheckedIn = 3,
    Completed = 4,
    NoShow = 5,
    Cancelled = 6,
    LateCancelled = 7,
    Rescheduled = 8,
}

public enum AppointmentKind
{
    PersonalTraining = 1,
    Assessment = 2,
    Consultation = 3,
    Induction = 4,
    SemiPrivate = 5,
    SmallGroup = 6,
    Physiotherapy = 7,
    Massage = 8,
    Nutrition = 9,
    SwimLesson = 10,
    Other = 11,
}

/// <summary>Why a session credit moved. The ledger that stops "how many do I have left?" arguments.</summary>
public enum SessionCreditMovementKind
{
    Purchased = 1,
    Granted = 2,
    Consumed = 3,
    Refunded = 4,
    Expired = 5,
    Transferred = 6,
    /// <summary>Taken because the member did not turn up.</summary>
    ForfeitedNoShow = 7,
    Adjusted = 8,
}

// ── Training & performance ───────────────────────────────────────────────────

/// <summary>How a workout result is measured, which decides how a leaderboard sorts it.</summary>
public enum ScoreType
{
    ForTime = 1,
    RoundsAndReps = 2,
    Reps = 3,
    MaxLoad = 4,
    Distance = 5,
    Calories = 6,
    TimeUnderLoad = 7,
    PassFail = 8,
    Points = 9,
}

public enum WorkoutSectionKind
{
    WarmUp = 1,
    Strength = 2,
    Skill = 3,
    Metcon = 4,
    Accessory = 5,
    Conditioning = 6,
    CoolDown = 7,
    Emom = 8,
    Amrap = 9,
    Tabata = 10,
    Interval = 11,
    Note = 12,
}

public enum ExerciseCategory
{
    Barbell = 1,
    Dumbbell = 2,
    Kettlebell = 3,
    Bodyweight = 4,
    Machine = 5,
    Cable = 6,
    Cardio = 7,
    Gymnastics = 8,
    Olympic = 9,
    Mobility = 10,
    Plyometric = 11,
    Band = 12,
    Other = 13,
}

/// <summary>Heart-rate zone bands, the standard five used by every wearable platform.</summary>
public enum EffortZone
{
    Grey = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4,
    Red = 5,
}

public enum RankAwardStatus
{
    InProgress = 1,
    EligibleForGrading = 2,
    Graded = 3,
    Awarded = 4,
    Failed = 5,
}

// ── Assessments ──────────────────────────────────────────────────────────────

/// <summary>What kind of number a measure holds, which decides its input control and its chart.</summary>
public enum MeasureType
{
    Weight = 1,
    Length = 2,
    Percentage = 3,
    Count = 4,
    Duration = 5,
    Pressure = 6,
    Rate = 7,
    Score = 8,
    Text = 9,
    Boolean = 10,
}

/// <summary>Whether a bigger number is better, so progress arrows point the right way.</summary>
public enum MeasureDirection
{
    HigherIsBetter = 1,
    LowerIsBetter = 2,
    /// <summary>Neither — a raw measurement like height.</summary>
    Neutral = 3,
    /// <summary>Best inside a band, worse outside it in either direction — blood pressure, body fat.</summary>
    RangeIsBetter = 4,
}

public enum GoalStatus
{
    Active = 1,
    Achieved = 2,
    Missed = 3,
    Abandoned = 4,
    Paused = 5,
}

// ── Sales & CRM ──────────────────────────────────────────────────────────────

public enum LeadStatus
{
    New = 1,
    Contacted = 2,
    TourBooked = 3,
    Toured = 4,
    Trialling = 5,
    Negotiating = 6,
    Won = 7,
    Lost = 8,
    /// <summary>Not now, but worth another call later.</summary>
    Nurturing = 9,
}

public enum LeadSourceKind
{
    WalkIn = 1,
    WebForm = 2,
    Phone = 3,
    Referral = 4,
    SocialMedia = 5,
    PaidAds = 6,
    Event = 7,
    Corporate = 8,
    Marketplace = 9,
    WinBack = 10,
    Import = 11,
    Other = 12,
}

public enum LeadActivityKind
{
    Call = 1,
    Email = 2,
    Sms = 3,
    WhatsApp = 4,
    Meeting = 5,
    Tour = 6,
    TrialIssued = 7,
    QuoteSent = 8,
    Note = 9,
    StageChange = 10,
    TaskCreated = 11,
}

// ── Retention & engagement ───────────────────────────────────────────────────

/// <summary>
/// How likely this member is to leave. A band rather than a raw score, because a coach can act
/// on "high risk, has not been in for 21 days" and cannot act on "0.71".
/// </summary>
public enum ChurnRiskBand
{
    Healthy = 1,
    Watch = 2,
    AtRisk = 3,
    Critical = 4,
    /// <summary>Already gone; kept so win-back lists can be built from the same table.</summary>
    Lost = 5,
}

/// <summary>The evidence behind a risk band, listed on the at-risk board so the call has a hook.</summary>
public enum ChurnFactorKind
{
    NoRecentVisit = 1,
    VisitFrequencyDropped = 2,
    NeverAttendedAfterJoining = 3,
    NoClassBooked = 4,
    NoUpcomingBooking = 5,
    PaymentFailed = 6,
    OutstandingBalance = 7,
    ContractEndingSoon = 8,
    ComplaintOpen = 9,
    LowNpsScore = 10,
    CreditsUnused = 11,
    PtPackageExpired = 12,
    RepeatedNoShows = 13,
    FrozenTooLong = 14,
    ShortTenure = 15,
}

public enum JourneyTrigger
{
    MemberJoined = 1,
    FirstVisit = 2,
    NoVisitForDays = 3,
    PaymentFailed = 4,
    ContractEndingInDays = 5,
    Birthday = 6,
    JoinAnniversary = 7,
    ClassAttended = 8,
    ClassMissed = 9,
    Cancelled = 10,
    TrialStarted = 11,
    TrialEnding = 12,
    CreditsExpiring = 13,
    RiskBandChanged = 14,
    MilestoneReached = 15,
    LeadCreated = 16,
    Manual = 17,
}

public enum JourneyStepKind
{
    Wait = 1,
    SendEmail = 2,
    SendSms = 3,
    SendPush = 4,
    SendWhatsApp = 5,
    CreateTask = 6,
    AddTag = 7,
    RemoveTag = 8,
    GrantOffer = 9,
    GrantLoyaltyPoints = 10,
    Condition = 11,
    ExitJourney = 12,
}

public enum MessageChannel
{
    Email = 1,
    Sms = 2,
    Push = 3,
    WhatsApp = 4,
    InApp = 5,
    DeskAlert = 6,
}

public enum MessageStatus
{
    Queued = 1,
    Sent = 2,
    Delivered = 3,
    Opened = 4,
    Clicked = 5,
    Failed = 6,
    Bounced = 7,
    /// <summary>Not sent because the member has not consented on that channel.</summary>
    SuppressedNoConsent = 8,
    /// <summary>Held back by quiet hours; will go out later.</summary>
    Deferred = 9,
}

public enum LoyaltyEventKind
{
    Visit = 1,
    ClassAttended = 2,
    PtSession = 3,
    Referral = 4,
    ChallengeCompleted = 5,
    ReviewLeft = 6,
    Purchase = 7,
    Streak = 8,
    Milestone = 9,
    ManualAward = 10,
    Redemption = 11,
    Expiry = 12,
    Adjustment = 13,
}

public enum ChallengeMetric
{
    Visits = 1,
    Classes = 2,
    EffortPoints = 3,
    Distance = 4,
    Calories = 5,
    WeightLifted = 6,
    Streak = 7,
    Custom = 8,
}

// ── Staff ────────────────────────────────────────────────────────────────────

/// <summary>
/// What someone does here. Not a permission set on its own — permissions are granted separately —
/// but it drives defaults, the rota, and which screens are worth putting in front of them.
/// </summary>
public enum StaffRoleKind
{
    Owner = 1,
    Manager = 2,
    DutyManager = 3,
    Receptionist = 4,
    SalesConsultant = 5,
    PersonalTrainer = 6,
    GroupInstructor = 7,
    Coach = 8,
    Physiotherapist = 9,
    Nutritionist = 10,
    Cleaner = 11,
    Maintenance = 12,
    Lifeguard = 13,
    Childcare = 14,
    Other = 15,
}

/// <summary>How a commission line is worked out. A single payslip can carry several of these.</summary>
public enum CommissionBasis
{
    PerSessionDelivered = 1,
    PerClassTaught = 2,
    PerClassHead = 3,
    PercentOfSessionValue = 4,
    PercentOfMembershipSold = 5,
    PercentOfPackageSold = 6,
    PercentOfRetailSold = 7,
    FlatPerPeriod = 8,
    TargetBonus = 9,
}

public enum CommissionStatementStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    /// <summary>Handed to HR payroll as a single posted total.</summary>
    Exported = 4,
    Paid = 5,
    Rejected = 6,
}

public enum ShiftStatus
{
    Draft = 1,
    Published = 2,
    /// <summary>Nobody assigned; staff can claim it.</summary>
    Open = 3,
    SwapRequested = 4,
    Confirmed = 5,
    Completed = 6,
    NoShow = 7,
    Cancelled = 8,
}

public enum CertificationStatus
{
    Valid = 1,
    ExpiringSoon = 2,
    Expired = 3,
    Missing = 4,
    Suspended = 5,
}

// ── Facility ─────────────────────────────────────────────────────────────────

public enum LockerStatus
{
    Free = 1,
    Rented = 2,
    /// <summary>Issued for today only and swept overnight.</summary>
    DayUse = 3,
    OutOfOrder = 4,
    Reserved = 5,
}

public enum LockerSize
{
    Small = 1,
    Medium = 2,
    Large = 3,
    FullHeight = 4,
}

public enum ResourceKind
{
    SquashCourt = 1,
    TennisCourt = 2,
    BadmintonCourt = 3,
    PoolLane = 4,
    Sauna = 5,
    SteamRoom = 6,
    MassageRoom = 7,
    MeetingRoom = 8,
    StudioHire = 9,
    Equipment = 10,
    PitchOrField = 11,
    ClimbingWall = 12,
    Other = 13,
}

public enum ResourceBookingStatus
{
    Booked = 1,
    CheckedIn = 2,
    Completed = 3,
    NoShow = 4,
    Cancelled = 5,
    LateCancelled = 6,
    Blocked = 7,
}

public enum AssetStatus
{
    InService = 1,
    OutOfOrder = 2,
    UnderMaintenance = 3,
    AwaitingParts = 4,
    Retired = 5,
    Disposed = 6,
}

public enum WorkOrderStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    AwaitingParts = 4,
    AwaitingContractor = 5,
    Completed = 6,
    Cancelled = 7,
}

public enum WorkOrderPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    /// <summary>A safety issue — the machine comes out of service immediately.</summary>
    Critical = 4,
}

public enum MaintenanceTrigger
{
    /// <summary>Every N days.</summary>
    Interval = 1,
    /// <summary>Every N usage hours the equipment reports.</summary>
    UsageHours = 2,
    /// <summary>Only when something breaks.</summary>
    OnFault = 3,
}

// ── Compliance & operations ──────────────────────────────────────────────────

public enum DocumentKind
{
    Waiver = 1,
    HealthScreening = 2,
    MedicalClearance = 3,
    Agreement = 4,
    PhotoConsent = 5,
    IdDocument = 6,
    GuardianConsent = 7,
    CorporateProof = 8,
    StudentProof = 9,
    InsuranceCertificate = 10,
    Certification = 11,
    Other = 12,
}

public enum SignatureStatus
{
    NotSigned = 1,
    Signed = 2,
    /// <summary>Signed against an older template version; needs re-signing.</summary>
    Superseded = 3,
    Expired = 4,
    Declined = 5,
    /// <summary>Sent for remote signature and not yet returned.</summary>
    Pending = 6,
}

/// <summary>How a health-screening answer is captured, and whether it can gate participation.</summary>
public enum ScreeningAnswerKind
{
    YesNo = 1,
    Text = 2,
    Number = 3,
    SingleChoice = 4,
    MultiChoice = 5,
    Date = 6,
}

public enum ClearanceStatus
{
    NotRequired = 1,
    Required = 2,
    Submitted = 3,
    Approved = 4,
    Rejected = 5,
    Expired = 6,
}

public enum IncidentKind
{
    Injury = 1,
    Illness = 2,
    NearMiss = 3,
    EquipmentFailure = 4,
    Slip = 5,
    Aggression = 6,
    Theft = 7,
    PropertyDamage = 8,
    FirstAidGiven = 9,
    AedUsed = 10,
    AmbulanceCalled = 11,
    Safeguarding = 12,
    Other = 13,
}

public enum IncidentSeverity
{
    Minor = 1,
    Moderate = 2,
    Serious = 3,
    /// <summary>Reportable to a regulator under the market's rules.</summary>
    Reportable = 4,
}

public enum IncidentStatus
{
    Open = 1,
    UnderInvestigation = 2,
    ActionRequired = 3,
    Closed = 4,
    Escalated = 5,
}

public enum ComplaintStatus
{
    Open = 1,
    Acknowledged = 2,
    InProgress = 3,
    Resolved = 4,
    Closed = 5,
    Escalated = 6,
}

public enum LostPropertyStatus
{
    Held = 1,
    Claimed = 2,
    Donated = 3,
    Disposed = 4,
}

public enum FacilityCheckKind
{
    Opening = 1,
    Closing = 2,
    Cleaning = 3,
    Safety = 4,
    PoolChemistry = 5,
    Temperature = 6,
    EquipmentSweep = 7,
    FireCheck = 8,
    Custom = 9,
}

// ── Corporate ────────────────────────────────────────────────────────────────

public enum CorporateBillingModel
{
    /// <summary>The employer pays one invoice for everybody.</summary>
    EmployerPaysAll = 1,
    /// <summary>The employee pays a discounted rate themselves.</summary>
    EmployeePaysDiscounted = 2,
    /// <summary>Split — the employer covers a fixed amount or percentage.</summary>
    Subsidised = 3,
}

public enum EligibilityProof
{
    None = 1,
    EmailDomain = 2,
    EmployeeId = 3,
    EmployeeList = 4,
    UploadedDocument = 5,
    AccessCode = 6,
}

// ── Cash & retail ────────────────────────────────────────────────────────────

public enum CashSessionStatus
{
    Open = 1,
    /// <summary>Counted without seeing the expected figure, which is how variance stays honest.</summary>
    BlindCounted = 2,
    Reconciled = 3,
    Closed = 4,
}

public enum CashMovementKind
{
    OpeningFloat = 1,
    Sale = 2,
    Refund = 3,
    PaidIn = 4,
    PaidOut = 5,
    Drop = 6,
    ClosingCount = 7,
    Variance = 8,
}

// ── Reporting ────────────────────────────────────────────────────────────────

/// <summary>Coarse period selector shared by every report so the filters look the same everywhere.</summary>
public enum ReportPeriod
{
    Today = 1,
    Yesterday = 2,
    ThisWeek = 3,
    LastWeek = 4,
    ThisMonth = 5,
    LastMonth = 6,
    ThisQuarter = 7,
    ThisYear = 8,
    Last7Days = 9,
    Last30Days = 10,
    Last90Days = 11,
    Custom = 12,
}

/// <summary>How a member's units are shown. A body-composition chart in the wrong unit is worse than no chart.</summary>
public enum UnitSystem
{
    Metric = 1,
    Imperial = 2,
}
