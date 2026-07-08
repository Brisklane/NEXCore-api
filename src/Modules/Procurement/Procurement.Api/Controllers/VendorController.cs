using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Vendor master — supplier registration, contacts, addresses, bank accounts, and status lifecycle.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VendorController : ControllerBase
{
    private readonly IVendorService _vendors;
    private readonly ILogger<VendorController> _logger;

    public VendorController(IVendorService vendors, ILogger<VendorController> logger)
    {
        _vendors = vendors;
        _logger  = logger;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>Get all vendors.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _vendors.GetAllAsync(pagination);
            return Ok(response);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendors"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendors" }); }
    }

    /// <summary>Get vendor by ID (includes contacts, addresses, bank accounts).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var vendor = await _vendors.GetByIdAsync(id);
            if (vendor is null) return NotFound(new ApiErrorResponse { Message = "Vendor not found" });
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor" }); }
    }

    /// <summary>Get vendor by vendor number (e.g. V-00001).</summary>
    [HttpGet("number/{vendorNumber}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string vendorNumber)
    {
        try
        {
            var vendor = await _vendors.GetByNumberAsync(vendorNumber);
            if (vendor is null) return NotFound(new ApiErrorResponse { Message = "Vendor not found" });
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor {Number}", vendorNumber); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor" }); }
    }

    /// <summary>Get vendors filtered by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(VendorStatus status)
    {
        try
        {
            var list = await _vendors.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<VendorDto>> { Success = true, Data = list, Message = $"{list.Count} vendor(s)" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendors by status"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendors" }); }
    }

    /// <summary>Get vendors by onboarding status.</summary>
    [HttpGet("by-onboarding-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOnboardingStatus(VendorOnboardingStatus status)
    {
        try
        {
            var list = await _vendors.GetByOnboardingStatusAsync(status);
            return Ok(new ApiResponse<List<VendorDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendors"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendors" }); }
    }

    /// <summary>Get active preferred vendors.</summary>
    [HttpGet("preferred")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferred()
    {
        try
        {
            var list = await _vendors.GetPreferredVendorsAsync();
            return Ok(new ApiResponse<List<VendorDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving preferred vendors"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendors" }); }
    }

    /// <summary>Get vendors by category.</summary>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        try
        {
            var list = await _vendors.GetByCategoryAsync(categoryId);
            return Ok(new ApiResponse<List<VendorDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendors by category"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendors" }); }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Register a new vendor.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVendorDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var vendor = await _vendors.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = vendor.Id },
                new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor registered successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating vendor"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating vendor" }); }
    }

    /// <summary>Update vendor master data.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorDto dto)
    {
        try
        {
            var vendor = await _vendors.UpdateAsync(id, dto);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating vendor" }); }
    }

    /// <summary>Approve vendor — moves to Active status.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var vendor = await _vendors.ApproveAsync(id, userId);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor approved" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error approving vendor" }); }
    }

    /// <summary>Deactivate a vendor (Active/Blocked → Inactive). Required before deletion.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            var vendor = await _vendors.DeactivateAsync(id);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor deactivated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deactivating vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deactivating vendor" }); }
    }

    /// <summary>Reactivate a vendor (Inactive → Active).</summary>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            var vendor = await _vendors.ReactivateAsync(id);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor reactivated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error reactivating vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error reactivating vendor" }); }
    }

    /// <summary>Block a vendor from transacting.</summary>
    [HttpPost("{id:guid}/block")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockVendorDto dto)
    {
        try
        {
            var userId = GetUserId();
            var vendor = await _vendors.BlockAsync(id, dto, userId);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor blocked" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error blocking vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error blocking vendor" }); }
    }

    /// <summary>Remove vendor block and restore to Active.</summary>
    [HttpPost("{id:guid}/unblock")]
    [ProducesResponseType(typeof(ApiResponse<VendorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unblock(Guid id)
    {
        try
        {
            var vendor = await _vendors.UnblockAsync(id);
            return Ok(new ApiResponse<VendorDto> { Success = true, Data = vendor, Message = "Vendor unblocked" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error unblocking vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error unblocking vendor" }); }
    }

    /// <summary>Soft-delete a vendor (draft / pending-approval only).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _vendors.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Vendor deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting vendor {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting vendor" }); }
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    /// <summary>Add a contact to a vendor.</summary>
    [HttpPost("{vendorId:guid}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<VendorContactDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddContact(Guid vendorId, [FromBody] VendorContactDto dto)
    {
        try
        {
            var contact = await _vendors.AddContactAsync(vendorId, dto);
            return StatusCode(201, new ApiResponse<VendorContactDto> { Success = true, Data = contact, Message = "Contact added" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding contact"); return StatusCode(500, new ApiErrorResponse { Message = "Error adding contact" }); }
    }

    /// <summary>Update a vendor contact.</summary>
    [HttpPut("{vendorId:guid}/contacts/{contactId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorContactDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateContact(Guid vendorId, Guid contactId, [FromBody] VendorContactDto dto)
    {
        try
        {
            var contact = await _vendors.UpdateContactAsync(vendorId, contactId, dto);
            return Ok(new ApiResponse<VendorContactDto> { Success = true, Data = contact, Message = "Contact updated" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating contact"); return StatusCode(500, new ApiErrorResponse { Message = "Error updating contact" }); }
    }

    /// <summary>Remove a contact from a vendor.</summary>
    [HttpDelete("{vendorId:guid}/contacts/{contactId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteContact(Guid vendorId, Guid contactId)
    {
        try
        {
            await _vendors.DeleteContactAsync(vendorId, contactId);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Contact removed" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting contact"); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting contact" }); }
    }

    // ── Addresses ─────────────────────────────────────────────────────────────

    /// <summary>Add an address to a vendor.</summary>
    [HttpPost("{vendorId:guid}/addresses")]
    [ProducesResponseType(typeof(ApiResponse<VendorAddressDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddAddress(Guid vendorId, [FromBody] VendorAddressDto dto)
    {
        try
        {
            var address = await _vendors.AddAddressAsync(vendorId, dto);
            return StatusCode(201, new ApiResponse<VendorAddressDto> { Success = true, Data = address, Message = "Address added" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding address"); return StatusCode(500, new ApiErrorResponse { Message = "Error adding address" }); }
    }

    /// <summary>Update a vendor address.</summary>
    [HttpPut("{vendorId:guid}/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorAddressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAddress(Guid vendorId, Guid addressId, [FromBody] VendorAddressDto dto)
    {
        try
        {
            var address = await _vendors.UpdateAddressAsync(vendorId, addressId, dto);
            return Ok(new ApiResponse<VendorAddressDto> { Success = true, Data = address, Message = "Address updated" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating address"); return StatusCode(500, new ApiErrorResponse { Message = "Error updating address" }); }
    }

    /// <summary>Remove an address from a vendor.</summary>
    [HttpDelete("{vendorId:guid}/addresses/{addressId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAddress(Guid vendorId, Guid addressId)
    {
        try
        {
            await _vendors.DeleteAddressAsync(vendorId, addressId);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Address removed" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting address"); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting address" }); }
    }

    // ── Bank Accounts ─────────────────────────────────────────────────────────

    /// <summary>Add a bank account to a vendor.</summary>
    [HttpPost("{vendorId:guid}/bank-accounts")]
    [ProducesResponseType(typeof(ApiResponse<VendorBankAccountDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddBankAccount(Guid vendorId, [FromBody] VendorBankAccountDto dto)
    {
        try
        {
            var account = await _vendors.AddBankAccountAsync(vendorId, dto);
            return StatusCode(201, new ApiResponse<VendorBankAccountDto> { Success = true, Data = account, Message = "Bank account added" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Vendor not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding bank account"); return StatusCode(500, new ApiErrorResponse { Message = "Error adding bank account" }); }
    }

    /// <summary>Update a vendor bank account.</summary>
    [HttpPut("{vendorId:guid}/bank-accounts/{bankAccountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorBankAccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBankAccount(Guid vendorId, Guid bankAccountId, [FromBody] VendorBankAccountDto dto)
    {
        try
        {
            var account = await _vendors.UpdateBankAccountAsync(vendorId, bankAccountId, dto);
            return Ok(new ApiResponse<VendorBankAccountDto> { Success = true, Data = account, Message = "Bank account updated" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating bank account"); return StatusCode(500, new ApiErrorResponse { Message = "Error updating bank account" }); }
    }

    /// <summary>Remove a bank account from a vendor.</summary>
    [HttpDelete("{vendorId:guid}/bank-accounts/{bankAccountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteBankAccount(Guid vendorId, Guid bankAccountId)
    {
        try
        {
            await _vendors.DeleteBankAccountAsync(vendorId, bankAccountId);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Bank account removed" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting bank account"); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting bank account" }); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
