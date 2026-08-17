using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Staff ────────────────────────────────────────────────────────────────────

public class RestaurantStaffDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public StaffRole Role { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>Never carries the PIN itself — only whether one has been set.</summary>
    public bool HasPin { get; set; }
    public bool IsLocked { get; set; }

    public bool CanTakeOrders { get; set; }
    public bool CanVoidLines { get; set; }
    public bool CanApplyDiscounts { get; set; }
    public bool CanApproveDiscounts { get; set; }
    public bool CanOpenCashDrawer { get; set; }
    public bool CanCloseSession { get; set; }
    public bool CanRunReports { get; set; }
    public bool CanEditMenu { get; set; }
    public bool CanManageTables { get; set; }
    public bool CanServeAlcohol { get; set; }

    public Guid? DefaultSectionId { get; set; }
    public string? DefaultSectionName { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? TipSharePercent { get; set; }
    public DateTime? HiredOn { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    /// <summary>Live: on shift right now, and how many tables they are holding.</summary>
    public bool IsOnShift { get; set; }
    public int OpenTableCount { get; set; }
}

public class SaveRestaurantStaffDto
{
    public string? Code { get; set; }
    public Guid OutletId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Waiter;
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public bool CanTakeOrders { get; set; } = true;
    public bool CanVoidLines { get; set; }
    public bool CanApplyDiscounts { get; set; }
    public bool CanApproveDiscounts { get; set; }
    public bool CanOpenCashDrawer { get; set; }
    public bool CanCloseSession { get; set; }
    public bool CanRunReports { get; set; }
    public bool CanEditMenu { get; set; }
    public bool CanManageTables { get; set; }
    public bool CanServeAlcohol { get; set; }
    public Guid? DefaultSectionId { get; set; }
    public decimal? HourlyRate { get; set; }
    public decimal? TipSharePercent { get; set; }
    public DateTime? HiredOn { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
}

public class SetStaffPinDto
{
    public Guid StaffId { get; set; }

    /// <summary>4–8 digits. Hashed on arrival and never stored or logged in the clear.</summary>
    public string Pin { get; set; } = string.Empty;
}

public class StaffPinLoginDto
{
    public Guid OutletId { get; set; }
    public string Pin { get; set; } = string.Empty;
}

/// <summary>Who signed in, and exactly what they are allowed to do — the till gates on this.</summary>
public class StaffSessionDto
{
    public Guid StaffId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public StaffRole Role { get; set; }
    public Guid OutletId { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? ShiftId { get; set; }
    public bool IsOnShift { get; set; }

    public bool CanTakeOrders { get; set; }
    public bool CanVoidLines { get; set; }
    public bool CanApplyDiscounts { get; set; }
    public bool CanApproveDiscounts { get; set; }
    public bool CanOpenCashDrawer { get; set; }
    public bool CanCloseSession { get; set; }
    public bool CanRunReports { get; set; }
    public bool CanEditMenu { get; set; }
    public bool CanManageTables { get; set; }
    public bool CanServeAlcohol { get; set; }
}

public class StaffShiftDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan ScheduledStart { get; set; }
    public TimeSpan ScheduledEnd { get; set; }
    public ShiftStatus Status { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public int BreakMinutes { get; set; }
    public decimal HoursWorked { get; set; }
    public Guid? SectionId { get; set; }
    public string? SectionName { get; set; }
    public StaffRole Role { get; set; }
    public decimal SalesAmount { get; set; }
    public int OrdersHandled { get; set; }
    public int CoversServed { get; set; }
    public decimal TipsEarned { get; set; }
    public string? Note { get; set; }
}

public class SaveStaffShiftDto
{
    public Guid OutletId { get; set; }
    public Guid StaffId { get; set; }
    public DateTime ShiftDate { get; set; }
    public TimeSpan ScheduledStart { get; set; }
    public TimeSpan ScheduledEnd { get; set; }
    public Guid? SectionId { get; set; }
    public StaffRole Role { get; set; } = StaffRole.Waiter;
    public string? Note { get; set; }
}

public class ClockDto
{
    public Guid StaffId { get; set; }
    public Guid OutletId { get; set; }
    public Guid? ShiftId { get; set; }
    public bool IsBreak { get; set; }
    public string? Note { get; set; }
}

public class TimeClockEntryDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime ClockedInAt { get; set; }
    public DateTime? ClockedOutAt { get; set; }
    public bool IsBreak { get; set; }
    public decimal Hours { get; set; }
    public bool IsAdjusted { get; set; }
    public string? Note { get; set; }
}

// ── Cash sessions ────────────────────────────────────────────────────────────

public class RestaurantSessionDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public SessionStatus Status { get; set; }
    public Guid? CashierId { get; set; }
    public string? CashierName { get; set; }
    public string? TerminalName { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningFloat { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ExpectedCard { get; set; }
    public decimal ExpectedOther { get; set; }
    public decimal CountedCash { get; set; }
    public decimal CountedCard { get; set; }
    public decimal CountedOther { get; set; }
    public decimal CashVariance { get; set; }
    public bool IsBlindClose { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal TotalVoids { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalTips { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalServiceCharge { get; set; }
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
    public int CheckCount { get; set; }
    public int XReadCount { get; set; }
    public DateTime? ZReadAt { get; set; }
    public string? ClosingNote { get; set; }
    public List<SessionCashMovementDto> CashMovements { get; set; } = [];
}

public class OpenSessionDto
{
    public Guid OutletId { get; set; }
    public Guid? CashierId { get; set; }
    public string? TerminalName { get; set; }
    public decimal OpeningFloat { get; set; }
    public bool IsBlindClose { get; set; } = true;
}

public class CloseSessionDto
{
    public Guid SessionId { get; set; }
    public decimal CountedCash { get; set; }
    public decimal CountedCard { get; set; }
    public decimal CountedOther { get; set; }
    public string? ClosingNote { get; set; }
    public Guid? StaffId { get; set; }
    public string? ApprovalPin { get; set; }
}

public class SessionCashMovementDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public CashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class AddCashMovementDto
{
    public Guid SessionId { get; set; }
    public CashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public string? Reference { get; set; }
    public Guid? StaffId { get; set; }
}

/// <summary>
/// X-read (mid-shift, repeatable) and Z-read (end of shift, once) share a shape — the difference
/// is that a Z closes the session and an X does not.
/// </summary>
public class ReadReportDto
{
    public Guid SessionId { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public string ReportType { get; set; } = "X";
    public string OutletName { get; set; } = string.Empty;
    public string? CashierName { get; set; }
    public string? TerminalName { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal OpeningFloat { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal Tax { get; set; }
    public decimal NetSales { get; set; }
    public decimal Tips { get; set; }
    public decimal Voids { get; set; }
    public decimal Refunds { get; set; }

    public decimal ExpectedCash { get; set; }
    public decimal CountedCash { get; set; }
    public decimal CashVariance { get; set; }

    public int OrderCount { get; set; }
    public int CheckCount { get; set; }
    public int CoverCount { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal AveragePerCover { get; set; }

    public List<TenderBreakdownDto> TenderBreakdown { get; set; } = [];
    public List<CategorySalesDto> CategoryBreakdown { get; set; } = [];
    public List<SessionCashMovementDto> CashMovements { get; set; } = [];
}

public class TenderBreakdownDto
{
    public TenderType TenderType { get; set; }
    public string TenderName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal SharePercent { get; set; }
}

// ── Recipes & cost ───────────────────────────────────────────────────────────

public class RecipeDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public Guid? MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal YieldQuantity { get; set; }
    public string YieldUom { get; set; } = "portion";
    public bool IsSubRecipe { get; set; }
    public Guid? OutputInventoryItemId { get; set; }
    public decimal TotalCost { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Instructions { get; set; }
    public string? PlatingNotes { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    /// <summary>Selling price of the linked item, so cost % is visible on the recipe itself.</summary>
    public decimal SellingPrice { get; set; }
    public decimal CostPerPortion { get; set; }
    public decimal FoodCostPercent { get; set; }
    public decimal ContributionMargin { get; set; }

    public List<RecipeIngredientDto> Ingredients { get; set; } = [];
}

public class SaveRecipeDto
{
    public string? Code { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal YieldQuantity { get; set; } = 1;
    public string YieldUom { get; set; } = "portion";
    public bool IsSubRecipe { get; set; }
    public Guid? OutputInventoryItemId { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public string? Instructions { get; set; }
    public string? PlatingNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<RecipeIngredientDto> Ingredients { get; set; } = [];
}

public class RecipeIngredientDto
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public Guid? SubRecipeId { get; set; }
    public string IngredientName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "g";
    public decimal YieldPercent { get; set; } = 100m;
    public decimal WastePercent { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public bool IsOptional { get; set; }
    public int DisplayOrder { get; set; }
    public string? Note { get; set; }
}

public class WastageLogDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public DateTime OccurredAt { get; set; }
    public WastageReason Reason { get; set; }
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public bool IsStockAdjusted { get; set; }
    public string? Note { get; set; }
}

public class SaveWastageDto
{
    public Guid OutletId { get; set; }
    public DateTime? OccurredAt { get; set; }
    public WastageReason Reason { get; set; } = WastageReason.Spoilage;
    public Guid? MenuItemId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Uom { get; set; } = "portion";
    public decimal UnitCost { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? StationId { get; set; }
    public string? Note { get; set; }
}

// ── Settings ─────────────────────────────────────────────────────────────────

public class RestaurantSettingsDto
{
    public Guid Id { get; set; }
    public bool RequireWaiterPin { get; set; }
    public bool RequireGuestCountOnSeat { get; set; }
    public bool RequireSeatNumbers { get; set; }
    public bool AutoFireOnSend { get; set; }
    public int SeatedAttentionMinutes { get; set; }
    public int ServedAttentionMinutes { get; set; }
    public bool AllowTableMerge { get; set; }
    public bool AllowTableTransfer { get; set; }
    public bool AllowSplitBill { get; set; }

    public bool PricesIncludeTax { get; set; }
    public bool TipsEnabled { get; set; }
    public string? TipPresetPercents { get; set; }
    public bool TipPoolingEnabled { get; set; }
    public TipDistributionBasis TipDistributionBasis { get; set; }
    public decimal KitchenTipSharePercent { get; set; }
    public decimal CashRoundingIncrement { get; set; }
    public decimal PackagingChargePerOrder { get; set; }
    public bool VoidRequiresReason { get; set; }
    public bool DiscountRequiresReason { get; set; }
    public decimal DiscountApprovalThreshold { get; set; }

    public bool KitchenDisplayEnabled { get; set; }
    public bool PrintKitchenTickets { get; set; }
    public bool ExpoScreenEnabled { get; set; }
    public int KdsWarningMinutes { get; set; }
    public bool AutoBumpOnServe { get; set; }
    public bool ShowAllergenWarnings { get; set; }

    public bool DepleteStockOnCheckClose { get; set; }
    public bool Auto86OnZeroStock { get; set; }
    public bool TrackWastage { get; set; }

    public bool ReservationsEnabled { get; set; }
    public bool WaitlistEnabled { get; set; }
    public int ReservationHoldMinutes { get; set; }
    public int DefaultReservationDuration { get; set; }
    public bool RequireDepositForLargeParty { get; set; }
    public int LargePartyThreshold { get; set; }

    public bool PrintReceiptAutomatically { get; set; }
    public bool EmailReceiptEnabled { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public bool ShowCalories { get; set; }
    public bool FeedbackPromptEnabled { get; set; }
}
