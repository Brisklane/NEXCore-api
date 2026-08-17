using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Enums;

namespace Restaurant.Application.Services.Interfaces;

/// <summary>
/// The table state machine and everything that moves a party around the room. Table state is
/// only ever changed through here, so the state log — and therefore every turn-time number —
/// stays complete.
/// </summary>
public interface IFloorService
{
    Task<FloorPlanViewDto> GetFloorPlanAsync(Guid outletId);
    Task<List<TableDto>> GetTablesAsync(Guid outletId, Guid? floorId = null);
    Task<TableDto?> GetTableAsync(Guid tableId);

    Task<FloorDto> SaveLayoutAsync(SaveLayoutDto request, Guid userId);

    Task<RestaurantOrderDto?> SeatGuestsAsync(SeatGuestsDto request, Guid userId);
    Task<TableDto> ChangeStateAsync(ChangeTableStateDto request, Guid userId);
    Task<TableDto> AssignWaiterAsync(AssignWaiterDto request, Guid userId);

    /// <summary>Moves the open order to another table; the origin is freed and flagged for cleaning.</summary>
    Task<TableDto> TransferTableAsync(TransferTableDto request, Guid userId);

    /// <summary>Folds several tables into one check. Fails if more than one already has an order.</summary>
    Task<List<TableDto>> MergeTablesAsync(MergeTablesDto request, Guid userId);
    Task<List<TableDto>> UnmergeTableAsync(Guid primaryTableId, Guid userId);

    Task<TableDto> ClearTableAsync(Guid tableId, Guid userId);
}

/// <summary>Order lifecycle: open, add, price, hold, fire, void, close.</summary>
public interface IRestaurantOrderService
{
    Task<RestaurantOrderDto> OpenOrderAsync(OpenOrderDto request, Guid userId);
    Task<RestaurantOrderDto?> GetOrderAsync(Guid orderId);
    Task<RestaurantOrderDto?> GetOrderByTableAsync(Guid tableId);
    Task<PaginatedResponse<OrderSummaryDto>> ListOrdersAsync(
        Guid? outletId, RestaurantOrderStatus? status, OrderType? orderType,
        DateTime? from, DateTime? to, PaginationParams pagination);

    /// <summary>Open orders for the service rail — no paging, this is the live working set.</summary>
    Task<List<OrderSummaryDto>> GetActiveOrdersAsync(Guid outletId, Guid? waiterId = null);

    Task<RestaurantOrderDto> AddLinesAsync(AddLinesDto request, Guid userId);
    Task<RestaurantOrderDto> UpdateLineAsync(Guid lineId, UpdateOrderLineDto request, Guid userId);
    Task<RestaurantOrderDto> VoidLineAsync(VoidLineDto request, Guid userId);
    Task<RestaurantOrderDto> FireCourseAsync(FireCourseDto request, Guid userId);
    Task<RestaurantOrderDto> MoveLinesAsync(MoveLinesDto request, Guid userId);
    Task<RestaurantOrderDto> UpdateOrderAsync(Guid orderId, UpdateOrderDto request, Guid userId);
    Task<RestaurantOrderDto> CancelOrderAsync(Guid orderId, CancelOrderDto request, Guid userId);

    /// <summary>Marks lines served; when the last one lands, the table moves to Served.</summary>
    Task<RestaurantOrderDto> MarkServedAsync(Guid orderId, List<Guid> lineIds, Guid userId);

    Task<RestaurantDeliveryDto> UpdateDeliveryAsync(Guid orderId, UpdateDeliveryStatusDto request, Guid userId);

    /// <summary>The catalogue the order pad renders, priced for this outlet and order type.</summary>
    Task<OrderPadCatalogDto> GetOrderPadCatalogAsync(Guid outletId, OrderType orderType);
}

/// <summary>Ticket routing and the KDS lifecycle.</summary>
public interface IKitchenService
{
    Task<KitchenDisplayDto> GetDisplayAsync(Guid outletId, Guid? stationId, bool includeBumped = false);
    Task<List<KitchenTicketDto>> GetTicketsForOrderAsync(Guid orderId);

    Task<KitchenTicketDto> AcknowledgeAsync(Guid ticketId, Guid userId);
    Task<KitchenTicketDto> StartAsync(Guid ticketId, Guid userId);
    Task<KitchenTicketDto> MarkReadyAsync(Guid ticketId, Guid? ticketLineId, Guid userId);
    Task<KitchenTicketDto> BumpAsync(BumpTicketDto request, Guid userId);
    Task<KitchenTicketDto> RecallAsync(RecallTicketDto request, Guid userId);
    Task<KitchenTicketDto> SetPriorityAsync(Guid ticketId, bool isPriority, Guid userId);

