using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Listings, instructions, portal syndication, campaigns and marketing content.
///
/// Publishing is gated rather than trusted. A listing that goes out without the photographs, the
/// energy rating or the permit number is at best ignored by the portal and at worst a regulatory
/// breach, so readiness is checked first and the failures come back named.
/// </summary>
[Route("api/realestate/listings")]
public class ListingController(
    IListingService listings,
    ILogger<ListingController> logger) : RealEstateControllerBase(logger)
{
    [HttpPost("search")]
    public Task<IActionResult> Search([FromBody] ListingSearchDto query)
        => RunPaged(() => listings.GetListingsAsync(query));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => listings.GetListingAsync(id), "That listing does not exist.");

    [HttpPost]
    public Task<IActionResult> Save([FromBody] ListingUpsertDto request)
        => Run(() => listings.SaveListingAsync(request, UserId), "Listing saved.");

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(
        Guid id, [FromQuery] ListingStatus status, [FromQuery] Guid? reasonCodeId)
        => Run(() => listings.ChangeStatusAsync(id, status, reasonCodeId, UserId), "Status updated.");

    /// <summary>Changes the asking price and records what it was, so the history reads honestly.</summary>
    [HttpPost("price")]
    public Task<IActionResult> ChangePrice([FromBody] ListingPriceChangeDto request)
        => Run(() => listings.ChangePriceAsync(request, UserId), "Price updated.");

    /// <summary>What is still missing before this listing can go out to the portals.</summary>
    [HttpGet("{id:guid}/publish-readiness")]
    public Task<IActionResult> CheckReadiness(Guid id)
        => Run(() => listings.CheckPublishReadinessAsync(id));

    // ── Instructions ─────────────────────────────────────────────────────────

    [HttpGet("instructions")]
    public Task<IActionResult> GetInstructions([FromQuery] ListQueryDto query)
        => RunPaged(() => listings.GetInstructionsAsync(query));

    [HttpGet("instructions/{id:guid}")]
    public Task<IActionResult> GetInstruction(Guid id)
        => RunFound(() => listings.GetInstructionAsync(id), "That instruction does not exist.");

    [HttpPost("instructions")]
    public Task<IActionResult> SaveInstruction([FromBody] InstructionUpsertDto request)
        => Run(() => listings.SaveInstructionAsync(request, UserId), "Instruction saved.");

    [HttpPost("instructions/{id:guid}/terminate")]
    public Task<IActionResult> TerminateInstruction(
        Guid id, [FromQuery] Guid reasonCodeId, [FromQuery] string? note)
        => Run(() => listings.TerminateInstructionAsync(id, reasonCodeId, note, UserId), "Instruction ended.");

    // ── Portals ──────────────────────────────────────────────────────────────

    [HttpGet("portals")]
    public Task<IActionResult> GetPortals() => Run(listings.GetPortalsAsync);

    [HttpPost("portals")]
    public Task<IActionResult> SavePortal([FromBody] PortalChannelDto request)
        => Run(() => listings.SavePortalAsync(request, UserId), "Portal saved.");

    [HttpGet("portals/{portalId:guid}/mappings")]
    public Task<IActionResult> GetMappings(Guid portalId)
        => Run(() => listings.GetPortalMappingsAsync(portalId));

    [HttpPost("portals/mappings")]
    public Task<IActionResult> SaveMapping([FromBody] PortalMappingDto request)
        => Run(() => listings.SavePortalMappingAsync(request, UserId), "Mapping saved.");

    [HttpPost("publish")]
    public Task<IActionResult> Publish([FromBody] PortalPublishRequestDto request)
        => Run(() => listings.PublishAsync(request, UserId));

    [HttpGet("publications")]
    public Task<IActionResult> GetPublications(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? portalId, [FromQuery] PortalPublishState? state)
        => RunPaged(() => listings.GetPublicationsAsync(query, portalId, state));

    // ── Marketing ────────────────────────────────────────────────────────────

    [HttpGet("campaigns")]
    public Task<IActionResult> GetCampaigns([FromQuery] ListQueryDto query)
        => RunPaged(() => listings.GetCampaignsAsync(query));

    [HttpPost("campaigns")]
    public Task<IActionResult> SaveCampaign([FromBody] CampaignDto request)
        => Run(() => listings.SaveCampaignAsync(request, UserId), "Campaign saved.");

    [HttpGet("events")]
    public Task<IActionResult> GetEvents([FromQuery] ListQueryDto query)
        => RunPaged(() => listings.GetEventsAsync(query));

    [HttpPost("events")]
    public Task<IActionResult> SaveEvent([FromBody] MarketingEventDto request)
        => Run(() => listings.SaveEventAsync(request, UserId), "Event saved.");

    [HttpGet("content")]
    public Task<IActionResult> GetContent([FromQuery] ListQueryDto query)
        => RunPaged(() => listings.GetContentAsync(query));

    [HttpPost("content")]
    public Task<IActionResult> SaveContent([FromBody] ContentAssetDto request)
        => Run(() => listings.SaveContentAsync(request, UserId), "Asset saved.");
}
