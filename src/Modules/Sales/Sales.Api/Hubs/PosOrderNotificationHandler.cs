using Microsoft.AspNetCore.SignalR;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Enums;

namespace Sales.Api.Hubs;

/// <summary>
/// Bridges in-process order events to the POS order hub. Computes the fresh pending
/// count once (single source of truth = <see cref="ISalesOrderService"/>) and pushes
/// to the store's connected POS clients.
/// </summary>
public class PosOrderNotificationHandler :
    IEventHandler<OnlineOrderPlacedEvent>,
    IEventHandler<OnlineOrderQueueChangedEvent>
{
    private readonly IHubContext<PosOrderHub, IPosOrderClient> _hub;
    private readonly ISalesOrderService _orders;
    private readonly ILogger<PosOrderNotificationHandler> _logger;

    public PosOrderNotificationHandler(
        IHubContext<PosOrderHub, IPosOrderClient> hub,
        ISalesOrderService orders,
        ILogger<PosOrderNotificationHandler> logger)
    {
        _hub     = hub;
        _orders  = orders;
        _logger  = logger;
    }

    public async Task HandleAsync(OnlineOrderPlacedEvent e, CancellationToken cancellationToken = default)
    {
        var pending = await SafeCountAsync(e.StoreId);

        await _hub.Clients.Group(PosOrderHub.StoreGroup(e.StoreId)).OnlineOrderReceived(new OnlineOrderNotificationDto
        {
            OrderId         = e.OrderId,
            OrderNumber     = e.OrderNumber,
            StoreId         = e.StoreId,
            ContactId       = e.ContactId,
            ContactName     = e.ContactName,
            ItemCount       = e.ItemCount,
            TotalAmount     = e.TotalAmount,
            CurrencyCode    = e.CurrencyCode,
            SalesChannel    = (SalesChannel)e.SalesChannel,
            FulfillmentType = (FulfillmentType)e.FulfillmentType,
            PlacedAt        = e.PlacedAt,
            PendingCount    = pending,
        });

        _logger.LogInformation(
            "POS: pushed OnlineOrderReceived for {OrderNumber} to store {StoreId} (pending={Pending})",
            e.OrderNumber, e.StoreId, pending);
    }

    public async Task HandleAsync(OnlineOrderQueueChangedEvent e, CancellationToken cancellationToken = default)
    {
        var pending = await SafeCountAsync(e.StoreId);

        await _hub.Clients.Group(PosOrderHub.StoreGroup(e.StoreId)).PendingOrderCountChanged(new PosPendingOrdersDto
        {
            StoreId      = e.StoreId,
            PendingCount = pending,
        });

        _logger.LogDebug("POS: pushed PendingOrderCountChanged to store {StoreId} (pending={Pending})", e.StoreId, pending);
    }

    private async Task<int> SafeCountAsync(Guid storeId)
    {
        try
        {
            return await _orders.GetPendingOnlineOrderCountAsync(storeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POS: failed to compute pending online-order count for store {StoreId}", storeId);
            return 0;
        }
    }
}