    /// <summary>
    /// Builds the tickets for a set of fired lines, one per station the routing rules select.
    /// Called by the order service on fire; exposed so a reprint can rebuild them.
    /// </summary>
    Task<List<KitchenTicketDto>> CreateTicketsAsync(Guid orderId, List<Guid> orderLineIds, bool isPriority, Guid userId);
}

/// <summary>Checks, splitting, tenders, discounts — and the close that moves stock and money.</summary>
public interface ICheckService
{
    Task<List<RestaurantCheckDto>> CreateChecksAsync(CreateChecksDto request, Guid userId);
    Task<RestaurantCheckDto?> GetCheckAsync(Guid checkId);
    Task<List<RestaurantCheckDto>> GetChecksForOrderAsync(Guid orderId);
    Task<PaginatedResponse<RestaurantCheckDto>> ListChecksAsync(
        Guid? outletId, CheckStatus? status, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<RestaurantCheckDto> TakePaymentAsync(TakePaymentDto request, Guid userId);
    Task<RestaurantCheckDto> ApplyDiscountAsync(ApplyDiscountDto request, Guid userId);
    Task<RestaurantCheckDto> RemoveDiscountAsync(Guid discountId, Guid userId);
    Task<RestaurantCheckDto> AddTipAsync(AddTipDto request, Guid userId);
    Task<RestaurantCheckDto> WaiveServiceChargeAsync(WaiveServiceChargeDto request, Guid userId);
    Task<RestaurantCheckDto> VoidCheckAsync(VoidCheckDto request, Guid userId);
    Task<RestaurantCheckDto> RefundPaymentAsync(RefundPaymentDto request, Guid userId);
    Task<RestaurantCheckDto> MarkPrintedAsync(Guid checkId, Guid userId);
}

/// <summary>PIN authentication, rosters, clock-in and tip pooling.</summary>
public interface IRestaurantStaffService
{
    Task<StaffSessionDto?> PinLoginAsync(StaffPinLoginDto request);
    Task SetPinAsync(SetStaffPinDto request, Guid userId);

    /// <summary>
    /// Verifies a supervisor PIN for one specific permission. Used by the till whenever an
    /// action is above the operator's own level.
    /// </summary>
    Task<StaffSessionDto?> VerifyApprovalAsync(Guid outletId, string pin, string permission);

    Task<List<RestaurantStaffDto>> GetStaffAsync(Guid? outletId, StaffRole? role, bool activeOnly);
    Task<RestaurantStaffDto?> GetStaffMemberAsync(Guid staffId);
    Task<RestaurantStaffDto> SaveStaffAsync(Guid? id, SaveRestaurantStaffDto request, Guid userId);
    Task DeleteStaffAsync(Guid staffId, Guid userId);

    Task<List<StaffShiftDto>> GetShiftsAsync(Guid outletId, DateTime from, DateTime to, Guid? staffId);
    Task<StaffShiftDto> SaveShiftAsync(Guid? id, SaveStaffShiftDto request, Guid userId);
    Task DeleteShiftAsync(Guid shiftId, Guid userId);
    Task<TimeClockEntryDto> ClockInAsync(ClockDto request, Guid userId);
    Task<TimeClockEntryDto> ClockOutAsync(Guid staffId, Guid userId);
    Task<List<TimeClockEntryDto>> GetTimeClockAsync(Guid outletId, DateTime from, DateTime to, Guid? staffId);

    Task<TipPoolDto> CreateTipPoolAsync(CreateTipPoolDto request, Guid userId);
    Task<TipPoolDto> CalculateTipPoolAsync(Guid tipPoolId, Guid userId);
    Task<TipPoolDto> FinaliseTipPoolAsync(Guid tipPoolId, Guid userId);
    Task<List<TipPoolDto>> GetTipPoolsAsync(Guid outletId, DateTime from, DateTime to);
    Task<List<TipRecordDto>> GetTipsAsync(Guid outletId, DateTime from, DateTime to, Guid? waiterId);
}

/// <summary>Cash sessions and the X/Z reads that close a trading day.</summary>
public interface IRestaurantSessionService
{
    Task<RestaurantSessionDto> OpenSessionAsync(OpenSessionDto request, Guid userId);
    Task<RestaurantSessionDto> CloseSessionAsync(CloseSessionDto request, Guid userId);
    Task<RestaurantSessionDto?> GetSessionAsync(Guid sessionId);
    Task<RestaurantSessionDto?> GetOpenSessionAsync(Guid outletId, string? terminalName);
    Task<PaginatedResponse<RestaurantSessionDto>> ListSessionsAsync(
        Guid? outletId, SessionStatus? status, DateTime? from, DateTime? to, PaginationParams pagination);

