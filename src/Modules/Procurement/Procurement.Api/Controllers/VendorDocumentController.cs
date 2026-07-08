using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Documents — compliance certificates, licences and registrations with expiry tracking.</summary>
[ApiController]
[Route("api/v1/vendors/{vendorId:guid}/documents")]
[Produces("application/json")]
[Authorize]
public class VendorDocumentController : ControllerBase
{
    private readonly IVendorDocumentService _service;
    private readonly ILogger<VendorDocumentController> _logger;

    public VendorDocumentController(IVendorDocumentService service, ILogger<VendorDocumentController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all documents for a vendor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<VendorDocumentDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor documents"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving documents" }); }
    }

    /// <summary>Get a specific document by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid vendorId, Guid id)
    {
        try
        {
            var doc = await _service.GetByIdAsync(id);
            if (doc is null) return NotFound(new ApiErrorResponse { Message = "Document not found" });
            return Ok(new ApiResponse<VendorDocumentDto> { Success = true, Data = doc });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving document {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving document" }); }
    }

    /// <summary>Upload / register a new compliance document for a vendor.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorDocumentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid vendorId, [FromBody] CreateVendorDocumentDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var doc = await _service.CreateAsync(vendorId, dto);
            return StatusCode(201, new ApiResponse<VendorDocumentDto> { Success = true, Data = doc, Message = "Document registered" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating document"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating document" }); }
    }

    /// <summary>Update a vendor document (expiry date, status, file path).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid vendorId, Guid id, [FromBody] UpdateVendorDocumentDto dto)
    {
        try
        {
            var doc = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<VendorDocumentDto> { Success = true, Data = doc, Message = "Document updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Document not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating document {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating document" }); }
    }

    /// <summary>Mark a document as verified by a compliance officer.</summary>
    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(ApiResponse<VendorDocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify(Guid vendorId, Guid id)
    {
        try
        {
            var userId = GetUserId();
            var doc = await _service.VerifyAsync(id, userId);
            return Ok(new ApiResponse<VendorDocumentDto> { Success = true, Data = doc, Message = "Document verified" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Document not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error verifying document {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error verifying document" }); }
    }

    /// <summary>Delete a vendor document.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid vendorId, Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Document deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Document not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting document {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting document" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}

/// <summary>Cross-vendor document expiry alerts — not scoped to a single vendor.</summary>
[ApiController]
[Route("api/v1/vendor-documents")]
[Produces("application/json")]
[Authorize]
public class VendorDocumentExpiryController : ControllerBase
{
    private readonly IVendorDocumentService _service;
    private readonly ILogger<VendorDocumentExpiryController> _logger;

    public VendorDocumentExpiryController(IVendorDocumentService service, ILogger<VendorDocumentExpiryController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all vendor documents expiring within N days (compliance alert dashboard).</summary>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiring([FromQuery] int withinDays = 30)
    {
        try
        {
            var list = await _service.GetExpiringAsync(withinDays);
            return Ok(new ApiResponse<List<VendorDocumentDto>> { Success = true, Data = list, Message = $"{list.Count} document(s) expiring within {withinDays} days" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving expiring documents"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving documents" }); }
    }
}
