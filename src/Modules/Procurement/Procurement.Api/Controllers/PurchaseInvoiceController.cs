using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Purchase Invoices (Vendor Bills) — AP invoice lifecycle including 3-way matching.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PurchaseInvoiceController : ControllerBase
{
    private readonly IPurchaseInvoiceService _service;
    private readonly ILogger<PurchaseInvoiceController> _logger;

    public PurchaseInvoiceController(IPurchaseInvoiceService service, ILogger<PurchaseInvoiceController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all purchase invoices (paginated, with optional search / status filter / sort).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get invoice by ID (with lines and match records).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var invoice = await _service.GetByIdAsync(id);
            if (invoice is null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoice" }); }
    }

    /// <summary>Get invoice by internal number (e.g. BILL-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var invoice = await _service.GetByNumberAsync(number);
            if (invoice is null) return NotFound(new ApiErrorResponse { Message = "Invoice not found" });
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoice"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoice" }); }
    }

    /// <summary>Get invoices by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(PurchaseInvoiceStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<PurchaseInvoiceDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get all invoices from a specific vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<PurchaseInvoiceDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get invoices linked to a purchase order.</summary>
    [HttpGet("by-purchase-order/{purchaseOrderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseInvoiceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPurchaseOrder(Guid purchaseOrderId)
    {
        try
        {
            var list = await _service.GetByPurchaseOrderAsync(purchaseOrderId);
            return Ok(new ApiResponse<List<PurchaseInvoiceDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get invoices past their due date with unpaid balance (paginated).</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetOverdueAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving overdue invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Get posted invoices awaiting payment (paginated).</summary>
    [HttpGet("pending-payment")]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingPayment([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetPendingPaymentAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending payment invoices"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving invoices" }); }
    }

    /// <summary>Update a draft invoice (dates, lines, dimensions).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseInvoiceDto dto)
    {
        try
        {
            var invoice = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating invoice" }); }
    }

    /// <summary>Create a new vendor bill from a PO / GRN.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var invoice = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id },
                new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating invoice"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating invoice" }); }
    }

    /// <summary>Approve an invoice before posting.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var invoice = await _service.ApproveAsync(id, userId);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice approved" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error approving invoice" }); }
    }

    /// <summary>Post an invoice — creates AP journal entry and updates PO invoiced amounts.</summary>
    [HttpPost("{id:guid}/post")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PostInvoice(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var invoice = await _service.PostAsync(id, userId);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice posted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error posting invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error posting invoice" }); }
    }

    /// <summary>Place an invoice on hold pending clarification.</summary>
    [HttpPost("{id:guid}/hold")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Hold(Guid id, [FromBody] HoldInvoiceDto dto)
    {
        try
        {
            var invoice = await _service.HoldAsync(id, dto);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice placed on hold" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error holding invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error holding invoice" }); }
    }

    /// <summary>Release an invoice from hold.</summary>
    [HttpPost("{id:guid}/release-hold")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReleaseHold(Guid id)
    {
        try
        {
            var invoice = await _service.ReleaseHoldAsync(id);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice hold released" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error releasing invoice hold {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error releasing invoice hold" }); }
    }

    /// <summary>Raise a dispute on an invoice (price/quantity discrepancy).</summary>
    [HttpPost("{id:guid}/dispute")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dispute(Guid id, [FromBody] DisputeInvoiceDto dto)
    {
        try
        {
            var invoice = await _service.DisputeAsync(id, dto);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice disputed" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error disputing invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error disputing invoice" }); }
    }

    /// <summary>Resolve an invoice dispute.</summary>
    [HttpPost("{id:guid}/resolve-dispute")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveDispute(Guid id)
    {
        try
        {
            var invoice = await _service.ResolveDisputeAsync(id);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice dispute resolved" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error resolving invoice dispute {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error resolving dispute" }); }
    }

    /// <summary>Cancel an invoice.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var invoice = await _service.CancelAsync(id);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = "Invoice cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling invoice" }); }
    }

    /// <summary>Run three-way match (PO vs GRN vs Invoice) — updates matching status.</summary>
    [HttpPost("{id:guid}/three-way-match")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseInvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunThreeWayMatch(Guid id)
    {
        try
        {
            var invoice = await _service.RunThreeWayMatchAsync(id);
            return Ok(new ApiResponse<PurchaseInvoiceDto> { Success = true, Data = invoice, Message = $"Three-way match completed: {invoice.MatchingStatus}" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error running three-way match for invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error running three-way match" }); }
    }

    /// <summary>Delete a draft invoice.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Invoice deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Invoice not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting invoice {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting invoice" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
