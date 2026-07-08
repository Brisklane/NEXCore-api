using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Nexcore.SharedKernel.Helpers;

namespace Sales.Api.Hubs;

/// <summary>
/// Real-time channel for the store POS screen. On connect, a POS client is
/// automatically subscribed to its own branch group (derived from the JWT
/// <c>BranchId</c> claim), so it receives only its store's online-order events.
/// The server pushes <see cref="IPosOrderClient.OnlineOrderReceived"/> and
/// <see cref="IPosOrderClient.PendingOrderCountChanged"/>; clients do not need to
/// call any hub method.
/// </summary>
[Authorize]
public class PosOrderHub : Hub<IPosOrderClient>
{
    private readonly ILogger<PosOrderHub> _logger;

    public PosOrderHub(ILogger<PosOrderHub> logger) => _logger = logger;

    /// <summary>SignalR group name for a store's POS clients.</summary>
    public static string StoreGroup(Guid storeId) => $"pos-store-{storeId}";

    public override async Task OnConnectedAsync()
    {
        if (TryGetBranchId(out var branchId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StoreGroup(branchId));
            _logger.LogDebug("POS client {ConnectionId} joined store group {StoreId}", Context.ConnectionId, branchId);
        }
        else
        {
            _logger.LogWarning("POS client {ConnectionId} connected without a valid BranchId claim", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Derive the caller's branch (store) from JWT claims. The client never supplies
    /// the branch, so it cannot subscribe to another store's order stream.
    /// </summary>
    private bool TryGetBranchId(out Guid branchId)
    {
        branchId = Guid.Empty;
        var user = Context.User;
        if (user is null) return false;

        try
        {
            var (_, branch, _) = TenantContextHelper.ExtractTenantContext(user);
            branchId = branch;
            return branchId != Guid.Empty;
        }
        catch
        {
            return false;
        }
    }
}
