using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Replays offline-completed POS sales onto the server.
///
/// Per sale: create the order (the server re-prices it — pricing stays server-authoritative),
/// then settle it through the SAME <see cref="IPosCheckoutService"/> pipeline as an online sale.
/// Keyed on <c>OfflineOrderNumber</c> so a retried batch is idempotent:
///   • already settled → return its real numbers (Duplicate);
///   • created on a prior attempt but not settled → resume the checkout;
///   • new → create + checkout.
///
/// Deliberately does NOT wrap create+checkout in one transaction: checkout fires accounting/stock
/// events (in separate scopes) part-way through, exactly as the online flow does, so wrapping it
/// could commit those while rolling back the sale. The resume path covers a partial failure instead.
/// </summary>
public class PosOfflineSyncService : IPosOfflineSyncService
{
    private readonly ISalesOrderService _orderService;
    private readonly IPosCheckoutService _checkout;
    private readonly ISalesOrderRepository _orders;
    private readonly IPosTransactionRepository _transactions;
    private readonly IPosSessionRepository _sessions;
    private readonly IDocumentSequenceService _sequences;
    private readonly ILogger<PosOfflineSyncService> _logger;

    public PosOfflineSyncService(
        ISalesOrderService orderService,
        IPosCheckoutService checkout,
        ISalesOrderRepository orders,
        IPosTransactionRepository transactions,
        IPosSessionRepository sessions,
        IDocumentSequenceService sequences,
        ILogger<PosOfflineSyncService> logger)
    {
        _orderService = orderService;
        _checkout     = checkout;
        _orders       = orders;
        _transactions = transactions;
        _sessions     = sessions;
        _sequences    = sequences;
        _logger       = logger;
    }

    public async Task<OfflineSyncResultDto> SyncAsync(OfflineSyncRequestDto request)
    {
        var result = new OfflineSyncResultDto();
        foreach (var sale in request.Orders ?? [])
        {
            var item = await SyncOneAsync(sale);
            result.Results.Add(item);
            switch (item.Status)
            {
                case OfflineSyncStatus.Synced:    result.SyncedCount++; break;
                case OfflineSyncStatus.Duplicate: result.DuplicateCount++; break;
                default:                          result.FailedCount++; break;
            }
        }

        _logger.LogInformation(
            "Offline sync from device {DeviceId}: {Synced} synced, {Dup} duplicate, {Failed} failed",
            request.DeviceId, result.SyncedCount, result.DuplicateCount, result.FailedCount);

        return result;
    }

