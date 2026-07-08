using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// POS Branch Dashboard — returns real-time session state and today's sales stats per branch.
/// Branch metadata (name, logo, trading name) is owned by Core.Branch; this controller supplies
/// only POS session state so the frontend can render the dashboard cards.
/// </summary>
[ApiController]
[Route("api/sales/pos/dashboard")]
[Authorize]
public class PosDashboardController : ControllerBase
{
    private readonly IPosSessionRepository  _sessions;
    private readonly IPosTerminalRepository _terminals;
    private readonly IPosStoreRepository    _stores;
    private readonly ILogger<PosDashboardController> _logger;

    private const int RecentDaysForChart = 7;

    public PosDashboardController(
        IPosSessionRepository  sessions,
        IPosTerminalRepository terminals,
        IPosStoreRepository    stores,
        ILogger<PosDashboardController> logger)
    {
        _sessions  = sessions;
        _terminals = terminals;
        _stores    = stores;
        _logger    = logger;
    }

    /// <summary>
    /// Get POS session state + today's stats for a single branch.
    /// Call this once per branch card on the dashboard, or use the bulk endpoint.
    /// </summary>
    [HttpGet("branch/{branchId}")]
    [ProducesResponseType(typeof(ApiResponse<PosBranchStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranchStatus(Guid branchId)
    {
        try
        {
            var status = await BuildBranchStatusAsync(branchId);
            return Ok(new ApiResponse<PosBranchStatusDto> { Success = true, Data = status, Message = "Branch POS status retrieved" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS status for branch {BranchId}", branchId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving branch POS status" });
        }
    }

    /// <summary>
    /// Get POS session state + today's stats for multiple branches in one call.
    /// Ideal for the initial dashboard load where all branch cards are rendered at once.
    /// </summary>
    [HttpPost("branches")]
    [ProducesResponseType(typeof(ApiResponse<List<PosBranchStatusDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBranchesStatus([FromBody] PosDashboardBulkRequestDto dto)
    {
        try
        {
            if (dto.BranchIds is null || dto.BranchIds.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "At least one branch ID is required" });

            var results = new List<PosBranchStatusDto>();
            foreach (var id in dto.BranchIds)
                results.Add(await BuildBranchStatusAsync(id));

            return Ok(new ApiResponse<List<PosBranchStatusDto>>
            {
                Success = true,
                Data = results,
                Message = $"POS status retrieved for {results.Count} branch(es)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bulk POS dashboard status");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving branch POS statuses" });
        }
    }

    /// <summary>
    /// Get today's sales status for every terminal across all stores.
    /// Ideal for the admin terminal overview panel.
    /// </summary>
    [HttpGet("terminals")]
    [ProducesResponseType(typeof(ApiResponse<List<PosTerminalStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTerminalsStatus()
    {
        try
        {
            var today     = DateTime.UtcNow.Date;
            // Scope to the active branch (from JWT) so terminals/stores from other branches don't leak in.
            var allTerminals = await _terminals.GetAllByBranchAsync();
            var allStores    = await _stores.GetAllByBranchAsync();

            // Fetch today's sessions once per store to avoid N+1 queries.
            // Use PosStoreId (the store the terminal belongs to) as the grouping key.
            var storeIds = allTerminals.Select(t => t.PosStoreId).Distinct().ToList();
            var sessionsByStore = new Dictionary<Guid, List<Sales.Domain.Entities.PosSession>>();
            foreach (var storeId in storeIds)
                sessionsByStore[storeId] = await _sessions.GetByStoreAndDateForCompanyAsync(storeId, today);

            var result = new List<PosTerminalStatusDto>();
            foreach (var terminal in allTerminals)
            {
                var store = allStores.FirstOrDefault(s => s.Id == terminal.PosStoreId);
                sessionsByStore.TryGetValue(terminal.PosStoreId, out var storeSessions);
                var termSessions = (storeSessions ?? []).Where(s => s.PosTerminalId == terminal.Id).ToList();

                var openSession = termSessions.FirstOrDefault(s => s.Status == PosSessionStatus.Open);
                var status = openSession is not null
                    ? PosBranchSessionStatus.Open
                    : PosBranchSessionStatus.NoSession;

                decimal todayTotal = 0, todayCash = 0, todayCard = 0, todayWallet = 0;
                int todayTxCount = 0;
                foreach (var s in termSessions)
                {
                    todayTotal    += s.NetSalesAmount;
                    todayCash     += s.CashCollected;
                    todayCard     += s.CardCollected;
                    todayWallet   += s.WalletCollected;
                    todayTxCount  += s.TransactionCount;
                }

                result.Add(new PosTerminalStatusDto
                {
                    TerminalId           = terminal.Id,
                    TerminalName         = terminal.TerminalName ?? terminal.TerminalCode,
                    TerminalCode         = terminal.TerminalCode,
                    StoreId              = terminal.PosStoreId,
                    StoreName            = store?.TradingName ?? terminal.PosStoreId.ToString(),
                    IsActive             = terminal.IsActive,
                    Status               = status,
                    ActiveSessionId      = openSession?.Id,
                    ActiveSessionNumber  = openSession?.SessionNumber,
                    ActiveCashierName    = openSession?.PosCashier?.DisplayName,
                    SessionOpenedAt      = openSession?.OpenedAt,
                    OpeningFloat         = openSession?.OpeningFloat ?? 0,
                    TodayTotalSales      = todayTotal,
                    TodayTransactionCount= todayTxCount,
                    TodayCashCollected   = todayCash,
                    TodayCardCollected   = todayCard,
                    TodayWalletCollected = todayWallet,
                });
            }

            return Ok(new ApiResponse<List<PosTerminalStatusDto>>
            {
                Success = true,
                Data    = result.OrderBy(t => t.StoreName).ThenBy(t => t.TerminalName).ToList(),
                Message = $"Terminal statuses retrieved for {result.Count} terminal(s)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving terminal dashboard statuses");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving terminal statuses" });
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task<PosBranchStatusDto> BuildBranchStatusAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        var chartFrom = today.AddDays(-(RecentDaysForChart - 1));

        var openSessions  = await _sessions.GetOpenByBranchAsync(branchId);
        var todaySessions = await _sessions.GetByBranchAndDateAsync(branchId, today);
        var chartSessions = await _sessions.GetByBranchAndDateRangeAsync(branchId, chartFrom, today.AddDays(1));

        // Determine status
        var status = openSessions.Count > 0
            ? PosBranchSessionStatus.Open
            : PosBranchSessionStatus.NoSession;

        // Pick the most recently opened active session for the deeplink
        var primary = openSessions.OrderByDescending(s => s.OpenedAt).FirstOrDefault();

        // Aggregate today's totals across ALL sessions (open + closed)
        decimal todayTotal = 0, todayCash = 0, todayCard = 0, todayWallet = 0;
        int todayTxCount = 0;
        foreach (var s in todaySessions)
        {
            todayTotal += s.NetSalesAmount;
            todayCash += s.CashCollected;
            todayCard += s.CardCollected;
            todayWallet += s.WalletCollected;
            todayTxCount += s.TransactionCount;
        }

        // Build daily chart data (last N days)
        var dailySales = chartSessions
            .GroupBy(s => DateOnly.FromDateTime(s.OpenedAt.Date))
            .OrderBy(g => g.Key)
            .Select(g => new PosDailySalesDto
            {
                Date = g.Key,
                TotalSales = g.Sum(s => s.NetSalesAmount),
                TransactionCount = g.Sum(s => s.TransactionCount),
            })
            .ToList();

        return new PosBranchStatusDto
        {
            BranchId = branchId,
            Status = status,
            OpenSessionCount = openSessions.Count,
            ActiveSessionId = primary?.Id,
            ActiveSessionNumber = primary?.SessionNumber,
            ActiveCashierId = primary?.PosCashierId,
            ActiveCashierName = primary?.PosCashier?.DisplayName,
            SessionOpenedAt = primary?.OpenedAt,
            TodayTotalSales = todayTotal,
            TodayTransactionCount = todayTxCount,
            TodayCashCollected = todayCash,
            TodayCardCollected = todayCard,
            TodayWalletCollected = todayWallet,
            RecentDailySales = dailySales,
        };
    }
}
