using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Crm.Api.Controllers;

/// <summary>
/// Manages a contact's saved delivery addresses.
/// The order frontend calls GET /contact/{contactId} when a customer is
/// selected to populate the address picker dropdown.
/// </summary>
[ApiController]
[Route("api/crm/contact-addresses")]
[Authorize]
public class ContactAddressController : ControllerBase
{
    private readonly IContactAddressRepository _repo;
    private readonly IGeocodingService _geocoding;
    private readonly ILogger<ContactAddressController> _logger;

    public ContactAddressController(
        IContactAddressRepository repo,
        IGeocodingService geocoding,
        ILogger<ContactAddressController> logger)
    {
        _repo = repo;
        _geocoding = geocoding;
        _logger = logger;
    }

    // ── GET /reverse-geocode?lat=&lng= ────────────────────────────────────────

    /// <summary>
    /// Resolves a map pin (lat/lng) to a structured address.
    /// Call this when the user drops a pin on the map so the form fields
    /// can be pre-populated before the user saves the address.
    /// </summary>
    [HttpGet("reverse-geocode")]
    [ProducesResponseType(typeof(ApiResponse<ReverseGeocodeResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lng)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
            return BadRequest(new ApiErrorResponse { Message = "Latitude must be -90..90 and longitude -180..180" });

        var result = await _geocoding.ReverseGeocodeAsync(lat, lng);

        if (result == null)
            return UnprocessableEntity(new ApiErrorResponse
            {
                Message = "No address found for these coordinates. Try adjusting the pin location.",
            });

        return Ok(new ApiResponse<ReverseGeocodeResultDto>
        {
            Success = true,
            Data = new ReverseGeocodeResultDto
            {
                Street      = result.Street,
                City        = result.City,
                State       = result.State,
                PostalCode  = result.PostalCode,
                Country     = result.Country,
                CountryCode = result.CountryCode,
                DisplayName = result.DisplayName,
                Latitude    = result.Latitude,
                Longitude   = result.Longitude,
            },
            Message = "Address resolved",
        });
    }

    // ── GET all addresses for a contact (used by address picker) ─────────────

    /// <summary>
    /// Returns all active addresses for a contact, default address first.
    /// Call this when the customer dropdown value changes on the order form.
    /// </summary>
    [HttpGet("contact/{contactId}")]
    [ProducesResponseType(typeof(ApiResponse<List<ContactAddressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByContact(Guid contactId)
    {
        try
        {
            var list = await _repo.GetByContactAsync(contactId);
            var ordered = list
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.Label)
                .Select(MapToDto)
                .ToList();

            return Ok(new ApiResponse<List<ContactAddressDto>>
            {
                Success = true,
                Data = ordered,
                Message = $"{ordered.Count} address(es) found",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses for contact {ContactId}", contactId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving addresses" });
        }
    }

    // ── GET single ───────────────────────────────────────────────────────────

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ContactAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var address = await _repo.GetByIdAsync(id);
            if (address == null) return NotFound(new ApiErrorResponse { Message = "Address not found" });
            return Ok(new ApiResponse<ContactAddressDto> { Success = true, Data = MapToDto(address) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving address {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving address" });
        }
    }

    // ── POST (create) ─────────────────────────────────────────────────────────

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactAddressDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateContactAddressDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            if (dto.IsDefault)
                await ClearDefaultAsync(dto.ContactId);

            var address = new ContactAddress
            {
                ContactId           = dto.ContactId,
                AddressType         = dto.AddressType,
                Label               = dto.Label,
                IsDefault           = dto.IsDefault,
                ContactName         = dto.ContactName,
                Phone               = dto.Phone,
                WhatsApp            = dto.WhatsApp,
                Email               = dto.Email,
                Street              = dto.Street,
                Street2             = dto.Street2,
                City                = dto.City,
                State               = dto.State,
                PostalCode          = dto.PostalCode,
                Country             = dto.Country,
                Latitude            = dto.Latitude,
                Longitude           = dto.Longitude,
                DeliveryInstructions = dto.DeliveryInstructions,
                IsActive            = true,
            };

            await _repo.AddAsync(address);
            await _repo.SaveChangesAsync();

            _logger.LogInformation(
                "Contact address created for {ContactId} — label '{Label}'",
                dto.ContactId, dto.Label);

            return StatusCode(201, new ApiResponse<ContactAddressDto>
            {
                Success = true,
                Data = MapToDto(address),
                Message = "Address saved",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact address");
            return StatusCode(500, new ApiErrorResponse { Message = "Error saving address" });
        }
    }

    // ── PUT (update) ──────────────────────────────────────────────────────────

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ContactAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactAddressDto dto)
    {
        try
        {
            var address = await _repo.GetByIdAsync(id);
            if (address == null) return NotFound(new ApiErrorResponse { Message = "Address not found" });

            if (dto.Label        != null) address.Label       = dto.Label;
            if (dto.AddressType  != null) address.AddressType = dto.AddressType;
            if (dto.ContactName  != null) address.ContactName = dto.ContactName;
            if (dto.Phone        != null) address.Phone       = dto.Phone;
            if (dto.WhatsApp     != null) address.WhatsApp    = dto.WhatsApp;
            if (dto.Email        != null) address.Email       = dto.Email;
            if (dto.Street       != null) address.Street      = dto.Street;
            if (dto.Street2      != null) address.Street2     = dto.Street2;
            if (dto.City         != null) address.City        = dto.City;
            if (dto.State        != null) address.State       = dto.State;
            if (dto.PostalCode   != null) address.PostalCode  = dto.PostalCode;
            if (dto.Country      != null) address.Country     = dto.Country;
            if (dto.Latitude.HasValue)    address.Latitude    = dto.Latitude;
            if (dto.Longitude.HasValue)   address.Longitude   = dto.Longitude;
            if (dto.DeliveryInstructions != null) address.DeliveryInstructions = dto.DeliveryInstructions;
            if (dto.IsActive.HasValue)    address.IsActive    = dto.IsActive.Value;

            await _repo.SaveChangesAsync();
            return Ok(new ApiResponse<ContactAddressDto>
            {
                Success = true,
                Data = MapToDto(address),
                Message = "Address updated",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating address" });
        }
    }

    // ── PUT /{id}/set-default ─────────────────────────────────────────────────

    /// <summary>
    /// Marks one address as the default for its contact.
    /// Automatically un-defaults all other addresses for the same contact.
    /// </summary>
    [HttpPut("{id}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<ContactAddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            var address = await _repo.GetByIdAsync(id);
            if (address == null) return NotFound(new ApiErrorResponse { Message = "Address not found" });

            await ClearDefaultAsync(address.ContactId);
            address.IsDefault = true;
            await _repo.SaveChangesAsync();

            return Ok(new ApiResponse<ContactAddressDto>
            {
                Success = true,
                Data = MapToDto(address),
                Message = "Default address updated",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default address {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error setting default address" });
        }
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var address = await _repo.GetByIdAsync(id);
            if (address == null) return NotFound(new ApiErrorResponse { Message = "Address not found" });

            _repo.Delete(address);
            await _repo.SaveChangesAsync();
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Address deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error deleting address" });
        }
    }

    // ── private helpers ───────────────────────────────────────────────────────

    private async Task ClearDefaultAsync(Guid contactId)
    {
        var existing = await _repo.GetDefaultAsync(contactId);
        if (existing != null)
        {
            existing.IsDefault = false;
            await _repo.SaveChangesAsync();
        }
    }

    private static ContactAddressDto MapToDto(ContactAddress a) => new()
    {
        Id                   = a.Id,
        ContactId            = a.ContactId,
        AddressType          = a.AddressType,
        Label                = a.Label,
        IsDefault            = a.IsDefault,
        IsActive             = a.IsActive,
        ContactName          = a.ContactName,
        Phone                = a.Phone,
        WhatsApp             = a.WhatsApp,
        Email                = a.Email,
        Street               = a.Street,
        Street2              = a.Street2,
        City                 = a.City,
        State                = a.State,
        PostalCode           = a.PostalCode,
        Country              = a.Country,
        Latitude             = a.Latitude,
        Longitude            = a.Longitude,
        DeliveryInstructions = a.DeliveryInstructions,
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class ContactAddressDto
{
    public Guid Id { get; set; }
    public Guid ContactId { get; set; }
    public string AddressType { get; set; } = "Shipping";
    public string? Label { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string Street { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryInstructions { get; set; }
}

public class CreateContactAddressDto
{
    public Guid ContactId { get; set; }
    public string AddressType { get; set; } = "Shipping";
    public string? Label { get; set; }
    public bool IsDefault { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string Street { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? State { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryInstructions { get; set; }
}

public class UpdateContactAddressDto
{
    public string? AddressType { get; set; }
    public string? Label { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Street { get; set; }
    public string? Street2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeliveryInstructions { get; set; }
    public bool? IsActive { get; set; }
}

public class ReverseGeocodeResultDto
{
    /// <summary>Street address resolved from the pin (house number + road).</summary>
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    /// <summary>ISO 3166-1 alpha-2 country code, e.g. "PK", "US".</summary>
    public string? CountryCode { get; set; }
    /// <summary>Full formatted address string from the geocoding provider.</summary>
    public string? DisplayName { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
