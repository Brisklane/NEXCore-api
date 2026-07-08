using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Debit Notes — AP credit claims raised against posted purchase returns.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class VendorDebitNoteController : ControllerBase
{
    private readonly IVendorDebitNoteService _service;
    private readonly ILogger<VendorDebitNoteController> _logger;

    public VendorDebitNoteController(IVendorDebitNoteService service, ILogger<VendorDebitNoteController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<VendorDebitNoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try { return Ok(await _service.GetAllAsync(pagination)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving debit notes"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving debit notes" }); }
    }

    [HttpGet("eligible-returns")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseReturnDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEligibleReturns()
    {
        try { return Ok(new ApiResponse<List<PurchaseReturnDto>> { Success = true, Data = await _service.GetEligibleReturnsAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving eligible returns"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving eligible returns" }); }
    }

    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorDebitNoteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try { return Ok(new ApiResponse<List<VendorDebitNoteDto>> { Success = true, Data = await _service.GetByVendorAsync(vendorId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving debit notes by vendor"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving debit notes" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorDebitNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry is null) return NotFound(new ApiErrorResponse { Message = "Debit note not found" });
            return Ok(new ApiResponse<VendorDebitNoteDto> { Success = true, Data = entry });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving debit note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving debit note" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorDebitNoteDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateVendorDebitNoteDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var entry = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = entry.Id }, new ApiResponse<VendorDebitNoteDto> { Success = true, Data = entry, Message = "Debit note created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating debit note"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating debit note" }); }
    }

    [HttpPost("{id:guid}/send")]
    public Task<IActionResult> Send(Guid id) => Run(() => _service.SendAsync(id), "Debit note sent", id);
    [HttpPost("{id:guid}/acknowledge")]
    public Task<IActionResult> Acknowledge(Guid id) => Run(() => _service.AcknowledgeAsync(id), "Debit note acknowledged", id);
    [HttpPost("{id:guid}/settle")]
    public Task<IActionResult> Settle(Guid id) => Run(() => _service.SettleAsync(id), "Debit note settled", id);
    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id) => Run(() => _service.CancelAsync(id), "Debit note cancelled", id);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id); return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Debit note deleted" }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Debit note not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting debit note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting debit note" }); }
    }

    private async Task<IActionResult> Run(Func<Task<VendorDebitNoteDto>> action, string msg, Guid id)
    {
        try { return Ok(new ApiResponse<VendorDebitNoteDto> { Success = true, Data = await action(), Message = msg }); }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Debit note not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error on debit note {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error processing debit note" }); }
    }
}
