using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Distribution.Api.Hubs;

/// <summary>
/// What a connected distribution screen can be told about.
///
/// Typed so a rename here is a compile error in the server code that raises it, rather than a
/// screen that silently stops updating.
/// </summary>
public interface IDistributionClient
{
    /// <summary>An order was created, approved, allocated, picked or dispatched.</summary>
    Task OrderChanged(object payload);

    /// <summary>A pick wave or one of its tasks moved along.</summary>
    Task WaveChanged(object payload);

    /// <summary>A trip departed, arrived somewhere, or closed a stop.</summary>
    Task TripChanged(object payload);

    /// <summary>A rep started or closed a day, or checked in or out of an outlet.</summary>
    Task FieldActivity(object payload);

    /// <summary>A settlement was opened, submitted, approved or closed.</summary>
    Task SettlementChanged(object payload);

    /// <summary>Something needs a human — the exception queue should refresh.</summary>
    Task ExceptionRaised(object payload);
}

/// <summary>
/// Live sync for the surfaces that cannot afford to be stale: the dispatch desk, the trip board
/// and the settlement queue.
///
/// Clients subscribe to a warehouse, a territory or a route so a national deployment does not
/// push every carton scan to every screen. Group names are built from the tenant's company id as
/// well as the scope id — two companies could hold the same warehouse id in a shared deployment
/// and must never see each other's traffic.
/// </summary>
[Authorize]
public class DistributionHub : Hub<IDistributionClient>
{
    public static string WarehouseGroup(Guid companyId, Guid warehouseId) => $"dst-wh:{companyId:N}:{warehouseId:N}";

    public static string TerritoryGroup(Guid companyId, Guid territoryId) => $"dst-tr:{companyId:N}:{territoryId:N}";

    public static string RouteGroup(Guid companyId, Guid routeId) => $"dst-rt:{companyId:N}:{routeId:N}";

    /// <summary>Everyone in the company — used only for exceptions, which are rare and important.</summary>
    public static string CompanyGroup(Guid companyId) => $"dst:{companyId:N}";

    private Guid CompanyId =>
        Guid.TryParse(Context.User?.FindFirst("CompanyId")?.Value, out var id)
            ? id
            : throw new HubException("No company context on this connection.");

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(CompanyId));
        await base.OnConnectedAsync();
    }

    public async Task JoinWarehouse(Guid warehouseId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, WarehouseGroup(CompanyId, warehouseId));

    public async Task LeaveWarehouse(Guid warehouseId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, WarehouseGroup(CompanyId, warehouseId));

    public async Task JoinTerritory(Guid territoryId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, TerritoryGroup(CompanyId, territoryId));

    public async Task LeaveTerritory(Guid territoryId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, TerritoryGroup(CompanyId, territoryId));

    public async Task JoinRoute(Guid routeId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, RouteGroup(CompanyId, routeId));

    public async Task LeaveRoute(Guid routeId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, RouteGroup(CompanyId, routeId));
}

/// <summary>
/// Server-side push, kept behind an interface so the services never take a SignalR dependency.
///
/// Every method is fire-and-forget from the caller's point of view: a screen that misses an
/// update refreshes on its next poll, and a hub that is momentarily unreachable must never fail
/// the order that triggered it.
/// </summary>
public interface IDistributionNotifier
{
    Task OrderChangedAsync(Guid companyId, Guid? warehouseId, Guid? routeId, object payload);
    Task WaveChangedAsync(Guid companyId, Guid? warehouseId, object payload);
    Task TripChangedAsync(Guid companyId, Guid? territoryId, Guid? routeId, object payload);
    Task FieldActivityAsync(Guid companyId, Guid? territoryId, Guid? routeId, object payload);
    Task SettlementChangedAsync(Guid companyId, Guid? territoryId, object payload);
    Task ExceptionRaisedAsync(Guid companyId, object payload);
}

/// <inheritdoc />
public class DistributionNotifier(
    IHubContext<DistributionHub, IDistributionClient> hub,
    ILogger<DistributionNotifier> logger) : IDistributionNotifier
{
    public Task OrderChangedAsync(Guid companyId, Guid? warehouseId, Guid? routeId, object payload)
        => SendAsync(Targets(companyId, warehouseId, null, routeId), c => c.OrderChanged(payload));

    public Task WaveChangedAsync(Guid companyId, Guid? warehouseId, object payload)
        => SendAsync(Targets(companyId, warehouseId, null, null), c => c.WaveChanged(payload));

    public Task TripChangedAsync(Guid companyId, Guid? territoryId, Guid? routeId, object payload)
        => SendAsync(Targets(companyId, null, territoryId, routeId), c => c.TripChanged(payload));

    public Task FieldActivityAsync(Guid companyId, Guid? territoryId, Guid? routeId, object payload)
        => SendAsync(Targets(companyId, null, territoryId, routeId), c => c.FieldActivity(payload));

    public Task SettlementChangedAsync(Guid companyId, Guid? territoryId, object payload)
        => SendAsync(Targets(companyId, null, territoryId, null), c => c.SettlementChanged(payload));

    public Task ExceptionRaisedAsync(Guid companyId, object payload)
        => SendAsync([DistributionHub.CompanyGroup(companyId)], c => c.ExceptionRaised(payload));

    private static List<string> Targets(Guid companyId, Guid? warehouseId, Guid? territoryId, Guid? routeId)
    {
        var groups = new List<string>();
        if (warehouseId.HasValue) groups.Add(DistributionHub.WarehouseGroup(companyId, warehouseId.Value));
        if (territoryId.HasValue) groups.Add(DistributionHub.TerritoryGroup(companyId, territoryId.Value));
        if (routeId.HasValue) groups.Add(DistributionHub.RouteGroup(companyId, routeId.Value));

        // Nothing scoped means nobody would hear it; fall back to the company so the update is
        // still delivered rather than silently dropped.
        if (groups.Count == 0) groups.Add(DistributionHub.CompanyGroup(companyId));

        return groups;
    }

    private async Task SendAsync(List<string> groups, Func<IDistributionClient, Task> send)
    {
        foreach (var group in groups)
        {
            try
            {
                await send(hub.Clients.Group(group));
            }
            catch (Exception ex)
            {
                // A screen that misses an update refreshes on its next poll. Failing the caller's
                // transaction because a websocket blinked would be far worse.
                logger.LogWarning(ex, "Could not push a Distribution update to group {Group}", group);
            }
        }
    }
}
