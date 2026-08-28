using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RealEstate.Api.Hubs;

/// <summary>
/// What a connected Real Estate screen can be told about.
///
/// Typed, so renaming a method here breaks the build in the code that raises it rather than
/// leaving a screen that quietly stops updating — which is the failure nobody notices for weeks.
/// </summary>
public interface IRealEstateClient
{
    /// <summary>A unit changed hands, was held, released, blocked or repriced.</summary>
    Task InventoryChanged(object payload);

    /// <summary>A booking was made, confirmed, cancelled or transferred.</summary>
    Task BookingChanged(object payload);

    /// <summary>Money moved: a receipt was posted, a cheque cleared or bounced, a refund went out.</summary>
    Task MoneyChanged(object payload);

    /// <summary>A lead arrived, was assigned, or breached its response promise.</summary>
    Task LeadChanged(object payload);

    /// <summary>A viewing or site visit was booked, confirmed, completed or missed.</summary>
    Task VisitChanged(object payload);

    /// <summary>Something on site moved: progress certified, a variation raised, a delay logged.</summary>
    Task SiteChanged(object payload);

    /// <summary>A gate event a guard or a resident is watching for.</summary>
    Task GateEvent(object payload);

    /// <summary>A work order or complaint changed state.</summary>
    Task ServiceChanged(object payload);

    /// <summary>An approval is waiting on this user, or one they raised has been decided.</summary>
    Task ApprovalChanged(object payload);

    /// <summary>An inbound message arrived on a conversation.</summary>
    Task MessageReceived(object payload);

    /// <summary>An alert for whoever is on shift — an overdrawn escrow, a missing file, a lapsed licence.</summary>
    Task DeskAlert(object payload);
}

/// <summary>
/// Live sync for the screens that cannot afford to be stale: the inventory board where two sales
/// executives can sell the same flat, the collections desk, the site diary and the gate.
///
/// The inventory board is the reason this exists. Two people looking at the same tower on two
/// screens must see a unit go on hold the instant it does, because the alternative is a customer
/// paying a token for something already sold — and that is a refund, an apology and a reputation.
///
/// Clients subscribe to a project, an office, a society or their own record, so a scheme with
/// eight thousand plots does not push every receipt to every phone. Every group name carries the
/// tenant's company id: two companies could hold the same project id in a shared deployment and
/// must never see each other's traffic.
/// </summary>
[Authorize]
public class RealEstateHub : Hub<IRealEstateClient>
{
    public static string ProjectGroup(Guid companyId, Guid projectId) => $"re-p:{companyId:N}:{projectId:N}";

    public static string OfficeGroup(Guid companyId, Guid officeId) => $"re-o:{companyId:N}:{officeId:N}";

    public static string SocietyGroup(Guid companyId, Guid societyId) => $"re-s:{companyId:N}:{societyId:N}";

    public static string SiteGroup(Guid companyId, Guid constructionProjectId)
        => $"re-c:{companyId:N}:{constructionProjectId:N}";

    public static string UserGroup(Guid companyId, Guid userId) => $"re-u:{companyId:N}:{userId:N}";

    public static string CompanyGroup(Guid companyId) => $"re:{companyId:N}";

    private Guid CompanyId =>
        Guid.TryParse(Context.User?.FindFirst("CompanyId")?.Value, out var id)
            ? id
            : throw new HubException("No company context on this connection.");

    public override async Task OnConnectedAsync()
    {
        // Everybody joins the company channel, because desk alerts are not scoped to a project and
        // the person who needs to see an overdrawn escrow account may not have it open.
        await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(CompanyId));

        var userId = Context.User?.FindFirst("UserId")?.Value;

        if (Guid.TryParse(userId, out var parsed))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(CompanyId, parsed));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinProject(Guid projectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(CompanyId, projectId));

    public async Task LeaveProject(Guid projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, ProjectGroup(CompanyId, projectId));

    public async Task JoinOffice(Guid officeId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, OfficeGroup(CompanyId, officeId));

    public async Task LeaveOffice(Guid officeId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, OfficeGroup(CompanyId, officeId));

    public async Task JoinSociety(Guid societyId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, SocietyGroup(CompanyId, societyId));

    public async Task LeaveSociety(Guid societyId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, SocietyGroup(CompanyId, societyId));

    /// <summary>A site engineer or quantity surveyor watching one construction project's diary.</summary>
    public async Task JoinSite(Guid constructionProjectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, SiteGroup(CompanyId, constructionProjectId));

    public async Task LeaveSite(Guid constructionProjectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, SiteGroup(CompanyId, constructionProjectId));
}