    private async Task<OfflineSyncResultItem> SyncOneAsync(OfflineSaleDto sale)
    {
        var uid = sale.OfflineOrderNumber;
        if (string.IsNullOrWhiteSpace(uid))
            return Fail(string.Empty, "Missing OfflineOrderNumber");

        try
        {
            var existing = await _orders.GetByOfflineNumberAsync(uid);

            // Fully settled already → idempotent replay, return its real numbers.
            if (existing?.ClosingPosTransactionId is Guid settledTxId)
            {
                var settled = await _transactions.GetByIdAsync(settledTxId);
                return new OfflineSyncResultItem
                {
                    OfflineOrderNumber = uid,
                    Status             = OfflineSyncStatus.Duplicate,
                    OrderNumber        = existing.OrderNumber,
                    ReceiptNumber      = settled?.ReceiptNumber,
                    TransactionId      = settledTxId,
                    DeviceTotal        = sale.DeviceTotal,
                    ServerTotal        = existing.TotalAmount,
                    Variance           = existing.TotalAmount - sale.DeviceTotal,
                };
            }

            // Resolve the session BEFORE creating the order: an online-opened session id is used
            // as-is; a session opened offline is reused (if already open) or created now. The order
            // must reference the resolved server-side session — its device-local OriginPosSessionId
            // does not exist in sales.PosSessions, so inserting it would break the FK constraint.
            var sessionId  = await ResolveSessionAsync(sale);
            var terminalId = sale.OfflineSession?.TerminalId ?? sale.Order.OriginPosTerminalId ?? Guid.Empty;
            var cashierId  = sale.OfflineSession?.CashierId  ?? sale.Order.OriginPosCashierId  ?? Guid.Empty;
            var storeId    = sale.OfflineSession?.StoreId    ?? sale.Order.OriginBranchId      ?? Guid.Empty;

            Guid orderId;
            if (existing != null)
            {
                // Created on a prior attempt but not settled — resume.
                orderId = existing.Id;
            }
            else
            {
                sale.Order.OfflineOrderNumber = uid;
                sale.Order.OriginPosSessionId = sessionId == Guid.Empty ? null : sessionId;
                var created = await _orderService.CreateAsync(sale.Order);
                orderId = created.Id;
            }

            var checkout = await _checkout.CheckoutAsync(new PosCheckoutDto
            {
                SalesOrderId  = orderId,
                PosSessionId  = sessionId,
                PosTerminalId = terminalId,
                PosStoreId    = storeId,
                PosCashierId  = cashierId,
                Tenders       = sale.Tenders,
                AllowCredit   = sale.AllowCredit,
                // Offline sales already completed on the device — never block them on re-settlement.
                SkipStockCheck = true,
                Notes         = sale.Order.Notes,
            });

            var variance = checkout.TotalAmount - sale.DeviceTotal;
            if (Math.Abs(variance) >= 0.005m)
                _logger.LogWarning(
                    "Offline order {Uid} re-priced on sync: device {DeviceTotal:N2} vs server {ServerTotal:N2} (variance {Variance:N2})",
                    uid, sale.DeviceTotal, checkout.TotalAmount, variance);

            return new OfflineSyncResultItem
            {
                OfflineOrderNumber = uid,
                Status             = OfflineSyncStatus.Synced,
                OrderNumber        = checkout.OrderNumber,
                ReceiptNumber      = checkout.ReceiptNumber,
                TransactionId      = checkout.Transaction.Id,
                DeviceTotal        = sale.DeviceTotal,
                ServerTotal        = checkout.TotalAmount,
                Variance           = variance,
            };
        }
        catch (Exception ex)
        {
            // Unwrap EF DbUpdateException so the real constraint/SQL error reaches the client.
            var message = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Offline sync failed for order {Uid}: {Error}", uid, message);
            return Fail(uid, message);
        }
    }

    /// <summary>
    /// Returns the POS session id to settle against. An online-opened session id is valid and
    /// returned as-is. A session opened offline is reused (if one is already open on this terminal
    /// — one open session per terminal, idempotent across retries / a batch) or opened now.
    /// </summary>
    private async Task<Guid> ResolveSessionAsync(OfflineSaleDto sale)
    {
        if (sale.OfflineSession is null)
            return sale.Order.OriginPosSessionId ?? Guid.Empty;

        var os = sale.OfflineSession;
        var cashierId  = os.CashierId  ?? sale.Order.OriginPosCashierId  ?? Guid.Empty;
        var terminalId = os.TerminalId ?? sale.Order.OriginPosTerminalId ?? Guid.Empty;
        if (cashierId == Guid.Empty || terminalId == Guid.Empty)
            return sale.Order.OriginPosSessionId ?? Guid.Empty;

        // One open session per terminal: reuse the terminal's open session (whoever opened it) so a
        // synced offline sale books against the same drawer instead of breaching the unique index.
        var open = await _sessions.GetOpenSessionByTerminalAsync(terminalId);
        if (open != null) return open.Id;

        var seq = await _sequences.GetNextNumberAsync(DocumentType.PosSession);
        var session = new PosSession
        {
            SessionNumber = seq.Code,
            Code          = seq.Code,
            CodeInt       = seq.CodeInt,
            PosTerminalId = terminalId,
            PosCashierId  = cashierId,
            OpeningFloat  = os.OpeningFloat,
            OpeningNotes  = $"Opened offline ({os.OfflineSessionNumber})",
            Status        = PosSessionStatus.Open,
            OpenedAt      = os.OpenedAt ?? DateTime.UtcNow,
        };
        await _sessions.AddAsync(session);
        await _sessions.SaveChangesAsync();
        _logger.LogInformation(
            "Offline sync opened session {SessionNumber} for cashier {CashierId} on terminal {TerminalId} (device ref {Ref})",
            session.SessionNumber, cashierId, terminalId, os.OfflineSessionNumber);
        return session.Id;
    }

    private static OfflineSyncResultItem Fail(string uid, string error) => new()
    {
        OfflineOrderNumber = uid,
        Status             = OfflineSyncStatus.Failed,
        Error              = error,
    };
}
