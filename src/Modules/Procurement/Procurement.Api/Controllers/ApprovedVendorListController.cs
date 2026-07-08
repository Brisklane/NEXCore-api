using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Approved Vendor List (AVL) / Source List — authorised vendors per item or category.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApprovedVendorListController : ControllerBase
{
    private readonly IApprovedVendorListService _service;
    private readonly ILogger<ApprovedVendorListController> _logger;

    public ApprovedVendorListController(IApprovedVendorListService service, ILogger<ApprovedVendorListController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all AVL entries (paginated).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedVendorListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try { return Ok(await _service.GetAllAsync(pagination)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving AVL entries"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving approved vendor list" }); }
    }

    /// <summary>Get AVL entries for a vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovedVendorListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try { return Ok(new ApiResponse<List<ApprovedVendorListDto>> { Success = true, Data = await _service.GetByVendorAsync(vendorId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving AVL by vendor"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving approved vendor list" }); }
    }

    /// <summary>Get AVL entries for an item.</summary>
    [HttpGet("by-item/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovedVendorListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByItem(Guid itemId)
    {
        try { return Ok(new ApiResponse<List<ApprovedVendorListDto>> { Success = true, Data = await _service.GetByItemAsync(itemId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving AVL by item"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving approved vendor list" }); }
    }

    /// <summary>Get AVL entries for a procurement category.</summary>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovedVendorListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        try { return Ok(new ApiResponse<List<ApprovedVendorListDto>> { Success = true, Data = await _service.GetByCategoryAsync(categoryId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving AVL by category"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving approved vendor list" }); }
    }

    /// <summary>Get an AVL entry by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovedVendorListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry is null) return NotFound(new ApiErrorResponse { Message = "AVL entry not found" });
            return Ok(new ApiResponse<ApprovedVendorListDto> { Success = true, Data = entry });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving AVL entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving approved vendor list entry" }); }
    }

    /// <summary>Create a new AVL entry.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApprovedVendorListDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateApprovedVendorListDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var entry = await _service.CreateAsync(dto, GetUserId());
            return CreatedAtAction(nameof(GetById), new { id = entry.Id },
                new ApiResponse<ApprovedVendorListDto> { Success = true, Data = entry, Message = "Approved vendor entry created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating AVL entry"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating approved vendor entry" }); }
    }

    /// <summary>Update an AVL entry.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovedVendorListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApprovedVendorListDto dto)
    {
        try { return Ok(new ApiResponse<ApprovedVendorListDto> { Success = true, Data = await _service.UpdateAsync(id, dto), Message = "Approved vendor entry updated" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "AVL entry not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating AVL entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating approved vendor entry" }); }
    }

    /// <summary>Block a vendor from supplying the item/category.</summary>
    [HttpPost("{id:guid}/block")]
    [ProducesResponseType(typeof(ApiResponse<ApprovedVendorListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockApprovedVendorDto dto)
    {
        try { return Ok(new ApiResponse<ApprovedVendorListDto> { Success = true, Data = await _service.BlockAsync(id, dto), Message = "Vendor blocked for this scope" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "AVL entry not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error blocking AVL entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error blocking approved vendor entry" }); }
    }

    /// <summary>Unblock a previously blocked AVL entry.</summary>
    [HttpPost("{id:guid}/unblock")]
    [ProducesResponseType(typeof(ApiResponse<ApprovedVendorListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unblock(Guid id)
    {
        try { return Ok(new ApiResponse<ApprovedVendorListDto> { Success = true, Data = await _service.UnblockAsync(id), Message = "Vendor unblocked for this scope" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "AVL entry not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error unblocking AVL entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error unblocking approved vendor entry" }); }
    }

    /// <summary>Delete an AVL entry.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Approved vendor entry deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "AVL entry not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting AVL entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting approved vendor entry" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
