using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Projects, their structure, milestones, budget, cash flow, site plans and plot files.
///
/// Certifying a milestone is the consequential call on this controller: it releases the linked
/// instalments to every buyer on the scheme at once. It gates on the milestones before it and on
/// any blocking approval, because a demand raised against work that has not happened is a demand
/// the whole scheme will hear about.
/// </summary>
[Route("api/realestate/projects")]
public class ProjectController(
    IProjectService projects,
    ILogger<ProjectController> logger) : RealEstateControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ListQueryDto query)
        => RunPaged(() => projects.GetProjectsAsync(query));

    [HttpGet("lookup")]
    public Task<IActionResult> Lookup() => Run(projects.GetProjectLookupAsync);

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => projects.GetProjectAsync(id), "That project does not exist.");

    [HttpPost]
    public Task<IActionResult> Save([FromBody] ProjectUpsertDto request)
        => Run(() => projects.SaveProjectAsync(request, UserId), "Project saved.");

    // ── Structure ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/structure")]
    public Task<IActionResult> GetStructure(Guid id)
        => Run(() => projects.GetStructureAsync(id));

    [HttpPost("nodes")]
    public Task<IActionResult> SaveNode([FromBody] ProjectNodeUpsertDto request)
        => Run(() => projects.SaveNodeAsync(request, UserId), "Saved.");

    [HttpDelete("nodes/{nodeId:guid}")]
    public Task<IActionResult> DeleteNode(Guid nodeId)
        => Run(() => projects.DeleteNodeAsync(nodeId, UserId), "Removed.");

    /// <summary>
    /// Creates the units for a block or a floor range in one pass, numbered to the scheme's own
    /// convention. Beats typing four hundred flats in by hand, and gets the numbering consistent.
    /// </summary>
    [HttpPost("generate-units")]
    public Task<IActionResult> GenerateUnits([FromBody] UnitGenerationDto request)
        => Run(() => projects.GenerateUnitsAsync(request, UserId), "Units generated.");

    // ── Milestones ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/milestones")]
    public Task<IActionResult> GetMilestones(Guid id)
        => Run(() => projects.GetMilestonesAsync(id));

    [HttpPost("milestones")]
    public Task<IActionResult> SaveMilestone([FromBody] ProjectMilestoneDto request)
        => Run(() => projects.SaveMilestoneAsync(request, UserId), "Milestone saved.");

    /// <summary>Certifies a milestone and raises the demands that hang off it.</summary>
    [HttpPost("milestones/certify")]
    public Task<IActionResult> CertifyMilestone([FromBody] MilestoneCertificateDto request)
        => Run(() => projects.CertifyMilestoneAsync(request, UserId), "Milestone certified.");

    // ── Money ────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/budget")]
    public Task<IActionResult> GetBudget(Guid id)
        => Run(() => projects.GetBudgetAsync(id));

    [HttpPost("{id:guid}/budget")]
    public Task<IActionResult> SaveBudgetLine(Guid id, [FromBody] ProjectBudgetLineDto request)
        => Run(() => projects.SaveBudgetLineAsync(id, request, UserId), "Budget line saved.");

    [HttpGet("{id:guid}/cash-flow")]
    public Task<IActionResult> GetCashFlow(Guid id, [FromQuery] int months = 24)
        => Run(() => projects.GetCashFlowAsync(id, months));

    // ── Site plans ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/site-plans")]
    public Task<IActionResult> GetSitePlans(Guid id)
        => Run(() => projects.GetSitePlansAsync(id));

    [HttpPost("{id:guid}/site-plans")]
    public Task<IActionResult> SaveSitePlan(Guid id, [FromBody] SitePlanDto request)
        => Run(() => projects.SaveSitePlanAsync(id, request, UserId), "Site plan saved.");

    /// <summary>The clickable shapes on a site plan, so a plot can be sold off the map.</summary>
    [HttpPut("site-plans/{sitePlanId:guid}/shapes")]
    public Task<IActionResult> SaveShapes(Guid sitePlanId, [FromBody] List<SitePlanShapeDto> shapes)
        => Run(() => projects.SaveShapesAsync(sitePlanId, shapes, UserId), "Shapes saved.");

    // ── Plot files ───────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/plot-files")]
    public Task<IActionResult> GetPlotFiles(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => projects.GetPlotFilesAsync(id, query));

    /// <summary>
    /// Issues a run of files against a scheme before the plots themselves are balloted. This is how
    /// a plot scheme sells: the file is the tradable thing, and the plot comes later.
    /// </summary>
    [HttpPost("{id:guid}/plot-files")]
    public Task<IActionResult> IssuePlotFiles(
        Guid id,
        [FromQuery] string categoryCode,
        [FromQuery] int count,
        [FromQuery] decimal price,
        [FromQuery] decimal areaSqFt)
        => Run(() => projects.IssuePlotFilesAsync(id, categoryCode, count, price, areaSqFt, UserId),
            $"{count} files issued.");
}
