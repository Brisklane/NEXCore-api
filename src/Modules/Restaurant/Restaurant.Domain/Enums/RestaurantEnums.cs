namespace Restaurant.Domain.Enums;

// ── Venue ────────────────────────────────────────────────────────────────────

/// <summary>How the venue serves food. Drives which screens and order types make sense.</summary>
public enum ServiceStyle
{
    FineDining = 1,
    CasualDining = 2,
    QuickService = 3,
    Cafe = 4,
    Bar = 5,
    /// <summary>Delivery-only kitchen: no floor plan, no dine-in.</summary>
    CloudKitchen = 6,
    FoodTruck = 7,
    Buffet = 8,
}

public enum TableShape
{
    Round = 1,
    Square = 2,
    Rectangle = 3,
    Oval = 4,
    Booth = 5,
    BarStool = 6,
    HighTop = 7,
    Sofa = 8,
}

/// <summary>
/// Where a table is in the service cycle. The floor plan renders this directly, and the
/// transitions between them are what the turn-time report measures.
/// </summary>
public enum TableState
{
    Free = 1,
    Reserved = 2,
    Seated = 3,
    Ordered = 4,
    Served = 5,
    BillPrinted = 6,
    Paid = 7,
    NeedsCleaning = 8,
    /// <summary>Out of service — broken, being repaired, or held back deliberately.</summary>
    Blocked = 9,
}

/// <summary>Non-table objects a floor plan can carry, so the map reads like the room.</summary>
public enum FloorFixtureKind
{
    Wall = 1,
    Door = 2,
    Window = 3,
    BarCounter = 4,
    Plant = 5,
    Pillar = 6,
    Stairs = 7,
    KitchenPass = 8,
    Restroom = 9,
    Label = 10,
}

// ── Orders ───────────────────────────────────────────────────────────────────

public enum OrderType
{
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3,
    DriveThru = 4,
    Curbside = 5,
    RoomService = 6,
    BarTab = 7,
    Counter = 8,
}

public enum OrderChannel
{
    InHouse = 1,
    Phone = 2,
    Online = 3,
    QrSelfOrder = 4,
    Aggregator = 5,
    Kiosk = 6,
}

public enum RestaurantOrderStatus
{
    Draft = 1,
    Open = 2,
    Fired = 3,
    PartiallyServed = 4,
    Served = 5,
    Billed = 6,
    Paid = 7,
    Closed = 8,
    Cancelled = 9,
}

/// <summary>
/// Course a line belongs to. Coursing is what stops a kitchen sending the dessert with the soup:
/// a course is only visible to the kitchen once the waiter fires it.
/// </summary>
public enum CourseType
{
    None = 0,
    Appetizer = 1,
    Soup = 2,
    Salad = 3,
    Main = 4,
    Side = 5,
    Dessert = 6,
    Beverage = 7,
}

public enum OrderLineStatus
{
    New = 1,
    /// <summary>Entered but deliberately withheld from the kitchen until the course is fired.</summary>
    Held = 2,
    Fired = 3,
    Preparing = 4,
    Ready = 5,
    Served = 6,
    Voided = 7,
}

// ── Kitchen ──────────────────────────────────────────────────────────────────

public enum KitchenTicketStatus
{
    New = 1,
    Acknowledged = 2,
    InProgress = 3,
    Ready = 4,
    /// <summary>Cleared from the screen — the food has left the pass.</summary>
    Bumped = 5,
    Recalled = 6,
    Cancelled = 7,
}

public enum StationType
{
    Grill = 1,
    Fryer = 2,
    ColdStation = 3,
    PizzaOven = 4,
    Tandoor = 5,
    Wok = 6,
    Bar = 7,
    Barista = 8,
    Dessert = 9,
    /// <summary>The pass: sees every station's work for a table so plates leave together.</summary>
    Expo = 10,
    Prep = 11,
}

/// <summary>What a routing rule matches on, most specific first at evaluation time.</summary>
public enum RoutingMatchType
{
    AllItems = 1,
    Category = 2,
    Item = 3,
    OrderType = 4,
}

// ── Money ────────────────────────────────────────────────────────────────────

public enum CheckStatus
{
    Open = 1,
    Printed = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Voided = 5,
    Refunded = 6,
}

public enum SplitMethod
{
    None = 0,
    BySeat = 1,
    ByItem = 2,
    Evenly = 3,
    ByAmount = 4,
    ByPercentage = 5,
}

public enum TenderType
{
    Cash = 1,
    Card = 2,
    Wallet = 3,
    GiftCard = 4,
    LoyaltyPoints = 5,
    RoomCharge = 6,
    HouseAccount = 7,
    Voucher = 8,
    BankTransfer = 9,
    Online = 10,
}

public enum DiscountKind
{
    Percentage = 1,
    Amount = 2,
    /// <summary>Line given away entirely — tracked separately from a discount for audit.</summary>
    Comp = 3,
}

