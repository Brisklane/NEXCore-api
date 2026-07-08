using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Purchase Requisitions — internal purchase requests raised by departments before a PO is issued.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PurchaseRequisitionController : ControllerBase
{
    private readonly IPurchaseRequisitionService _service;
    private readonly ILogger<PurchaseRequisitionController> _logger;

    public PurchaseRequisitionController(IPurchaseRequisitionService service, ILogger<PurchaseRequisitionController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all purchase requisitions (paginated).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisitions" }); }
    }

    /// <summary>Get requisition by ID (with lines).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var r = await _service.GetByIdAsync(id);
            if (r is null) return NotFound(new ApiErrorResponse { Message = "Requisition not found" });
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisition" }); }
    }

    /// <summary>Get requisition by number (e.g. PR-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var r = await _service.GetByNumberAsync(number);
            if (r is null) return NotFound(new ApiErrorResponse { Message = "Requisition not found" });
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisition"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisition" }); }
    }

    /// <summary>Get requisitions by lifecycle status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseRequisitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(RequisitionStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<PurchaseRequisitionDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisitions" }); }
    }

    /// <summary>Get requisitions raised by a specific user.</summary>
    [HttpGet("by-requester/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseRequisitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRequester(Guid userId)
    {
        try
        {
            var list = await _service.GetByRequesterAsync(userId);
            return Ok(new ApiResponse<List<PurchaseRequisitionDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisitions" }); }
    }

    /// <summary>Get requisitions for a department.</summary>
    [HttpGet("by-department/{departmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseRequisitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDepartment(Guid departmentId)
    {
        try
        {
            var list = await _service.GetByDepartmentAsync(departmentId);
            return Ok(new ApiResponse<List<PurchaseRequisitionDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving requisitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisitions" }); }
    }

    /// <summary>Get requisitions awaiting approval action.</summary>
    [HttpGet("pending-approval")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseRequisitionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApproval()
    {
        try
        {
            var list = await _service.GetPendingApprovalAsync();
            return Ok(new ApiResponse<List<PurchaseRequisitionDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending requisitions"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving requisitions" }); }
    }

    /// <summary>Update a draft purchase requisition (lines, dates, priority).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseRequisitionDto dto)
    {
        try
        {
            var r = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating requisition" }); }
    }

    /// <summary>Create a new purchase requisition.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseRequisitionDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var r = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = r.Id },
                new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating requisition"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating requisition" }); }
    }

    /// <summary>Submit a draft requisition for approval.</summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            var r = await _service.SubmitAsync(id);
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition submitted for approval" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error submitting requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error submitting requisition" }); }
    }

    /// <summary>Approve a submitted requisition.</summary>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var r = await _service.ApproveAsync(id, userId);
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition approved" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error approving requisition" }); }
    }

    /// <summary>Reject a submitted requisition with a reason.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequisitionDto dto)
    {
        try
        {
            var userId = GetUserId();
            var r = await _service.RejectAsync(id, dto, userId);
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition rejected" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error rejecting requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error rejecting requisition" }); }
    }

    /// <summary>Cancel a requisition.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseRequisitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var r = await _service.CancelAsync(id);
            return Ok(new ApiResponse<PurchaseRequisitionDto> { Success = true, Data = r, Message = "Requisition cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling requisition" }); }
    }

    /// <summary>Convert an approved requisition directly into a draft Purchase Order.</summary>
    [HttpPost("{id:guid}/convert-to-order")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConvertToOrder(Guid id)
    {
        try
        {
            var order = await _service.ConvertToPurchaseOrderAsync(id);
            return Ok(new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = $"Purchase order {order.OrderNumber} created from requisition" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error converting requisition {Id} to purchase order", id); return StatusCode(500, new ApiErrorResponse { Message = "Error converting requisition to purchase order" }); }
    }

    /// <summary>Delete a draft requisition.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Requisition deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Requisition not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting requisition {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting requisition" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
