using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Payments — outgoing AP payments allocated across one or more vendor bills.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VendorPaymentController : ControllerBase
{
    private readonly IVendorPaymentService _service;
    private readonly ILogger<VendorPaymentController> _logger;

    public VendorPaymentController(IVendorPaymentService service, ILogger<VendorPaymentController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all vendor payments (paginated, with optional search / status filter / sort).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetAllAsync(pagination);
            return Ok(response);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving vendor payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving vendor payments" }); }
    }

    /// <summary>Get vendor payment by ID (with invoice allocations).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var payment = await _service.GetByIdAsync(id);
            if (payment is null) return NotFound(new ApiErrorResponse { Message = "Payment not found" });
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payment" }); }
    }

    /// <summary>Get payment by number (e.g. PAY-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var payment = await _service.GetByNumberAsync(number);
            if (payment is null) return NotFound(new ApiErrorResponse { Message = "Payment not found" });
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payment"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payment" }); }
    }

    /// <summary>Get payments by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(VendorPaymentStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<VendorPaymentDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    /// <summary>Get all payments to a specific vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorPaymentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<VendorPaymentDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving payments"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving payments" }); }
    }

    /// <summary>Update a draft vendor payment (date, bank account, reference).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorPaymentDto dto)
    {
        try
        {
            var payment = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating payment" }); }
    }

    /// <summary>Create a new vendor payment with invoice allocations.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVendorPaymentDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var payment = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = payment.Id },
                new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating vendor payment"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating payment" }); }
    }

    /// <summary>Approve a draft payment for processing.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var payment = await _service.ApproveAsync(id, userId);
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment approved" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error approving payment" }); }
    }

    /// <summary>Mark payment as sent to bank / vendor.</summary>
    [HttpPost("{id:guid}/mark-sent")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkSent(Guid id, [FromQuery] string? transactionReference = null)
    {
        try
        {
            var payment = await _service.MarkSentAsync(id, transactionReference);
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment marked as sent" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error marking payment as sent {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error marking payment as sent" }); }
    }

    /// <summary>Mark payment as cleared — updates invoice paid amounts and posts accounting entry.</summary>
    [HttpPost("{id:guid}/clear")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Clear(Guid id, [FromQuery] string? bankReferenceNumber = null)
    {
        try
        {
            var payment = await _service.ClearAsync(id, bankReferenceNumber);
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment cleared" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error clearing payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error clearing payment" }); }
    }

    /// <summary>Cancel a payment.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<VendorPaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var payment = await _service.CancelAsync(id);
            return Ok(new ApiResponse<VendorPaymentDto> { Success = true, Data = payment, Message = "Payment cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling payment" }); }
    }

    /// <summary>Delete a draft payment.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Payment deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Payment not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting payment {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting payment" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
