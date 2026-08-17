using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

/// <summary>Filter shared by every report endpoint, so the screens can reuse one control bar.</summary>
public class ReportFilterDto
{
    public Guid? OutletId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? WaiterId { get; set; }
    public Guid? StationId { get; set; }
    public Guid? CategoryId { get; set; }
    public OrderType? OrderType { get; set; }
    public int Top { get; set; } = 20;
}

// ── Dashboard ────────────────────────────────────────────────────────────────

/// <summary>The manager's home screen: today at a glance, plus what needs attention now.</summary>
public class RestaurantDashboardDto
{
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // Today
    public decimal SalesToday { get; set; }
    public decimal SalesYesterday { get; set; }
    public decimal SalesChangePercent { get; set; }
    public int OrdersToday { get; set; }
    public int CoversToday { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal AveragePerCover { get; set; }
    public decimal TipsToday { get; set; }
    public decimal DiscountsToday { get; set; }
    public decimal VoidsToday { get; set; }
    public decimal FoodCostToday { get; set; }
    public decimal FoodCostPercent { get; set; }
    public decimal WastageToday { get; set; }

    // Right now
    public int OpenOrders { get; set; }
    public decimal OpenOrderValue { get; set; }
    public int OccupiedTables { get; set; }
    public int TotalTables { get; set; }
    public decimal OccupancyPercent { get; set; }
    public int SeatedGuests { get; set; }
    public int KitchenTicketsOpen { get; set; }
    public int KitchenTicketsOverdue { get; set; }
    public int WaitlistLength { get; set; }
    public int UpcomingReservations { get; set; }
    public int StaffOnShift { get; set; }
    public int TablesNeedingAttention { get; set; }
    public int Items86 { get; set; }

    public decimal AverageTableTurnMinutes { get; set; }
    public decimal AveragePrepMinutes { get; set; }

    public List<HourlySalesDto> SalesByHour { get; set; } = [];
    public List<CategorySalesDto> TopCategories { get; set; } = [];
    public List<ItemSalesDto> TopItems { get; set; } = [];
    public List<WaiterPerformanceDto> TopWaiters { get; set; } = [];
    public List<OrderSummaryDto> RecentOrders { get; set; } = [];
    public List<ReservationDto> NextReservations { get; set; } = [];
}

public class HourlySalesDto
{
    public int Hour { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
}

public class CategorySalesDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal SharePercent { get; set; }
}

public class ItemSalesDto
{
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public decimal CostAmount { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal SharePercent { get; set; }
}

// ── Menu engineering ─────────────────────────────────────────────────────────

/// <summary>
/// One item placed on the popularity × margin grid. The classification is the point of the
/// report: it turns "this sold 40 units" into "re-cost this or drop it".
/// </summary>
public class MenuEngineeringRowDto
{
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ContributionMargin { get; set; }
    public decimal TotalMargin { get; set; }
    public decimal FoodCostPercent { get; set; }

    /// <summary>Share of units sold across the analysed set.</summary>
    public decimal PopularityPercent { get; set; }

    public MenuEngineeringClass Classification { get; set; }
    public string ClassificationName { get; set; } = string.Empty;

    /// <summary>Plain-language next step, so the report is actionable without training.</summary>
    public string Recommendation { get; set; } = string.Empty;
}

public class MenuEngineeringReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? OutletId { get; set; }
    public decimal AverageMargin { get; set; }

    /// <summary>Popularity line: an item beats "popular" if it exceeds this share.</summary>
    public decimal PopularityThreshold { get; set; }

    public int StarCount { get; set; }
    public int PlowhorseCount { get; set; }
    public int PuzzleCount { get; set; }
    public int DogCount { get; set; }
    public List<MenuEngineeringRowDto> Rows { get; set; } = [];
}

// ── Operational reports ──────────────────────────────────────────────────────

public class WaiterPerformanceDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
    public decimal SalesAmount { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal AveragePerCover { get; set; }
    public decimal TipsEarned { get; set; }
    public decimal TipPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VoidAmount { get; set; }
    public int VoidCount { get; set; }
    public decimal AverageTurnMinutes { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal SalesPerHour { get; set; }

    /// <summary>Share of covers that took a dessert or a second drink — the upsell signal.</summary>
    public decimal UpsellRate { get; set; }
}

public class TableTurnoverDto
{
    public Guid TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string? SectionName { get; set; }
    public int Seats { get; set; }
    public int TurnCount { get; set; }
    public int CoverCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal RevenuePerSeat { get; set; }
    public decimal AverageTurnMinutes { get; set; }
    public decimal OccupancyPercent { get; set; }
}

public class KitchenPerformanceDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public int ItemCount { get; set; }
    public decimal AveragePrepMinutes { get; set; }
    public decimal MedianPrepMinutes { get; set; }
    public decimal LongestPrepMinutes { get; set; }
    public int SlaBreachCount { get; set; }
    public decimal SlaBreachPercent { get; set; }
    public int RecallCount { get; set; }
    public int RemakeCount { get; set; }
}

public class VoidAuditRowDto
{
    public DateTime OccurredAt { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TableNumber { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? ReasonName { get; set; }
    public string? Note { get; set; }
    public string? StaffName { get; set; }
    public string? ApprovedByName { get; set; }
    public bool WasFired { get; set; }
    public string Kind { get; set; } = "Void";
}

public class WastageSummaryDto
{
    public WastageReason Reason { get; set; }
    public string ReasonName { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal SharePercent { get; set; }
}

public class SalesSummaryReportDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? OutletId { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal PackagingCharge { get; set; }
    public decimal DeliveryFees { get; set; }
    public decimal Tax { get; set; }
    public decimal NetSales { get; set; }
    public decimal Tips { get; set; }
    public decimal Voids { get; set; }
    public decimal Refunds { get; set; }
    public decimal FoodCost { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercent { get; set; }

    public int OrderCount { get; set; }
    public int CheckCount { get; set; }
    public int CoverCount { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal AveragePerCover { get; set; }

    public List<HourlySalesDto> ByHour { get; set; } = [];
    public List<CategorySalesDto> ByCategory { get; set; } = [];
    public List<ItemSalesDto> ByItem { get; set; } = [];
    public List<OrderTypeSalesDto> ByOrderType { get; set; } = [];
    public List<TenderBreakdownDto> ByTender { get; set; } = [];
    public List<DailySalesDto> ByDay { get; set; } = [];
}

public class OrderTypeSalesDto
{
    public OrderType OrderType { get; set; }
    public string OrderTypeName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
    public decimal Amount { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal SharePercent { get; set; }
}

public class DailySalesDto
{
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public int OrderCount { get; set; }
    public int CoverCount { get; set; }
    public decimal AverageCheck { get; set; }
}

// ── Lookups ──────────────────────────────────────────────────────────────────

public class LookupItemDto
{
    public int Value { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Label { get; set; }
}