public enum ServiceChargeBasis
{
    Percentage = 1,
    FixedAmount = 2,
    PerCover = 3,
}

public enum TipDistributionBasis
{
    ByHoursWorked = 1,
    BySales = 2,
    EqualShare = 3,
    FixedPercentage = 4,
}

// ── Menu ─────────────────────────────────────────────────────────────────────

public enum MenuDaypart
{
    AllDay = 0,
    Breakfast = 1,
    Brunch = 2,
    Lunch = 3,
    HighTea = 4,
    Dinner = 5,
    LateNight = 6,
}

public enum ModifierSelectionMode
{
    Single = 1,
    Multiple = 2,
}

public enum ComboComponentMode
{
    Fixed = 1,
    ChooseOne = 2,
    ChooseMany = 3,
}

/// <summary>
/// Which price of a menu item applies. Dine-in and delivery are routinely priced differently —
/// delivery carries commission, dine-in carries service — so price is per scope, not per item.
/// </summary>
public enum PriceScope
{
    Base = 0,
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3,
    DriveThru = 4,
    Online = 5,
}

public enum SpiceLevel
{
    None = 0,
    Mild = 1,
    Medium = 2,
    Hot = 3,
    ExtraHot = 4,
}

/// <summary>Classic menu-engineering quadrant: popularity against contribution margin.</summary>
public enum MenuEngineeringClass
{
    /// <summary>Popular and profitable — protect it.</summary>
    Star = 1,
    /// <summary>Popular but thin margin — re-cost or re-portion.</summary>
    Plowhorse = 2,
    /// <summary>Profitable but nobody orders it — reposition or promote.</summary>
    Puzzle = 3,
    /// <summary>Neither — remove it.</summary>
    Dog = 4,
}

// ── Front of house ───────────────────────────────────────────────────────────

public enum ReservationStatus
{
    Requested = 1,
    Confirmed = 2,
    Seated = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6,
}

public enum WaitlistStatus
{
    Waiting = 1,
    Notified = 2,
    Seated = 3,
    /// <summary>Gave up and left before being seated.</summary>
    Left = 4,
    Cancelled = 5,
}

public enum DeliveryStatus
{
    Pending = 1,
    Assigned = 2,
    PickedUp = 3,
    EnRoute = 4,
    Delivered = 5,
    Failed = 6,
    Cancelled = 7,
}

// ── Staff & cash ─────────────────────────────────────────────────────────────

public enum StaffRole
{
    Waiter = 1,
    Host = 2,
    Bartender = 3,
    Barista = 4,
    Chef = 5,
    LineCook = 6,
    KitchenPorter = 7,
    Cashier = 8,
    Supervisor = 9,
    Manager = 10,
}

public enum ShiftStatus
{
    Scheduled = 1,
    Started = 2,
    OnBreak = 3,
    Ended = 4,
    Absent = 5,
}

public enum SessionStatus
{
    Open = 1,
    Closed = 2,
    Suspended = 3,
}

public enum CashMovementType
{
    OpeningFloat = 1,
    CashIn = 2,
    CashOut = 3,
    /// <summary>Cash moved from drawer to safe mid-shift.</summary>
    Drop = 4,
    Payout = 5,
    ClosingCount = 6,
}

// ── Food safety & compliance ─────────────────────────────────────────────────

public enum TemperatureCheckpointKind
{
    Refrigerator = 1,
    Freezer = 2,
    /// <summary>Bain-marie, hot cabinet, pass lamp — food held above the danger zone.</summary>
    HotHolding = 3,
    /// <summary>Temperature of a delivery on arrival, before it is accepted.</summary>
    DeliveryIntake = 4,
    /// <summary>Probe into the thickest part of cooked food.</summary>
    CookedCore = 5,
    /// <summary>Reheated food, which has its own (higher) safe threshold.</summary>
    Reheated = 6,
    AmbientStore = 7,
    DishwasherRinse = 8,
}

public enum ChecklistFrequency
{
    /// <summary>Run once, at the start of trading.</summary>
    Opening = 1,
    /// <summary>Run once, at close.</summary>
    Closing = 2,
    Daily = 3,
    Weekly = 4,
    Monthly = 5,
    /// <summary>Triggered by an event (an allergen changeover, a deep clean) rather than a clock.</summary>
    OnDemand = 6,
}

public enum ChecklistAnswerType
{
    YesNo = 1,
    /// <summary>A measured value that must fall inside the item's range.</summary>
    Numeric = 2,
    Text = 3,
    /// <summary>Nothing to answer — a step that is simply acknowledged.</summary>
    Acknowledge = 4,
}

// ── Costing ──────────────────────────────────────────────────────────────────

public enum WastageReason
{
    Spoilage = 1,
    Burnt = 2,
    Dropped = 3,
    OverPortioned = 4,
    GuestReturn = 5,
    StaffMeal = 6,
    Expired = 7,
    Training = 8,
    Other = 99,
}
