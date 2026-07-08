using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Requests for Quotation — sourcing events sent to vendors to collect and compare bids.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class RequestForQuotationController : ControllerBase
{
    private readonly IRequestForQuotationService _service;
    private readonly ILogger<RequestForQuotationController> _logger;

    public RequestForQuotationController(IRequestForQuotationService service, ILogger<RequestForQuotationController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all RFQs.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving RFQs"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving RFQs" }); }
    }

    /// <summary>Get RFQ by ID (with lines, vendors, quotations).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var rfq = await _service.GetByIdAsync(id);
            if (rfq is null) return NotFound(new ApiErrorResponse { Message = "RFQ not found" });
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving RFQ" }); }
    }

    /// <summary>Get RFQ by number (e.g. RFQ-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var rfq = await _service.GetByNumberAsync(number);
            if (rfq is null) return NotFound(new ApiErrorResponse { Message = "RFQ not found" });
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving RFQ"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving RFQ" }); }
    }

    /// <summary>Get RFQs by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<RequestForQuotationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(RFQStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<RequestForQuotationDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving RFQs"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving RFQs" }); }
    }

    /// <summary>Get RFQs originating from a specific purchase requisition.</summary>
    [HttpGet("by-requisition/{requisitionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<RequestForQuotationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRequisition(Guid requisitionId)
    {
        try
        {
            var list = await _service.GetByRequisitionAsync(requisitionId);
            return Ok(new ApiResponse<List<RequestForQuotationDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving RFQs"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving RFQs" }); }
    }

    /// <summary>Update a draft RFQ (title, deadline, terms).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRFQDto dto)
    {
        try
        {
            var rfq = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "RFQ updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating RFQ" }); }
    }

    /// <summary>Create a new RFQ and invite vendors.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRFQDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var rfq = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = rfq.Id },
                new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "RFQ created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating RFQ"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating RFQ" }); }
    }

    /// <summary>Send the RFQ to all invited vendors.</summary>
    [HttpPost("{id:guid}/send")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Send(Guid id)
    {
        try
        {
            var rfq = await _service.SendToVendorsAsync(id);
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "RFQ sent to vendors" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error sending RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error sending RFQ" }); }
    }

    /// <summary>Close the RFQ (deadline passed, no more submissions).</summary>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            var rfq = await _service.CloseAsync(id);
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "RFQ closed" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error closing RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error closing RFQ" }); }
    }

    /// <summary>Cancel the RFQ.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            var rfq = await _service.CancelAsync(id);
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "RFQ cancelled" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error cancelling RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error cancelling RFQ" }); }
    }

    /// <summary>Delete a draft RFQ.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "RFQ deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting RFQ {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting RFQ" }); }
    }

    /// <summary>Submit a vendor quotation in response to an RFQ.</summary>
    [HttpPost("{rfqId:guid}/quotations")]
    [ProducesResponseType(typeof(ApiResponse<VendorQuotationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitQuotation(Guid rfqId, [FromBody] SubmitVendorQuotationDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var quotation = await _service.SubmitQuotationAsync(rfqId, dto);
            return StatusCode(201, new ApiResponse<VendorQuotationDto> { Success = true, Data = quotation, Message = "Quotation submitted" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error submitting quotation for RFQ {RfqId}", rfqId); return StatusCode(500, new ApiErrorResponse { Message = "Error submitting quotation" }); }
    }

    /// <summary>Score and evaluate vendor quotations (technical and commercial scores).</summary>
    [HttpPost("{rfqId:guid}/evaluate")]
    [ProducesResponseType(typeof(ApiResponse<RequestForQuotationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Evaluate(Guid rfqId, [FromBody] List<EvaluateQuotationDto> evaluations)
    {
        try
        {
            var rfq = await _service.EvaluateQuotationsAsync(rfqId, evaluations);
            return Ok(new ApiResponse<RequestForQuotationDto> { Success = true, Data = rfq, Message = "Quotations evaluated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "RFQ not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error evaluating RFQ {RfqId}", rfqId); return StatusCode(500, new ApiErrorResponse { Message = "Error evaluating quotations" }); }
    }

    /// <summary>Award the RFQ to the winning quotation and create a draft PO.</summary>
    [HttpPost("{rfqId:guid}/award/{quotationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Award(Guid rfqId, Guid quotationId)
    {
        try
        {
            var order = await _service.AwardAndCreatePurchaseOrderAsync(rfqId, quotationId);
            return StatusCode(201, new ApiResponse<PurchaseOrderDto> { Success = true, Data = order, Message = "RFQ awarded and purchase order created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error awarding RFQ {RfqId}", rfqId); return StatusCode(500, new ApiErrorResponse { Message = "Error awarding RFQ" }); }
    }
}
