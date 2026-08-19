using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Distribution.Api.Controllers;

/// <summary>Channel partners: the distribution network and its paperwork.</summary>
[Route("api/distribution/partners")]
public class PartnerController(INetworkService service, ILogger<PartnerController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] PartnerType? type, [FromQuery] PartnerStatus? status,
        [FromQuery] Guid? territoryId, [FromQuery] Guid? parentPartnerId,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListPartnersAsync(
            search, type, status, territoryId, parentPartnerId, pagination ?? new PaginationParams()));

    [HttpGet("tree")]
    public Task<IActionResult> Tree([FromQuery] Guid? rootPartnerId)
        => Run(() => service.GetPartnerTreeAsync(rootPartnerId));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetPartnerAsync(id), "That partner no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SavePartnerDto request)
        => Run(() => service.SavePartnerAsync(null, request, UserId), "Partner created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SavePartnerDto request)
        => Run(() => service.SavePartnerAsync(id, request, UserId), "Partner saved.");

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangePartnerStatusDto request)
        => Run(() => service.ChangePartnerStatusAsync(id, request, UserId), "Status updated.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeletePartnerAsync(id, UserId), "Partner removed.");

    [HttpGet("{id:guid}/documents")]
    public Task<IActionResult> Documents(Guid id)
        => Run(() => service.GetPartnerDocumentsAsync(id));

    [HttpPost("{id:guid}/documents")]
    public Task<IActionResult> SaveDocument(Guid id, [FromBody] PartnerDocumentDto request)
        => Run(() => service.SavePartnerDocumentAsync(id, request, UserId), "Document saved.");

    [HttpPost("documents/{documentId:guid}/verify")]
    public Task<IActionResult> VerifyDocument(Guid documentId, [FromBody] string? note)
        => Run(() => service.VerifyPartnerDocumentAsync(documentId, note, UserId), "Document verified.");

    /// <summary>Licences and registrations expiring inside the window, across the whole network.</summary>
    [HttpGet("expiring-documents")]
    public Task<IActionResult> Expiring([FromQuery] int withinDays = 30)
        => Run(() => service.GetExpiringDocumentsAsync(withinDays));
}

