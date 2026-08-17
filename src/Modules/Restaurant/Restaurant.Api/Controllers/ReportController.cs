using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;

namespace Restaurant.Api.Controllers;

/// <summary>The dashboard and every analytical report.</summary>
[Route("api/restaurant/reports")]
public class ReportController(
    IRestaurantReportService reports,
    ILogger<ReportController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard([FromQuery] Guid? outletId)
        => Run(() => reports.GetDashboardAsync(outletId));

    [HttpPost("sales-summary")]
    public Task<IActionResult> SalesSummary([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetSalesSummaryAsync(filter));

    /// <summary>Popularity × margin, with every dish labelled Star, Plowhorse, Puzzle or Dog.</summary>
    [HttpPost("menu-engineering")]
    public Task<IActionResult> MenuEngineering([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetMenuEngineeringAsync(filter));

    [HttpPost("waiter-performance")]
    public Task<IActionResult> WaiterPerformance([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetWaiterPerformanceAsync(filter));

    [HttpPost("table-turnover")]
    public Task<IActionResult> TableTurnover([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetTableTurnoverAsync(filter));

    [HttpPost("kitchen-performance")]
    public Task<IActionResult> KitchenPerformance([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetKitchenPerformanceAsync(filter));

    /// <summary>Voids, comps and discounts in one audit — leakage rarely shows up in only one.</summary>
    [HttpPost("void-audit")]
    public Task<IActionResult> VoidAudit([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetVoidAuditAsync(filter));

    [HttpPost("wastage-summary")]
    public Task<IActionResult> WastageSummary([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetWastageSummaryAsync(filter));

    [HttpPost("item-sales")]
    public Task<IActionResult> ItemSales([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetItemSalesAsync(filter));
}

/// <summary>
/// Enum lookups, so the UI never hard-codes a numeric value that could drift from the server's.
/// </summary>
[Route("api/restaurant/lookups")]
public class LookupController(ILogger<LookupController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet("service-styles")]      public IActionResult ServiceStyles()      => Ok(Wrap<ServiceStyle>());
    [HttpGet("table-shapes")]        public IActionResult TableShapes()        => Ok(Wrap<TableShape>());
    [HttpGet("table-states")]        public IActionResult TableStates()        => Ok(Wrap<TableState>());
    [HttpGet("fixture-kinds")]       public IActionResult FixtureKinds()       => Ok(Wrap<FloorFixtureKind>());
    [HttpGet("order-types")]         public IActionResult OrderTypes()         => Ok(Wrap<OrderType>());
    [HttpGet("order-channels")]      public IActionResult OrderChannels()      => Ok(Wrap<OrderChannel>());
    [HttpGet("order-statuses")]      public IActionResult OrderStatuses()      => Ok(Wrap<RestaurantOrderStatus>());
    [HttpGet("courses")]             public IActionResult Courses()            => Ok(Wrap<CourseType>());
    [HttpGet("line-statuses")]       public IActionResult LineStatuses()       => Ok(Wrap<OrderLineStatus>());
    [HttpGet("ticket-statuses")]     public IActionResult TicketStatuses()     => Ok(Wrap<KitchenTicketStatus>());
    [HttpGet("station-types")]       public IActionResult StationTypes()       => Ok(Wrap<StationType>());
    [HttpGet("routing-match-types")] public IActionResult RoutingMatchTypes()  => Ok(Wrap<RoutingMatchType>());
    [HttpGet("check-statuses")]      public IActionResult CheckStatuses()      => Ok(Wrap<CheckStatus>());
    [HttpGet("split-methods")]       public IActionResult SplitMethods()       => Ok(Wrap<SplitMethod>());
    [HttpGet("tender-types")]        public IActionResult TenderTypes()        => Ok(Wrap<TenderType>());
    [HttpGet("discount-kinds")]      public IActionResult DiscountKinds()      => Ok(Wrap<DiscountKind>());
    [HttpGet("service-charge-bases")]public IActionResult ServiceChargeBases() => Ok(Wrap<ServiceChargeBasis>());
    [HttpGet("tip-bases")]           public IActionResult TipBases()           => Ok(Wrap<TipDistributionBasis>());
    [HttpGet("dayparts")]            public IActionResult Dayparts()           => Ok(Wrap<MenuDaypart>());
    [HttpGet("modifier-modes")]      public IActionResult ModifierModes()      => Ok(Wrap<ModifierSelectionMode>());
    [HttpGet("combo-modes")]         public IActionResult ComboModes()         => Ok(Wrap<ComboComponentMode>());
    [HttpGet("price-scopes")]        public IActionResult PriceScopes()        => Ok(Wrap<PriceScope>());
    [HttpGet("spice-levels")]        public IActionResult SpiceLevels()        => Ok(Wrap<SpiceLevel>());
    [HttpGet("reservation-statuses")]public IActionResult ReservationStatuses()=> Ok(Wrap<ReservationStatus>());
    [HttpGet("waitlist-statuses")]   public IActionResult WaitlistStatuses()   => Ok(Wrap<WaitlistStatus>());
    [HttpGet("delivery-statuses")]   public IActionResult DeliveryStatuses()   => Ok(Wrap<DeliveryStatus>());
    [HttpGet("staff-roles")]         public IActionResult StaffRoles()         => Ok(Wrap<StaffRole>());
    [HttpGet("shift-statuses")]      public IActionResult ShiftStatuses()      => Ok(Wrap<ShiftStatus>());
    [HttpGet("session-statuses")]    public IActionResult SessionStatuses()    => Ok(Wrap<SessionStatus>());
    [HttpGet("cash-movement-types")] public IActionResult CashMovementTypes()  => Ok(Wrap<CashMovementType>());
    [HttpGet("wastage-reasons")]     public IActionResult WastageReasons()     => Ok(Wrap<WastageReason>());

    private static Nexcore.SharedKernel.Api.ApiResponse<List<LookupItemDto>> Wrap<TEnum>() where TEnum : struct, Enum
        => new()
        {
            Success = true,
            Data = Enum.GetValues<TEnum>()
                .Select(v => new LookupItemDto
                {
                    Value = Convert.ToInt32(v),
                    Name = v.ToString(),
                    Label = Humanise(v.ToString()),
                })
                .ToList(),
        };

    /// <summary>"QuickService" → "Quick service" — enum names are for code, labels are for people.</summary>
    private static string Humanise(string name)
    {
        var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }
}
