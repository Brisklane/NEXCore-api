using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Manages the full approval lifecycle for HR entities (Job, OfferLetter, etc.).
///
/// Typical workflow for a Job Requisition:
///   1. POST /approval           — Submit a new approval request (creates steps from WorkflowConfig)
///   2. GET  /approval/{id}      — View request + all steps
///   3. POST /approval/{id}/approve  — Approver approves the current step
///   4. POST /approval/{id}/reject   — Approver rejects (terminates the request)
///   5. POST /approval/{id}/delegate — Reassign current step to another employee
///   6. POST /approval/{id}/cancel   — Requester cancels before completion
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _service;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(IApprovalService service, ILogger<ApprovalController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // Queries

    /// <summary>Get all approval requests for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApprovalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var data = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<ApprovalRequestDto>> { Success = true, Data = data });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approval requests"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get a single approval request with all its steps.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Approval request not found" });
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approval request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Get all approval requests for a specific entity, e.g. all approvals for Job id XYZ.
    /// entityType = "Job" | "OfferLetter" | any other EntityType value.
    /// </summary>
    [HttpGet("by-entity/{entityType}/{entityId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApprovalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(string entityType, Guid entityId)
    {
        try
        {
            var data = await _service.GetByEntityAsync(entityType, entityId);
            return Ok(new ApiResponse<IEnumerable<ApprovalRequestDto>> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving approvals for {EntityType}/{EntityId}", entityType, entityId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Get all requests that are currently pending action by the given approver employee.
    /// Use this to build the "My Pending Approvals" inbox.
    /// </summary>
    [HttpGet("pending/employee/{approverEmployeeId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApprovalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingForApprover(Guid approverEmployeeId)
    {
        try
        {
            var data = await _service.GetPendingForApproverAsync(approverEmployeeId);
            return Ok(new ApiResponse<IEnumerable<ApprovalRequestDto>> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pending approvals for employee {Id}", approverEmployeeId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // Commands

    /// <summary>
    /// Submit a new approval request for an HR entity.
    /// The service auto-resolves the matching WorkflowConfig and creates all approval steps.
    /// If WorkflowConfigId is omitted the system will find the active config for the EntityType.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitApprovalRequestDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.SubmitAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Approval request submitted successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error submitting approval request"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Approve the current pending step.
    /// If this is the final level the request is marked fully Approved.
    /// Otherwise it advances to the next level.
    /// </summary>
    [HttpPost("{id}/approve")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveStep(Guid id, [FromBody] ApproveStepDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.ApproveStepAsync(id, dto, userId);
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Step approved", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error approving step for request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Reject the current pending step.
    /// This terminates the entire approval request with status Rejected.
    /// </summary>
    [HttpPost("{id}/reject")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectStep(Guid id, [FromBody] RejectStepDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.RejectStepAsync(id, dto, userId);
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Step rejected", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error rejecting step for request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Delegate the current pending step to another employee.
    /// The original approver remains on record; the delegated employee gains the ability to act.
    /// </summary>
    [HttpPost("{id}/delegate")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DelegateStep(Guid id, [FromBody] DelegateStepDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.DelegateStepAsync(id, dto, userId);
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Step delegated", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error delegating step for request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Cancel a pending approval request.
    /// Not allowed once the request is already Approved or Rejected.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason = null)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CancelAsync(id, reason, userId);
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Approval request cancelled", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling approval request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Update mutable header fields (priority, comments, labels) of a pending request.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApprovalRequestDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ApprovalRequestDto> { Success = true, Message = "Approval request updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating approval request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Soft-delete an approval request.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting approval request {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
