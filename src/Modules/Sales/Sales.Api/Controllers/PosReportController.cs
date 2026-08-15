using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Implementations;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// POS reporting — shift reads (X/Z) and period analysis.
///
/// Everything here is read-only. A Z read in particular does **not** close the session:
/// closing is <c>PosCashier/check-out</c>, and keeping them apart means a manager can take
/// a Z read of yesterday's shift without touching anything.
/// </summary>
[ApiController]
[Route("api/sales/PosReport")]
[Authorize]
public class PosReportController : ControllerBase
{
    private readonly IPosReportRepository _reports;
    private readonly IPosStoreRepository _stores;
    private readonly ILogger<PosReportController> _logger;

    /// <summary>Guard rail on "top N" — a report is for reading, not for exporting the catalogue.</summary>
    private const int MaxTopProducts = 200;

    /// <summary>A range longer than this is almost always a mistyped date.</summary>
    private const int MaxRangeDays = 400;

    public PosReportController(
        IPosReportRepository reports,
        IPosStoreRepository stores,
        ILogger<PosReportController> logger)
    {
        _reports = reports;
        _stores = stores;
        _logger = logger;
    }

    // ── Shift reads ───────────────────────────────────────────────────────────

    /// <summary>
    /// X read — a mid-shift snapshot of an open session. Changes nothing.
    /// </summary>
    [HttpGet("session/{sessionId}/x")]
    [ProducesResponseType(typeof(ApiResponse<PosShiftReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetXRead(Guid sessionId) => BuildShiftReport(sessionId, "X");

    /// <summary>
    /// Z read — the end-of-shift report. Allowed on an open session, but the result is then
    /// marked provisional so nobody files a figure that can still change.
    /// </summary>
    [HttpGet("session/{sessionId}/z")]
    [ProducesResponseType(typeof(ApiResponse<PosShiftReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetZRead(Guid sessionId) => BuildShiftReport(sessionId, "Z");

    private async Task<IActionResult> BuildShiftReport(Guid sessionId, string kind)
    {
        try
        {
            var session = await _reports.GetSessionAsync(sessionId);
            if (session is null)
                return NotFound(new ApiErrorResponse { Message = "Session not found" });

            var totals = await _reports.GetSessionTotalsAsync(sessionId);
            var tenders = await _reports.GetSessionTendersAsync(sessionId);
            var items = await _reports.GetSessionItemCountAsync(sessionId);
            var movements = await _reports.GetCashMovementsAsync(sessionId);
            var (cashSales, cashRefunds) = await _reports.GetSessionCashAsync(sessionId);

            // Opening and closing float are recorded on the session itself; counting the
            // matching movement rows too would double them into the drawer.
            var cashIn = movements
                .Where(m => m.MovementType == PosCashMovementType.CashIn)
                .Sum(m => m.Amount);
            var cashOut = movements
                .Where(m => m.MovementType is PosCashMovementType.CashOut
                                           or PosCashMovementType.SafeDrop
                                           or PosCashMovementType.PettyCash)
                .Sum(m => m.Amount);

            var expectedCash = session.OpeningFloat + cashSales - cashRefunds + cashIn - cashOut;
            var isOpen = session.Status == PosSessionStatus.Open;

            var storeId = session.PosTerminal?.PosStoreId;
            string? storeName = null;
            if (storeId.HasValue)
                storeName = (await _stores.GetByIdAsync(storeId.Value))?.TradingName;

            var report = new PosShiftReportDto
            {
                SessionId = session.Id,
                SessionNumber = session.SessionNumber,
                SessionStatus = session.Status,
                IsProvisional = isOpen,

                CashierId = session.PosCashierId,
                CashierName = session.PosCashier?.DisplayName,
                TerminalId = session.PosTerminalId,
                TerminalName = session.PosTerminal?.TerminalName,
                StoreId = storeId,
                StoreName = storeName,

                OpenedAt = session.OpenedAt,
                ClosedAt = session.ClosedAt,
                GeneratedAt = DateTime.UtcNow,

                GrossSales = totals.GrossSales,
                Refunds = totals.Refunds,
                Discounts = totals.Discounts,
                Tax = totals.Tax,
                NetSales = totals.NetSales,
                SaleCount = totals.SaleCount,
                RefundCount = totals.RefundCount,
                VoidCount = totals.VoidCount,
                AverageBasket = totals.SaleCount > 0 ? totals.GrossSales / totals.SaleCount : 0m,
                ItemsSold = items,

                Tenders = tenders,

                OpeningFloat = session.OpeningFloat,
                CashSales = cashSales,
                CashRefunds = cashRefunds,
                CashIn = cashIn,
                CashOut = cashOut,
                ExpectedCash = expectedCash,
                // While the drawer is still trading there is nothing counted to compare against,
                // and printing a variance mid-shift invites someone to "correct" a live till.
                CountedCash = isOpen ? null : session.ClosingFloat,
                CashVariance = isOpen ? null : session.ClosingFloat - expectedCash,
                CashMovements = movements.Select(m => new PosCashMovementSummaryDto
                {
                    MovementType = m.MovementType,
                    MovementName = PosReportRepository.MovementName(m.MovementType),
                    Amount = m.Amount,
                    Reason = m.Reason,
                    MovementDate = m.MovementDate,
                }).ToList(),

                RecordedSalesTotal = session.TotalSalesAmount,
                RecordedCashCollected = session.CashCollected,
                RecordedTransactionCount = session.TransactionCount,
            };

            // The session's counters are maintained incrementally at checkout and are not
            // adjusted by refunds, so a mismatch is expected on a day with returns. Surfacing
            // it beats silently preferring one number over the other.
            report.CountersDisagree =
                Math.Abs(session.TotalSalesAmount - totals.GrossSales) > 0.01m
                || Math.Abs(session.CashCollected - cashSales) > 0.01m
                || session.TransactionCount != totals.SaleCount;

            return Ok(new ApiResponse<PosShiftReportDto>
            {
                Success = true,
                Data = report,
                Message = $"{kind} read generated for session {session.SessionNumber}",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building {Kind} read for session {SessionId}", kind, sessionId);
            return StatusCode(500, new ApiErrorResponse { Message = $"Error generating {kind} read" });
        }
    }

    // ── Period reports ────────────────────────────────────────────────────────

    /// <summary>Headline figures for a date range.</summary>
    [HttpGet("sales-summary")]
    [ProducesResponseType(typeof(ApiResponse<PosSalesSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSalesSummary(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? storeId = null)
    {
        var bad = ValidateRange(ref from, ref to);
        if (bad is not null) return BadRequest(bad);

        try
        {
            var totals = await _reports.GetSummaryAsync(from, to, storeId);
            var items = await _reports.GetItemsSoldAsync(from, to, storeId);
            var sessions = await _reports.GetSessionCountAsync(from, to, storeId);

            return Ok(new ApiResponse<PosSalesSummaryDto>
            {
                Success = true,
                Data = new PosSalesSummaryDto
                {
                    From = from,
                    To = to,
                    StoreId = storeId,
                    GrossSales = totals.GrossSales,
                    Refunds = totals.Refunds,
                    Discounts = totals.Discounts,
                    Tax = totals.Tax,
                    NetSales = totals.NetSales,
                    SaleCount = totals.SaleCount,
                    RefundCount = totals.RefundCount,
                    ItemsSold = items,
                    AverageBasket = totals.SaleCount > 0 ? totals.GrossSales / totals.SaleCount : 0m,
                    SessionCount = sessions,
                },
                Message = "Sales summary retrieved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building POS sales summary");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating sales summary" });
        }
    }

    /// <summary>What sold, best first. Variants are reported separately from their parent item.</summary>
    [HttpGet("by-product")]
    [ProducesResponseType(typeof(ApiResponse<List<PosProductSalesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? storeId = null, [FromQuery] int top = 50)
    {
        var bad = ValidateRange(ref from, ref to);
        if (bad is not null) return BadRequest(bad);

        try
        {
            var rows = await _reports.GetByProductAsync(from, to, storeId, Math.Clamp(top, 1, MaxTopProducts));
            return Ok(new ApiResponse<List<PosProductSalesDto>> { Success = true, Data = rows, Message = "Product sales retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building POS product sales report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating product sales" });
        }
    }

    /// <summary>Who sold it.</summary>
    [HttpGet("by-cashier")]
    [ProducesResponseType(typeof(ApiResponse<List<PosCashierSalesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCashier(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? storeId = null)
    {
        var bad = ValidateRange(ref from, ref to);
        if (bad is not null) return BadRequest(bad);

        try
        {
            var rows = await _reports.GetByCashierAsync(from, to, storeId);
            return Ok(new ApiResponse<List<PosCashierSalesDto>> { Success = true, Data = rows, Message = "Cashier sales retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building POS cashier sales report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating cashier sales" });
        }
    }

    /// <summary>When it sold. All 24 hours are returned, including the quiet ones.</summary>
    [HttpGet("by-hour")]
    [ProducesResponseType(typeof(ApiResponse<List<PosHourlySalesDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByHour(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? storeId = null)
    {
        var bad = ValidateRange(ref from, ref to);
        if (bad is not null) return BadRequest(bad);

        try
        {
            var rows = await _reports.GetByHourAsync(from, to, storeId);
            return Ok(new ApiResponse<List<PosHourlySalesDto>> { Success = true, Data = rows, Message = "Hourly sales retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building POS hourly sales report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating hourly sales" });
        }
    }

    /// <summary>How it was paid for.</summary>
    [HttpGet("tender-mix")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTenderTotalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenderMix(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] Guid? storeId = null)
    {
        var bad = ValidateRange(ref from, ref to);
        if (bad is not null) return BadRequest(bad);

        try
        {
            var rows = await _reports.GetTenderMixAsync(from, to, storeId);
            return Ok(new ApiResponse<List<PosTenderTotalDto>> { Success = true, Data = rows, Message = "Tender mix retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building POS tender mix report");
            return StatusCode(500, new ApiErrorResponse { Message = "Error generating tender mix" });
        }
    }

    /// <summary>
    /// Normalises the range in place: a bare date for <paramref name="to"/> would otherwise
    /// mean midnight and silently drop the whole of the last day's trading.
    /// </summary>
    private static ApiErrorResponse? ValidateRange(ref DateTime from, ref DateTime to)
    {
        if (from == default || to == default)
            return new ApiErrorResponse { Message = "Both 'from' and 'to' are required" };

        if (to.TimeOfDay == TimeSpan.Zero)
            to = to.Date.AddDays(1).AddTicks(-1);

        if (to < from)
            return new ApiErrorResponse { Message = "'to' cannot be earlier than 'from'" };

        if ((to - from).TotalDays > MaxRangeDays)
            return new ApiErrorResponse { Message = $"Range cannot exceed {MaxRangeDays} days" };

        return null;
    }
}
