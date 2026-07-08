using Sales.Application.DTOs;

namespace Sales.Api.Hubs;

/// <summary>
/// Strongly-typed methods the POS order hub invokes on connected POS clients.
/// The POS app (WPF / web / Android) implements these to drive the screen.
/// </summary>
public interface IPosOrderClient
{
    /// <summary>
    /// A new online order arrived for this store. The POS screen should play the
    /// arrival animation and set the "Orders" badge to <c>dto.PendingCount</c>.
    /// </summary>
    Task OnlineOrderReceived(OnlineOrderNotificationDto dto);

    /// <summary>
    /// The pending online-order count changed without a new arrival (e.g. an order
    /// was accepted/rejected on another terminal). The POS screen should update the
    /// "Orders" badge to <c>dto.PendingCount</c> — no animation.
    /// </summary>
    Task PendingOrderCountChanged(PosPendingOrdersDto dto);
}
