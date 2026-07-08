using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Purchase Returns (Return-to-Vendor) — returns received goods to the vendor.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PurchaseReturnController : ControllerBase
{
    private readonly IPurchaseReturnService _service;
    private readonly ILogger<PurchaseReturnController> _logger;

    public PurchaseReturnController(IPurchaseReturnService service, ILogger<PurchaseReturnController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try { return Ok(await _service.GetAllAsync(pagination)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving returns"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase returns" }); }
    }

    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseReturnDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try { return Ok(new ApiResponse<List<PurchaseReturnDto>> { Success = true, Data = await _service.GetByVendorAsync(vendorId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving returns by vendor"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase returns" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry is null) return NotFound(new ApiErrorResponse { Message = "Return not found" });
            return Ok(new ApiResponse<PurchaseReturnDto> { Success = true, Data = entry });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving return {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase return" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseReturnDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var entry = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, new ApiResponse<PurchaseReturnDto> { Success = true, Data = entry, Message = "Purchase return created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating return"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating purchase return" }); }
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try { return Ok(new ApiResponse<PurchaseReturnDto> { Success = true, Data = await _service.ApproveAsync(id, GetUserId()), Message = "Return approved" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Return not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving return {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error approving return" }); }
    }

    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(Guid id)
    {
        try { return Ok(new ApiResponse<PurchaseReturnDto> { Success = true, Data = await _service.PostAsync(id, GetUserId()), Message = "Return posted" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Return not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error posting return {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error posting return" }); }
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseReturnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try { return Ok(new ApiResponse<PurchaseReturnDto> { Success = true, Data = await _service.CancelAsync(id), Message = "Return cancelled" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Return not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling return {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling return" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id); return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Return deleted" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Return not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting return {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting return" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
