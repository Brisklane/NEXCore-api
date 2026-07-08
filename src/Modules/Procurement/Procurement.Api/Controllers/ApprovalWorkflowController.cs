using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Approval Workflows — configure multi-level approval routing for POs, invoices, requisitions, and contracts.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly IApprovalWorkflowService _service;
    private readonly ILogger<ApprovalWorkflowController> _logger;

    public ApprovalWorkflowController(IApprovalWorkflowService service, ILogger<ApprovalWorkflowController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all approval workflows.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovalWorkflowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _service.GetAllAsync();
            return Ok(new ApiResponse<List<ApprovalWorkflowDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflows"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving workflows" }); }
    }

    /// <summary>Get workflows configured for a specific document type (e.g. PurchaseOrder).</summary>
    [HttpGet("by-document-type/{documentType}")]
    [ProducesResponseType(typeof(ApiResponse<List<ApprovalWorkflowDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDocumentType(ApprovalDocumentType documentType)
    {
        try
        {
            var list = await _service.GetByDocumentTypeAsync(documentType);
            return Ok(new ApiResponse<List<ApprovalWorkflowDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflows"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving workflows" }); }
    }

    /// <summary>Get approval workflow by ID (with steps).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var wf = await _service.GetByIdAsync(id);
            if (wf is null) return NotFound(new ApiErrorResponse { Message = "Approval workflow not found" });
            return Ok(new ApiResponse<ApprovalWorkflowDto> { Success = true, Data = wf });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving workflow {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving workflow" }); }
    }

    /// <summary>Create a new approval workflow with steps.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateApprovalWorkflowDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var wf = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = wf.Id },
                new ApiResponse<ApprovalWorkflowDto> { Success = true, Data = wf, Message = "Approval workflow created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating workflow"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating workflow" }); }
    }

    /// <summary>Update an approval workflow and its steps.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApprovalWorkflowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateApprovalWorkflowDto dto)
    {
        try
        {
            var wf = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<ApprovalWorkflowDto> { Success = true, Data = wf, Message = "Approval workflow updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Approval workflow not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating workflow {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating workflow" }); }
    }

    /// <summary>Delete an approval workflow.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Approval workflow deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Approval workflow not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting workflow {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting workflow" }); }
    }
}
