using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Restaurant.Api.Hubs;

/// <summary>
/// What a connected restaurant screen can be told about.
///
/// Typed so a rename of a method here is a compile error in the server code that raises it,
/// rather than a screen that silently stops updating.
/// </summary>
public interface IRestaurantClient
{
    /// <summary>A table changed state — the floor plan should refresh that tile.</summary>
    Task TableChanged(object payload);

    /// <summary>An order was opened, added to, fired or closed.</summary>
    Task OrderChanged(object payload);

    /// <summary>A kitchen ticket was created or moved along — the KDS should refresh.</summary>
    Task TicketChanged(object payload);

    /// <summary>An item was taken off or put back on the menu.</summary>
    Task AvailabilityChanged(object payload);

    /// <summary>A party joined, was called, or was seated.</summary>
    Task WaitlistChanged(object payload);
}

/// <summary>
/// Live sync for the three screens that cannot afford to be stale: the floor plan, the order pad
/// and the kitchen display.
///
/// Clients subscribe to an outlet, and kitchen screens additionally to their own station, so a
/// six-station kitchen does not push every grill ticket to the bar screen. Group names are built
/// from the tenant's company id as well as the outlet id — two companies could hold the same
/// outlet id in a shared deployment and must never see each other's tickets.
/// </summary>
[Authorize]
public class RestaurantHub : Hub<IRestaurantClient>
{
    public static string OutletGroup(Guid companyId, Guid outletId) => $"rst:{companyId:N}:{outletId:N}";

    public static string StationGroup(Guid companyId, Guid stationId) => $"rst-st:{companyId:N}:{stationId:N}";

    private Guid CompanyId =>
        Guid.TryParse(Context.User?.FindFirst("CompanyId")?.Value, out var id)
            ? id
            : throw new HubException("No company context on this connection.");

    public async Task JoinOutlet(Guid outletId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, OutletGroup(CompanyId, outletId));

    public async Task LeaveOutlet(Guid outletId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, OutletGroup(CompanyId, outletId));

    public async Task JoinStation(Guid stationId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, StationGroup(CompanyId, stationId));

    public async Task LeaveStation(Guid stationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, StationGroup(CompanyId, stationId));
}

/// <summary>
/// How the rest of the module pushes to connected screens without taking a dependency on SignalR.
/// </summary>
public interface IRestaurantNotifier
{
    Task TableChangedAsync(Guid companyId, Guid outletId, object payload);
    Task OrderChangedAsync(Guid companyId, Guid outletId, object payload);
    Task TicketChangedAsync(Guid companyId, Guid outletId, Guid stationId, object payload);
    Task AvailabilityChangedAsync(Guid companyId, Guid outletId, object payload);
    Task WaitlistChangedAsync(Guid companyId, Guid outletId, object payload);
}

/// <inheritdoc />
public class RestaurantNotifier(IHubContext<RestaurantHub, IRestaurantClient> hub) : IRestaurantNotifier
{
    public Task TableChangedAsync(Guid companyId, Guid outletId, object payload)
        => hub.Clients.Group(RestaurantHub.OutletGroup(companyId, outletId)).TableChanged(payload);

    public Task OrderChangedAsync(Guid companyId, Guid outletId, object payload)
        => hub.Clients.Group(RestaurantHub.OutletGroup(companyId, outletId)).OrderChanged(payload);

    public async Task TicketChangedAsync(Guid companyId, Guid outletId, Guid stationId, object payload)
    {
        // The station screen and the pass both need it; the pass is subscribed at outlet level,
        // so send to both groups rather than making the expo screen subscribe to every station.
        await hub.Clients.Group(RestaurantHub.StationGroup(companyId, stationId)).TicketChanged(payload);
        await hub.Clients.Group(RestaurantHub.OutletGroup(companyId, outletId)).TicketChanged(payload);
    }

    public Task AvailabilityChangedAsync(Guid companyId, Guid outletId, object payload)
        => hub.Clients.Group(RestaurantHub.OutletGroup(companyId, outletId)).AvailabilityChanged(payload);

    public Task WaitlistChangedAsync(Guid companyId, Guid outletId, object payload)
        => hub.Clients.Group(RestaurantHub.OutletGroup(companyId, outletId)).WaitlistChanged(payload);
}
