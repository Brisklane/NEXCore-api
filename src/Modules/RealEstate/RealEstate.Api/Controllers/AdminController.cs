using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Settings, offices, geography, agents, approvals, saved views and data import.
///
/// The lines-of-business switches live here, and they change what the whole application looks
/// like: an agency never sees escrow, a plot developer never sees a rent roll. That is one setting
/// with very wide consequences, which is why it has its own endpoint rather than hiding inside a
/// general settings blob.
/// </summary>
[Route("api/realestate/admin")]
public class AdminController(
    IRealEstateAdminService admin,
    ILogger<AdminController> logger) : RealEstateControllerBase(logger)
{
    // ── Settings ─────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public Task<IActionResult> GetSettings() => Run(admin.GetSettingsAsync);

    [HttpPut("settings")]
    public Task<IActionResult> UpdateSettings([FromBody] RealEstateSettingsDto request)
        => Run(() => admin.UpdateSettingsAsync(request, UserId), "Settings saved.");

    /// <summary>Which of the four lines of business this company runs, and what each one turns on.</summary>
    [HttpGet("lines-of-business")]
    public Task<IActionResult> GetLinesOfBusiness() => Run(admin.GetLinesOfBusinessAsync);

    // ── Offices ──────────────────────────────────────────────────────────────

    [HttpGet("offices")]
    public Task<IActionResult> GetOffices([FromQuery] ListQueryDto query)
        => RunPaged(() => admin.GetOfficesAsync(query));

    [HttpGet("offices/{id:guid}")]
    public Task<IActionResult> GetOffice(Guid id)
        => RunFound(() => admin.GetOfficeAsync(id), "That office does not exist.");

    [HttpPost("offices")]
    public Task<IActionResult> SaveOffice([FromBody] RealEstateOfficeDto request)
        => Run(() => admin.SaveOfficeAsync(request, UserId), "Office saved.");

    // ── Geography ────────────────────────────────────────────────────────────

    [HttpGet("geo")]
    public Task<IActionResult> GetGeoTree([FromQuery] Guid? parentId, [FromQuery] int depth = 2)
        => Run(() => admin.GetGeoTreeAsync(parentId, depth));

    [HttpGet("geo/search")]
    public Task<IActionResult> SearchGeo([FromQuery] string search, [FromQuery] int take = 20)
        => Run(() => admin.SearchGeoAreasAsync(search, take));

    [HttpPost("geo")]
    public Task<IActionResult> SaveGeo([FromBody] GeoAreaDto request)
        => Run(() => admin.SaveGeoAreaAsync(request, UserId), "Area saved.");

    [HttpDelete("geo/{id:guid}")]
    public Task<IActionResult> DeleteGeo(Guid id)
        => Run(() => admin.DeleteGeoAreaAsync(id, UserId), "Area removed.");

    // ── Territories ──────────────────────────────────────────────────────────

    [HttpGet("territories")]
    public Task<IActionResult> GetTerritories([FromQuery] Guid? officeId)
        => Run(() => admin.GetTerritoriesAsync(officeId));

    [HttpPost("territories")]
    public Task<IActionResult> SaveTerritory([FromBody] TerritoryDto request)
        => Run(() => admin.SaveTerritoryAsync(request, UserId), "Territory saved.");

    // ── Agents and teams ─────────────────────────────────────────────────────

    [HttpGet("agents")]
    public Task<IActionResult> GetAgents([FromQuery] ListQueryDto query)
        => RunPaged(() => admin.GetAgentsAsync(query));

    [HttpGet("agents/{id:guid}")]
    public Task<IActionResult> GetAgent(Guid id)
        => RunFound(() => admin.GetAgentAsync(id), "That agent does not exist.");

    [HttpPost("agents")]
    public Task<IActionResult> SaveAgent([FromBody] AgentProfileDto request)
        => Run(() => admin.SaveAgentAsync(request, UserId), "Agent saved.");

    [HttpGet("agents/lookup")]
    public Task<IActionResult> GetAgentLookup([FromQuery] Guid? officeId)
        => Run(() => admin.GetAgentLookupAsync(officeId));

    [HttpGet("teams")]
    public Task<IActionResult> GetTeams([FromQuery] Guid? officeId)
        => Run(() => admin.GetTeamsAsync(officeId));

    [HttpPost("teams")]
    public Task<IActionResult> SaveTeam([FromBody] SalesTeamDto request)
        => Run(() => admin.SaveTeamAsync(request, UserId), "Team saved.");

    // ── Reason codes ─────────────────────────────────────────────────────────

    [HttpGet("reason-codes")]
    public Task<IActionResult> GetReasonCodes([FromQuery] string? context)
        => Run(() => admin.GetReasonCodesAsync(context));

    [HttpPost("reason-codes")]
    public Task<IActionResult> SaveReasonCode([FromBody] ReasonCodeDto request)
        => Run(() => admin.SaveReasonCodeAsync(request, UserId), "Reason code saved.");

    // ── Approvals ────────────────────────────────────────────────────────────

    [HttpGet("approval-matrix")]
    public Task<IActionResult> GetMatrix([FromQuery] string? documentType)
        => Run(() => admin.GetApprovalMatrixAsync(documentType));

    [HttpPost("approval-matrix")]
    public Task<IActionResult> SaveMatrix([FromBody] ApprovalMatrixDto request)
        => Run(() => admin.SaveApprovalMatrixAsync(request, UserId), "Approval band saved.");

    [HttpGet("approvals")]
    public Task<IActionResult> GetApprovals([FromQuery] ListQueryDto query, [FromQuery] bool mineOnly = true)
        => RunPaged(() => admin.GetApprovalsAsync(query, mineOnly, UserId));

    [HttpPost("approvals/decide")]
    public Task<IActionResult> DecideApproval([FromBody] ApprovalDecisionDto request)
        => Run(() => admin.DecideApprovalAsync(request, UserId), "Decision recorded.");

    // ── Saved views ──────────────────────────────────────────────────────────

    [HttpGet("views")]
    public Task<IActionResult> GetViews([FromQuery] string screenKey)
        => Run(() => admin.GetSavedViewsAsync(screenKey, UserId));

    [HttpPost("views")]
    public Task<IActionResult> SaveView([FromBody] SavedViewDto request)
        => Run(() => admin.SaveViewAsync(request, UserId), "View saved.");

    [HttpDelete("views/{id:guid}")]
    public Task<IActionResult> DeleteView(Guid id)
        => Run(() => admin.DeleteSavedViewAsync(id, UserId), "View removed.");

    // ── Import ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a file and stages what it would create, without writing anything to the live data.
    /// A migration that cannot be inspected before it lands is a migration nobody should run.
    /// </summary>
    [HttpPost("imports")]
    public Task<IActionResult> RunImport([FromBody] ImportRequestDto request)
        => Run(() => admin.RunImportAsync(request, UserId));

    [HttpGet("imports")]
    public Task<IActionResult> GetImports([FromQuery] ListQueryDto query)
        => RunPaged(() => admin.GetImportBatchesAsync(query));

    [HttpGet("imports/{id:guid}")]
    public Task<IActionResult> GetImport(Guid id)
        => RunFound(() => admin.GetImportBatchAsync(id), "That import does not exist.");

    [HttpPost("imports/{id:guid}/commit")]
    public Task<IActionResult> CommitImport(Guid id)
        => Run(() => admin.CommitImportAsync(id, UserId), "Import committed.");

    [HttpPost("imports/{id:guid}/rollback")]
    public Task<IActionResult> RollbackImport(Guid id)
        => Run(() => admin.RollbackImportAsync(id, UserId), "Import rolled back.");
}