/// <summary>The retail universe: outlets, their assets, photos and notes.</summary>
[Route("api/distribution/outlets")]
public class OutletController(INetworkService service, ILogger<OutletController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] OutletChannel? channel, [FromQuery] OutletGrade? grade,
        [FromQuery] OutletStatus? status, [FromQuery] Guid? partnerId, [FromQuery] Guid? territoryId,
        [FromQuery] Guid? routeId, [FromQuery] bool? pendingApprovalOnly,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListOutletsAsync(
            search, channel, grade, status, partnerId, territoryId, routeId, pendingApprovalOnly,
            pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetOutletAsync(id), "That outlet no longer exists.");

    /// <summary>Everything about one outlet on one screen — the rep's and the manager's view.</summary>
    [HttpGet("{id:guid}/360")]
    public Task<IActionResult> Get360(Guid id)
        => RunFound(() => service.GetOutlet360Async(id), "That outlet no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveOutletDto request)
        => Run(() => service.SaveOutletAsync(null, request, UserId), "Outlet created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveOutletDto request)
        => Run(() => service.SaveOutletAsync(id, request, UserId), "Outlet saved.");

    /// <summary>Doorstep onboarding. Lands pending approval and cannot be invoiced until cleared.</summary>
    [HttpPost("onboard")]
    public Task<IActionResult> Onboard([FromBody] OnboardOutletDto request)
        => Run(() => service.OnboardOutletAsync(request, UserId), "Outlet added for approval.");

    /// <summary>Run before onboarding. Returns candidates rather than blocking.</summary>
    [HttpGet("duplicates")]
    public Task<IActionResult> Duplicates(
        [FromQuery] string? phone, [FromQuery] string? taxNumber, [FromQuery] string? name,
        [FromQuery] double? latitude, [FromQuery] double? longitude)
        => Run(() => service.FindDuplicateOutletsAsync(phone, taxNumber, name, latitude, longitude));

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, [FromQuery] bool isApproved = true, [FromQuery] string? reason = null)
        => Run(() => service.ApproveOutletAsync(id, isApproved, reason, UserId),
            isApproved ? "Outlet approved." : "Outlet rejected.");

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(Guid id, [FromQuery] OutletStatus status, [FromQuery] string? reason)
        => Run(() => service.ChangeOutletStatusAsync(id, status, reason, UserId), "Status updated.");

    [HttpPost("{survivorId:guid}/merge/{duplicateId:guid}")]
    public Task<IActionResult> Merge(Guid survivorId, Guid duplicateId)
        => Run(() => service.MergeOutletsAsync(survivorId, duplicateId, UserId), "Outlets merged.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeleteOutletAsync(id, UserId), "Outlet removed.");

    // ── Assets, photos, notes ────────────────────────────────────────────────

    [HttpGet("assets")]
    public Task<IActionResult> Assets(
        [FromQuery] Guid? outletId, [FromQuery] OutletAssetKind? kind, [FromQuery] AssetCondition? condition)
        => Run(() => service.ListAssetsAsync(outletId, kind, condition));

    [HttpPost("assets")]
    public Task<IActionResult> CreateAsset([FromBody] OutletAssetDto request)
        => Run(() => service.SaveAssetAsync(null, request, UserId), "Asset recorded.");

    [HttpPut("assets/{id:guid}")]
    public Task<IActionResult> UpdateAsset(Guid id, [FromBody] OutletAssetDto request)
        => Run(() => service.SaveAssetAsync(id, request, UserId), "Asset saved.");

    [HttpPost("assets/{id:guid}/verify")]
    public Task<IActionResult> VerifyAsset(
        Guid id, [FromQuery] AssetCondition condition,
        [FromQuery] string? photoUrl, [FromQuery] string? note)
        => Run(() => service.VerifyAssetAsync(id, condition, photoUrl, note, UserId), "Asset verified.");

    [HttpGet("{id:guid}/photos")]
    public Task<IActionResult> Photos(Guid id, [FromQuery] string? tag)
        => Run(() => service.ListPhotosAsync(id, tag));

    [HttpPost("photos")]
    public Task<IActionResult> AddPhoto([FromBody] OutletPhotoDto request)
        => Run(() => service.AddPhotoAsync(request, UserId), "Photo added.");

    [HttpGet("{id:guid}/notes")]
    public Task<IActionResult> Notes(Guid id)
        => Run(() => service.ListNotesAsync(id));

    [HttpPost("notes")]
    public Task<IActionResult> AddNote([FromBody] OutletNoteDto request)
        => Run(() => service.AddNoteAsync(request, UserId), "Note added.");
}

/// <summary>Geography, territories, routes and the journey plan.</summary>
[Route("api/distribution/routes")]
public class RouteController(IRouteService service, ILogger<RouteController> logger)
    : DistributionControllerBase(logger)
{
    // ── Geography ────────────────────────────────────────────────────────────

    [HttpGet("geo")]
    public Task<IActionResult> GeoTree([FromQuery] Guid? rootId)
        => Run(() => service.GetGeoTreeAsync(rootId));

    [HttpPost("geo")]
    public Task<IActionResult> CreateGeo([FromBody] GeoNodeDto request)
        => Run(() => service.SaveGeoNodeAsync(null, request, UserId), "Location created.");

    [HttpPut("geo/{id:guid}")]
    public Task<IActionResult> UpdateGeo(Guid id, [FromBody] GeoNodeDto request)
        => Run(() => service.SaveGeoNodeAsync(id, request, UserId), "Location saved.");

    [HttpDelete("geo/{id:guid}")]
    public Task<IActionResult> DeleteGeo(Guid id)
        => Run(() => service.DeleteGeoNodeAsync(id, UserId), "Location removed.");

    // ── Territories ──────────────────────────────────────────────────────────

    [HttpGet("territories/tree")]
    public Task<IActionResult> TerritoryTree([FromQuery] Guid? rootId)
        => Run(() => service.GetTerritoryTreeAsync(rootId));

    [HttpGet("territories")]
    public Task<IActionResult> Territories([FromQuery] string? search, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListTerritoriesAsync(search, pagination ?? new PaginationParams()));

    [HttpGet("territories/{id:guid}")]
    public Task<IActionResult> Territory(Guid id)
        => RunFound(() => service.GetTerritoryAsync(id), "That territory no longer exists.");

    [HttpPost("territories")]
    public Task<IActionResult> CreateTerritory([FromBody] SaveTerritoryDto request)
        => Run(() => service.SaveTerritoryAsync(null, request, UserId), "Territory created.");

    [HttpPut("territories/{id:guid}")]
    public Task<IActionResult> UpdateTerritory(Guid id, [FromBody] SaveTerritoryDto request)
        => Run(() => service.SaveTerritoryAsync(id, request, UserId), "Territory saved.");

    [HttpDelete("territories/{id:guid}")]
    public Task<IActionResult> DeleteTerritory(Guid id)
        => Run(() => service.DeleteTerritoryAsync(id, UserId), "Territory removed.");

    // ── Routes ───────────────────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] RouteKind? kind, [FromQuery] Guid? territoryId,
        [FromQuery] Guid? fieldRepId, [FromQuery] Guid? partnerId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListRoutesAsync(
            search, kind, territoryId, fieldRepId, partnerId, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetRouteAsync(id), "That route no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveRouteDto request)
        => Run(() => service.SaveRouteAsync(null, request, UserId), "Route created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveRouteDto request)
        => Run(() => service.SaveRouteAsync(id, request, UserId), "Route saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeleteRouteAsync(id, UserId), "Route removed.");

    [HttpPost("outlets")]
    public Task<IActionResult> AddOutlets([FromBody] AddOutletsToRouteDto request)
        => Run(() => service.AddOutletsAsync(request, UserId), "Outlets added to the beat.");

    [HttpDelete("{routeId:guid}/outlets/{outletId:guid}")]
    public Task<IActionResult> RemoveOutlet(Guid routeId, Guid outletId)
        => Run(() => service.RemoveOutletAsync(routeId, outletId, UserId), "Outlet removed from the beat.");

    [HttpPost("resequence")]
    public Task<IActionResult> Resequence([FromBody] ResequenceRouteDto request)
        => Run(() => service.ResequenceAsync(request, UserId), "Beat resequenced.");

    [HttpPost("assign")]
    public Task<IActionResult> Assign([FromBody] AssignRouteDto request)
        => Run(() => service.AssignAsync(request, UserId), "Route assigned.");

    [HttpPost("{id:guid}/split")]
    public Task<IActionResult> Split(
        Guid id, [FromQuery] string newRouteName, [FromBody] List<Guid> outletIdsToMove)
        => Run(() => service.SplitRouteAsync(id, outletIdsToMove, newRouteName, UserId), "Route split.");

    /// <summary>Outlets nobody is scheduled to visit — the quiet revenue leak.</summary>
    [HttpGet("unrouted")]
    public Task<IActionResult> Unrouted([FromQuery] Guid? territoryId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.GetUnroutedOutletsAsync(territoryId, pagination ?? new PaginationParams()));

    // ── Journey plans ────────────────────────────────────────────────────────

    [HttpGet("journey")]
    public Task<IActionResult> JourneyPlans([FromQuery] Guid? territoryId, [FromQuery] DateTime periodStart)
        => Run(() => service.ListJourneyPlansAsync(territoryId, periodStart));

    [HttpGet("journey/{fieldRepId:guid}")]
    public Task<IActionResult> JourneyPlan(Guid fieldRepId, [FromQuery] DateTime periodStart)
        => RunFound(() => service.GetJourneyPlanAsync(fieldRepId, periodStart),
            "No journey plan exists for that period.");

    [HttpPost("journey/generate")]
    public Task<IActionResult> GenerateJourneyPlan([FromBody] GenerateJourneyPlanDto request)
        => Run(() => service.GenerateJourneyPlanAsync(request, UserId), "Journey plan generated.");

    [HttpPost("journey/{planId:guid}/publish")]
    public Task<IActionResult> PublishJourneyPlan(Guid planId)
        => Run(() => service.PublishJourneyPlanAsync(planId, UserId), "Journey plan published.");

    [HttpPut("journey/days/{dayId:guid}")]
    public Task<IActionResult> UpdateJourneyDay(Guid dayId, [FromBody] UpdateJourneyPlanDayDto request)
        => Run(() => service.UpdateJourneyPlanDayAsync(dayId, request, UserId), "Plan day updated.");
}
