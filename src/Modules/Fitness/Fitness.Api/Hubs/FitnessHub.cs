using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fitness.Api.Hubs;

/// <summary>
/// What a connected club screen can be told about.
///
/// Typed so a rename of a method here is a compile error in the server code that raises it,
/// rather than a screen that silently stops updating.
/// </summary>
public interface IFitnessClient
{
    /// <summary>Someone came in or went out — the front desk and the occupancy dial refresh.</summary>
    Task CheckInChanged(object payload);

    /// <summary>A booking was made, cancelled, promoted off the waitlist or marked attended.</summary>
    Task BookingChanged(object payload);

    /// <summary>A class was cancelled, moved, or had its instructor substituted.</summary>
    Task ClassChanged(object payload);

    /// <summary>A door decision worth showing on the desk — usually a refusal a member will query.</summary>
    Task AccessDecision(object payload);

    /// <summary>A member's record changed in a way the desk needs to see (status, balance, alerts).</summary>
    Task MemberChanged(object payload);

    /// <summary>A controller went offline or came back.</summary>
    Task ControllerChanged(object payload);

    /// <summary>An alert for whoever is on shift — a failed check, an incident, a cash variance.</summary>
    Task DeskAlert(object payload);
}

/// <summary>
/// Live sync for the screens that cannot afford to be stale: the front desk, the kiosk, the class
/// roster and the trainer's diary.
///
/// Clients subscribe to a club, and a member's own app subscribes to just their own record, so a
/// club with two thousand members does not push every check-in to every phone. Group names are
/// built from the tenant's company id as well as the club id — two companies could hold the same
/// club id in a shared deployment and must never see each other's members.
/// </summary>
[Authorize]
public class FitnessHub : Hub<IFitnessClient>
{
    public static string ClubGroup(Guid companyId, Guid clubId) => $"fit:{companyId:N}:{clubId:N}";

    public static string MemberGroup(Guid companyId, Guid memberId) => $"fit-m:{companyId:N}:{memberId:N}";

    public static string RosterGroup(Guid companyId, Guid occurrenceId) => $"fit-r:{companyId:N}:{occurrenceId:N}";

    private Guid CompanyId =>
        Guid.TryParse(Context.User?.FindFirst("CompanyId")?.Value, out var id)
            ? id
            : throw new HubException("No company context on this connection.");

    public async Task JoinClub(Guid clubId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, ClubGroup(CompanyId, clubId));

    public async Task LeaveClub(Guid clubId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, ClubGroup(CompanyId, clubId));

    public async Task JoinMember(Guid memberId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, MemberGroup(CompanyId, memberId));

    public async Task LeaveMember(Guid memberId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, MemberGroup(CompanyId, memberId));

    /// <summary>An instructor watching one class's roster fill up on the studio screen.</summary>
    public async Task JoinRoster(Guid occurrenceId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, RosterGroup(CompanyId, occurrenceId));

    public async Task LeaveRoster(Guid occurrenceId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, RosterGroup(CompanyId, occurrenceId));
}

/// <summary>
/// How the rest of the module pushes to connected screens without taking a dependency on SignalR.
/// </summary>
public interface IFitnessNotifier
{
    Task CheckInChangedAsync(Guid companyId, Guid clubId, object payload);
    Task BookingChangedAsync(Guid companyId, Guid clubId, Guid occurrenceId, object payload);
    Task ClassChangedAsync(Guid companyId, Guid clubId, object payload);
    Task AccessDecisionAsync(Guid companyId, Guid clubId, object payload);
    Task MemberChangedAsync(Guid companyId, Guid clubId, Guid memberId, object payload);
    Task ControllerChangedAsync(Guid companyId, Guid clubId, object payload);
    Task DeskAlertAsync(Guid companyId, Guid clubId, object payload);
}

/// <inheritdoc />
public class FitnessNotifier(IHubContext<FitnessHub, IFitnessClient> hub) : IFitnessNotifier
{
    public Task CheckInChangedAsync(Guid companyId, Guid clubId, object payload)
        => hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).CheckInChanged(payload);

    public async Task BookingChangedAsync(Guid companyId, Guid clubId, Guid occurrenceId, object payload)
    {
        // The studio screen watching this one class and the desk watching the whole club both
        // need it; sending to both beats making the desk subscribe to every occurrence.
        await hub.Clients.Group(FitnessHub.RosterGroup(companyId, occurrenceId)).BookingChanged(payload);
        await hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).BookingChanged(payload);
    }

    public Task ClassChangedAsync(Guid companyId, Guid clubId, object payload)
        => hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).ClassChanged(payload);

    public Task AccessDecisionAsync(Guid companyId, Guid clubId, object payload)
        => hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).AccessDecision(payload);

    public async Task MemberChangedAsync(Guid companyId, Guid clubId, Guid memberId, object payload)
    {
        await hub.Clients.Group(FitnessHub.MemberGroup(companyId, memberId)).MemberChanged(payload);
        await hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).MemberChanged(payload);
    }

    public Task ControllerChangedAsync(Guid companyId, Guid clubId, object payload)
        => hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).ControllerChanged(payload);

    public Task DeskAlertAsync(Guid companyId, Guid clubId, object payload)
        => hub.Clients.Group(FitnessHub.ClubGroup(companyId, clubId)).DeskAlert(payload);
}
