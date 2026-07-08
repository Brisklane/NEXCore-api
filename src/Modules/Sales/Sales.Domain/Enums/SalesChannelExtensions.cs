namespace Sales.Domain.Enums;

/// <summary>
/// Helpers for classifying <see cref="SalesChannel"/> values.
/// </summary>
public static class SalesChannelExtensions
{
    /// <summary>
    /// Channels that represent a customer-placed online order that should pop the
    /// "new order" animation + badge on the store's POS screen.
    /// Excludes cashier-keyed PhoneOrder and counter PosWalkIn.
    /// </summary>
    private static readonly HashSet<SalesChannel> OnlineOrderChannels =
    [
        SalesChannel.OnlineApp,
        SalesChannel.OnlineStore,
        SalesChannel.Marketplace,
    ];

    /// <summary>
    /// True when the order arrived through an online customer channel
    /// (customer app, web store, or marketplace).
    /// </summary>
    public static bool IsOnlineOrderChannel(this SalesChannel channel)
        => OnlineOrderChannels.Contains(channel);
}
