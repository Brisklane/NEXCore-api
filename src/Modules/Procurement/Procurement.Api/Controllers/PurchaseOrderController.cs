using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Purchase Orders — legally binding commitments to buy from a vendor.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PurchaseOrderController : ControllerBase
{
    private readonly IPurchaseOrderService _service;
    private readonly ILogger<PurchaseOrderController> _logger;

    public PurchaseOrderController(IPurchaseOrderService service, ILogger<PurchaseOrderController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all purchase orders (paginated, with optional search / status filter / sort).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase orders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase orders" }); }
    }

    /// <summary>Get purchase order by ID (with full details).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var order = await _service.GetByIdAsync(id);
            if (order is null) return NotFound(new ApiErrorResponse { Message = "Purchase order not found" });
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase order" }); }
    }

    /// <summary>Get purchase order by number (e.g. PO-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var order = await _service.GetByNumberAsync(number);
            if (order is null) return NotFound(new ApiErrorResponse { Message = "Purchase order not found" });
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase order"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase order" }); }
    }

    /// <summary>Get purchase orders by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(PurchaseOrderStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<PurchaseOrderDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase orders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase orders" }); }
    }

    /// <summary>Get all orders for a specific vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<PurchaseOrderDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving purchase orders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase orders" }); }
    }

    /// <summary>Get orders awaiting goods receipt (receipt queue, paginated).</summary>
    [HttpGet("pending-receipt")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingReceipt([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetPendingReceiptAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending receipt orders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase orders" }); }
    }

    /// <summary>Get orders with uninvoiced received quantities (AP billing queue, paginated).</summary>
    [HttpGet("to-invoice")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetToInvoice([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetToInvoiceAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders to invoice"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving purchase orders" }); }
    }

    /// <summary>Update a draft purchase order (delivery, terms, lines).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseOrderDto dto)
    {
        try
        {
            var order = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating purchase order" }); }
    }

    /// <summary>Create a new purchase order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var order = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id },
                new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating purchase order"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating purchase order" }); }
    }

    /// <summary>Confirm a draft PO — commits it to the vendor and triggers budget encumbrance.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var order = await _service.ConfirmAsync(id, userId);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order confirmed" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error confirming PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error confirming purchase order" }); }
    }

    /// <summary>Mark PO as sent to vendor (email / portal dispatch).</summary>
    [HttpPost("{id:guid}/send-to-vendor")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendToVendor(Guid id)
    {
        try
        {
            var order = await _service.SendToVendorAsync(id);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order sent to vendor" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error sending PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error sending purchase order" }); }
    }

    /// <summary>Record vendor acknowledgement of the PO.</summary>
    [HttpPost("{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Acknowledge(Guid id)
    {
        try
        {
            var order = await _service.AcknowledgeAsync(id);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order acknowledged" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error acknowledging PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error acknowledging purchase order" }); }
    }

    /// <summary>Close a fully received and invoiced PO.</summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            var order = await _service.CloseAsync(id);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order closed" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error closing PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error closing purchase order" }); }
    }

    /// <summary>Cancel a purchase order.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelPurchaseOrderDto dto)
    {
        try
        {
            var order = await _service.CancelAsync(id, dto);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "Purchase order cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling purchase order" }); }
    }

    /// <summary>Delete a draft purchase order.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Purchase order deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Purchase order not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting PO {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting purchase order" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
