using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Goods Receipts — records physical delivery of goods against a purchase order (GRN).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class GoodsReceiptController : ControllerBase
{
    private readonly IGoodsReceiptService _service;
    private readonly ILogger<GoodsReceiptController> _logger;

    public GoodsReceiptController(IGoodsReceiptService service, ILogger<GoodsReceiptController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all goods receipts (paginated, with optional search / status filter / sort).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipts" }); }
    }

    /// <summary>Get goods receipt by ID (with lines).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var r = await _service.GetByIdAsync(id);
            if (r is null) return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" });
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipt" }); }
    }

    /// <summary>Get goods receipt by number (e.g. GRN-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var r = await _service.GetByNumberAsync(number);
            if (r is null) return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" });
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipt"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipt" }); }
    }

    /// <summary>Get goods receipts by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<GoodsReceiptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(GoodsReceiptStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<GoodsReceiptDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipts" }); }
    }

    /// <summary>Get all goods receipts for a purchase order.</summary>
    [HttpGet("by-purchase-order/{purchaseOrderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<GoodsReceiptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPurchaseOrder(Guid purchaseOrderId)
    {
        try
        {
            var list = await _service.GetByPurchaseOrderAsync(purchaseOrderId);
            return Ok(new ApiResponse<List<GoodsReceiptDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipts" }); }
    }

    /// <summary>Get all goods receipts for a vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<GoodsReceiptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<GoodsReceiptDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving goods receipts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving goods receipts" }); }
    }

    /// <summary>Update a draft goods receipt (delivery note, dates, warehouse).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoodsReceiptDto dto)
    {
        try
        {
            var r = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r, Message = "Goods receipt updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating goods receipt" }); }
    }

    /// <summary>Create a draft goods receipt note against a PO.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var r = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = r.Id },
                new ApiResponse<GoodsReceiptDto> { Success = true, Data = r, Message = "Goods receipt created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating goods receipt"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating goods receipt" }); }
    }

    /// <summary>Post a goods receipt — updates PO received quantities and creates inventory/accounting entries.</summary>
    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Post(Guid id, [FromQuery] Guid? fiscalPeriodId = null)
    {
        try
        {
            var userId = GetUserId();
            var r = await _service.PostAsync(id, userId, fiscalPeriodId);
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r, Message = "Goods receipt posted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error posting goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error posting goods receipt" }); }
    }

    /// <summary>Cancel a goods receipt.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var r = await _service.CancelAsync(id);
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r, Message = "Goods receipt cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling goods receipt" }); }
    }

    /// <summary>Record quality inspection results for receipt lines.</summary>
    [HttpPost("{id:guid}/inspect")]
    [ProducesResponseType(typeof(ApiResponse<GoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Inspect(Guid id, [FromBody] List<InspectGoodsReceiptLineDto> inspections)
    {
        try
        {
            var userId = GetUserId();
            var r = await _service.InspectLinesAsync(id, inspections, userId);
            return Ok(new ApiResponse<GoodsReceiptDto> { Success = true, Data = r, Message = "Inspection results recorded" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error inspecting goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error recording inspection" }); }
    }

    /// <summary>Delete a draft goods receipt.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Goods receipt deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Goods receipt not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting goods receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting goods receipt" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