/// <summary>
/// How the rest of the module pushes to connected screens without taking a dependency on SignalR.
/// </summary>
public interface IRealEstateNotifier
{
    Task InventoryChangedAsync(Guid companyId, Guid projectId, object payload);
    Task BookingChangedAsync(Guid companyId, Guid projectId, object payload);
    Task MoneyChangedAsync(Guid companyId, Guid? projectId, Guid? officeId, object payload);
    Task LeadChangedAsync(Guid companyId, Guid? officeId, Guid? assignedUserId, object payload);
    Task VisitChangedAsync(Guid companyId, Guid? projectId, Guid? officeId, object payload);
    Task SiteChangedAsync(Guid companyId, Guid constructionProjectId, object payload);
    Task GateEventAsync(Guid companyId, Guid societyId, object payload);
    Task ServiceChangedAsync(Guid companyId, Guid? societyId, Guid? projectId, object payload);
    Task ApprovalChangedAsync(Guid companyId, Guid? recipientUserId, object payload);
    Task MessageReceivedAsync(Guid companyId, Guid? assignedUserId, object payload);
    Task DeskAlertAsync(Guid companyId, object payload);
}

/// <inheritdoc />
public class RealEstateNotifier(IHubContext<RealEstateHub, IRealEstateClient> hub) : IRealEstateNotifier
{
    public Task InventoryChangedAsync(Guid companyId, Guid projectId, object payload)
        => hub.Clients.Group(RealEstateHub.ProjectGroup(companyId, projectId)).InventoryChanged(payload);

    public Task BookingChangedAsync(Guid companyId, Guid projectId, object payload)
        => hub.Clients.Group(RealEstateHub.ProjectGroup(companyId, projectId)).BookingChanged(payload);

    public async Task MoneyChangedAsync(Guid companyId, Guid? projectId, Guid? officeId, object payload)
    {
        // A receipt matters to the project's collections board and to the office that took it, and
        // those are rarely the same screen. Sending to both beats making either one poll.
        if (projectId is not null)
            await hub.Clients.Group(RealEstateHub.ProjectGroup(companyId, projectId.Value)).MoneyChanged(payload);

        if (officeId is not null)
            await hub.Clients.Group(RealEstateHub.OfficeGroup(companyId, officeId.Value)).MoneyChanged(payload);

        if (projectId is null && officeId is null)
            await hub.Clients.Group(RealEstateHub.CompanyGroup(companyId)).MoneyChanged(payload);
    }

    public async Task LeadChangedAsync(Guid companyId, Guid? officeId, Guid? assignedUserId, object payload)
    {
        if (assignedUserId is not null)
            await hub.Clients.Group(RealEstateHub.UserGroup(companyId, assignedUserId.Value)).LeadChanged(payload);

        if (officeId is not null)
            await hub.Clients.Group(RealEstateHub.OfficeGroup(companyId, officeId.Value)).LeadChanged(payload);
    }

    public async Task VisitChangedAsync(Guid companyId, Guid? projectId, Guid? officeId, object payload)
    {
        if (projectId is not null)
            await hub.Clients.Group(RealEstateHub.ProjectGroup(companyId, projectId.Value)).VisitChanged(payload);

        if (officeId is not null)
            await hub.Clients.Group(RealEstateHub.OfficeGroup(companyId, officeId.Value)).VisitChanged(payload);
    }

    public Task SiteChangedAsync(Guid companyId, Guid constructionProjectId, object payload)
        => hub.Clients.Group(RealEstateHub.SiteGroup(companyId, constructionProjectId)).SiteChanged(payload);

    public Task GateEventAsync(Guid companyId, Guid societyId, object payload)
        => hub.Clients.Group(RealEstateHub.SocietyGroup(companyId, societyId)).GateEvent(payload);

    public async Task ServiceChangedAsync(Guid companyId, Guid? societyId, Guid? projectId, object payload)
    {
        if (societyId is not null)
            await hub.Clients.Group(RealEstateHub.SocietyGroup(companyId, societyId.Value)).ServiceChanged(payload);

        if (projectId is not null)
            await hub.Clients.Group(RealEstateHub.ProjectGroup(companyId, projectId.Value)).ServiceChanged(payload);

        if (societyId is null && projectId is null)
            await hub.Clients.Group(RealEstateHub.CompanyGroup(companyId)).ServiceChanged(payload);
    }

    public Task ApprovalChangedAsync(Guid companyId, Guid? recipientUserId, object payload)
        => recipientUserId is null
            ? hub.Clients.Group(RealEstateHub.CompanyGroup(companyId)).ApprovalChanged(payload)
            : hub.Clients.Group(RealEstateHub.UserGroup(companyId, recipientUserId.Value)).ApprovalChanged(payload);

    public Task MessageReceivedAsync(Guid companyId, Guid? assignedUserId, object payload)
        => assignedUserId is null
            ? hub.Clients.Group(RealEstateHub.CompanyGroup(companyId)).MessageReceived(payload)
            : hub.Clients.Group(RealEstateHub.UserGroup(companyId, assignedUserId.Value)).MessageReceived(payload);

    public Task DeskAlertAsync(Guid companyId, object payload)
        => hub.Clients.Group(RealEstateHub.CompanyGroup(companyId)).DeskAlert(payload);
}
