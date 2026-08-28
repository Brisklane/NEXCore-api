using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// The property register, its media and documents, ownership, valuations — and the land side:
/// parcels, title chains, encumbrances and acquisition.
///
/// A property record here is the physical thing, permanent and re-used: it outlives every listing
/// it was ever advertised under and every tenancy it ever held. That is why re-pricing lives on
/// the unit rather than here, and why deleting a property is nearly always the wrong instinct.
/// </summary>
[Route("api/realestate/properties")]
public class PropertyController(
    IPropertyService properties,
    ILogger<PropertyController> logger) : RealEstateControllerBase(logger)
{
    [HttpPost("search")]
    public Task<IActionResult> Search([FromBody] PropertySearchDto query)
        => RunPaged(() => properties.GetPropertiesAsync(query));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => properties.GetPropertyAsync(id), "That property does not exist.");

    [HttpPost]
    public Task<IActionResult> Save([FromBody] PropertyUpsertDto request)
        => Run(() => properties.SavePropertyAsync(request, UserId), "Property saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => properties.DeletePropertyAsync(id, UserId), "Property removed.");

    /// <summary>
    /// Looks for a property already on file that this one might be. Run before saving, because two
    /// records for the same flat means two owners, two histories and an argument later.
    /// </summary>
    [HttpPost("check-duplicates")]
    public Task<IActionResult> CheckDuplicates([FromBody] PropertyUpsertDto request)
        => Run(() => properties.CheckDuplicatesAsync(request));

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(
        Guid id,
        [FromQuery] PropertyStatus status,
        [FromQuery] Guid? reasonCodeId,
        [FromQuery] string? note)
        => Run(() => properties.ChangeStatusAsync(id, status, reasonCodeId, note, UserId), "Status updated.");

    // ── Media and documents ──────────────────────────────────────────────────

    [HttpPut("{id:guid}/media")]
    public Task<IActionResult> SaveMedia(Guid id, [FromBody] List<PropertyMediaDto> media)
        => Run(() => properties.SaveMediaAsync(id, media, UserId), "Media saved.");

    [HttpDelete("media/{mediaId:guid}")]
    public Task<IActionResult> DeleteMedia(Guid mediaId)
        => Run(() => properties.DeleteMediaAsync(mediaId, UserId), "Media removed.");

    [HttpGet("{id:guid}/documents")]
    public Task<IActionResult> GetDocuments(Guid id)
        => Run(() => properties.GetDocumentsAsync(id));

    [HttpPost("{id:guid}/documents")]
    public Task<IActionResult> SaveDocument(Guid id, [FromBody] PropertyDocumentDto request)
        => Run(() => properties.SaveDocumentAsync(id, request, UserId), "Document saved.");

    // ── Ownership ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/ownership")]
    public Task<IActionResult> GetOwnership(Guid id, [FromQuery] bool includeHistory = false)
        => Run(() => properties.GetOwnershipAsync(id, includeHistory));

    [HttpPost("{id:guid}/ownership")]
    public Task<IActionResult> SetOwner(Guid id, [FromBody] PropertyOwnershipDto request)
        => Run(() => properties.SetOwnerAsync(id, request, UserId), "Owner recorded.");

    // ── Valuation ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/valuations")]
    public Task<IActionResult> GetValuations(Guid id)
        => Run(() => properties.GetValuationsAsync(id));

    [HttpPost("valuations")]
    public Task<IActionResult> SaveValuation([FromBody] PropertyValuationDto request)
        => Run(() => properties.SaveValuationAsync(request, UserId), "Valuation saved.");

    /// <summary>
    /// A defensible asking price built from comparables, each weighted by how recent, how similar
    /// and how close it is — with the workings returned so a vendor can be shown them.
    /// </summary>
    [HttpGet("{id:guid}/price-opinion")]
    public Task<IActionResult> GetPriceOpinion(Guid id, [FromQuery] ListingKind kind = ListingKind.ForSale)
        => Run(() => properties.GetPriceOpinionAsync(id, kind));

    [HttpGet("{id:guid}/comparables")]
    public Task<IActionResult> FindComparables(Guid id, [FromQuery] int take = 10)
        => Run(() => properties.FindComparablesAsync(id, take));

    // ── Land parcels ─────────────────────────────────────────────────────────

    [HttpGet("parcels")]
    public Task<IActionResult> GetParcels([FromQuery] ListQueryDto query)
        => RunPaged(() => properties.GetParcelsAsync(query));

    [HttpGet("parcels/{id:guid}")]
    public Task<IActionResult> GetParcel(Guid id)
        => RunFound(() => properties.GetParcelAsync(id), "That parcel does not exist.");

    [HttpPost("parcels")]
    public Task<IActionResult> SaveParcel([FromBody] LandParcelDetailDto request)
        => Run(() => properties.SaveParcelAsync(request, UserId), "Parcel saved.");

    /// <summary>Appends one link to the chain of title. Never edits an earlier one.</summary>
    [HttpPost("parcels/{id:guid}/title-chain")]
    public Task<IActionResult> SaveTitleEntry(Guid id, [FromBody] TitleChainEntryDto request)
        => Run(() => properties.SaveTitleEntryAsync(id, request, UserId), "Title entry recorded.");

    [HttpPost("encumbrances")]
    public Task<IActionResult> SaveEncumbrance(
        [FromQuery] Guid? parcelId,
        [FromQuery] Guid? propertyId,
        [FromBody] EncumbranceDto request)
        => Run(() => properties.SaveEncumbranceAsync(parcelId, propertyId, request, UserId), "Encumbrance saved.");

    [HttpPost("parcels/{id:guid}/verifications")]
    public Task<IActionResult> SaveVerification(Guid id, [FromBody] TitleVerificationItemDto request)
        => Run(() => properties.SaveVerificationAsync(id, request, UserId), "Verification recorded.");

    // ── Acquisition ──────────────────────────────────────────────────────────

    [HttpGet("acquisitions")]
    public Task<IActionResult> GetAcquisitions([FromQuery] ListQueryDto query)
        => RunPaged(() => properties.GetAcquisitionsAsync(query));

    [HttpPost("acquisitions")]
    public Task<IActionResult> SaveAcquisition([FromBody] LandAcquisitionDto request)
        => Run(() => properties.SaveAcquisitionAsync(request, UserId), "Acquisition saved.");

    [HttpPost("acquisitions/{id:guid}/costs")]
    public Task<IActionResult> SaveAcquisitionCost(Guid id, [FromBody] AcquisitionCostLineDto request)
        => Run(() => properties.SaveAcquisitionCostAsync(id, request, UserId), "Cost recorded.");
}