    Task<SessionCashMovementDto> AddCashMovementAsync(AddCashMovementDto request, Guid userId);
    Task<ReadReportDto> GetXReadAsync(Guid sessionId);
    Task<ReadReportDto> GetZReadAsync(Guid sessionId, Guid userId);
}

/// <summary>Bookings, availability and the walk-in queue.</summary>
public interface IReservationService
{
    Task<List<ReservationDto>> GetReservationsAsync(Guid outletId, DateTime from, DateTime to, ReservationStatus? status);
    Task<ReservationDto?> GetReservationAsync(Guid reservationId);
    Task<ReservationDto> SaveReservationAsync(Guid? id, SaveReservationDto request, Guid userId);
    Task<ReservationDto> ChangeStatusAsync(Guid reservationId, ChangeReservationStatusDto request, Guid userId);
    Task<ReservationAvailabilityDto> CheckAvailabilityAsync(Guid outletId, DateTime forTime, int partySize, int durationMinutes);

    Task<List<WaitlistEntryDto>> GetWaitlistAsync(Guid outletId, bool activeOnly = true);
    Task<WaitlistEntryDto> SaveWaitlistEntryAsync(Guid? id, SaveWaitlistEntryDto request, Guid userId);
    Task<WaitlistEntryDto> ChangeWaitlistStatusAsync(Guid entryId, ChangeWaitlistStatusDto request, Guid userId);
}

/// <summary>Recipe cost roll-up, the explosion that depletes stock, and wastage.</summary>
public interface IRecipeCostingService
{
    Task<List<RecipeDto>> GetRecipesAsync(Guid? menuItemId, bool includeSubRecipes);
    Task<RecipeDto?> GetRecipeAsync(Guid recipeId);
    Task<RecipeDto> SaveRecipeAsync(Guid? id, SaveRecipeDto request, Guid userId);
    Task DeleteRecipeAsync(Guid recipeId, Guid userId);

    /// <summary>Recomputes cost for one recipe and pushes the result onto its menu item.</summary>
    Task<RecipeDto> RecalculateCostAsync(Guid recipeId, Guid userId);

    /// <summary>Recomputes every recipe — run after an ingredient price change.</summary>
    Task<int> RecalculateAllAsync(Guid userId);

    Task<List<WastageLogDto>> GetWastageAsync(Guid outletId, DateTime from, DateTime to, WastageReason? reason);
    Task<WastageLogDto> LogWastageAsync(SaveWastageDto request, Guid userId);
    Task DeleteWastageAsync(Guid wastageId, Guid userId);
}

/// <summary>Dashboard and every analytical report.</summary>
public interface IRestaurantReportService
{
    Task<RestaurantDashboardDto> GetDashboardAsync(Guid? outletId);
    Task<SalesSummaryReportDto> GetSalesSummaryAsync(ReportFilterDto filter);
    Task<MenuEngineeringReportDto> GetMenuEngineeringAsync(ReportFilterDto filter);
    Task<List<WaiterPerformanceDto>> GetWaiterPerformanceAsync(ReportFilterDto filter);
    Task<List<TableTurnoverDto>> GetTableTurnoverAsync(ReportFilterDto filter);
    Task<List<KitchenPerformanceDto>> GetKitchenPerformanceAsync(ReportFilterDto filter);
    Task<List<VoidAuditRowDto>> GetVoidAuditAsync(ReportFilterDto filter);
    Task<List<WastageSummaryDto>> GetWastageSummaryAsync(ReportFilterDto filter);
    Task<List<ItemSalesDto>> GetItemSalesAsync(ReportFilterDto filter);
}

/// <summary>Menu resolution — which menus are live, and what an item actually costs a guest.</summary>
public interface IMenuService
{
    Task<List<MenuCardDto>> GetMenusAsync(Guid? outletId, bool activeNowOnly);
    Task<MenuItemDto?> GetItemAsync(Guid itemId);
    Task<MenuItemDto> SaveItemAsync(Guid? id, SaveMenuItemDto request, Guid userId);
    Task DeleteItemAsync(Guid itemId, Guid userId);
    Task<PaginatedResponse<MenuItemDto>> ListItemsAsync(
        Guid? menuId, Guid? categoryId, string? search, PaginationParams pagination);

    Task<ModifierGroupDto> SaveModifierGroupAsync(Guid? id, SaveModifierGroupDto request, Guid userId);
    Task<List<ModifierGroupDto>> GetModifierGroupsAsync();

    Task<ComboMealDto> SaveComboAsync(Guid? id, ComboMealDto request, Guid userId);
    Task<List<ComboMealDto>> GetCombosAsync(Guid? outletId);

    Task<List<AvailabilityDto>> Get86ListAsync(Guid outletId, bool unavailableOnly);
    Task<AvailabilityDto> Set86Async(Set86Dto request, Guid userId);

    /// <summary>
    /// The price a guest is charged, resolved server-side: scoped price, then happy hour, then
    /// the item's base. The order pad shows this so the till and the screen can never disagree.
    /// </summary>
    Task<decimal> ResolvePriceAsync(Guid menuItemId, Guid? variantId, Guid outletId, OrderType orderType, DateTime at);
}
