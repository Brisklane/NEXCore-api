using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Enums;

namespace Procurement.Api.Controllers;

/// <summary>Purchase Contracts — long-term vendor agreements (blanket orders, framework agreements, MSAs).</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class PurchaseContractController : ControllerBase
{
    private readonly IPurchaseContractService _service;
    private readonly ILogger<PurchaseContractController> _logger;

    public PurchaseContractController(IPurchaseContractService service, ILogger<PurchaseContractController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all purchase contracts (paged).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await _service.GetAllAsync(pagination));
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contracts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contracts" }); }
    }

    /// <summary>Get contract by ID (with lines).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var contract = await _service.GetByIdAsync(id);
            if (contract is null) return NotFound(new ApiErrorResponse { Message = "Contract not found" });
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contract" }); }
    }

    /// <summary>Get contract by number (e.g. PC-2026-00001).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNumber(string number)
    {
        try
        {
            var contract = await _service.GetByNumberAsync(number);
            if (contract is null) return NotFound(new ApiErrorResponse { Message = "Contract not found" });
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contract"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contract" }); }
    }

    /// <summary>Get contracts by status.</summary>
    [HttpGet("by-status/{status}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseContractDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(PurchaseContractStatus status)
    {
        try
        {
            var list = await _service.GetByStatusAsync(status);
            return Ok(new ApiResponse<List<PurchaseContractDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contracts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contracts" }); }
    }

    /// <summary>Get contracts with a specific vendor.</summary>
    [HttpGet("by-vendor/{vendorId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseContractDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<PurchaseContractDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving contracts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contracts" }); }
    }

    /// <summary>Get all currently active contracts.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseContractDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _service.GetActiveAsync();
            return Ok(new ApiResponse<List<PurchaseContractDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active contracts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contracts" }); }
    }

    /// <summary>Get contracts expiring within N days (renewal alert dashboard).</summary>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponse<List<PurchaseContractDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiring([FromQuery] int withinDays = 30)
    {
        try
        {
            var list = await _service.GetExpiringAsync(withinDays);
            return Ok(new ApiResponse<List<PurchaseContractDto>> { Success = true, Data = list, Message = $"{list.Count} contract(s) expiring within {withinDays} days" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving expiring contracts"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving contracts" }); }
    }

    /// <summary>Update a draft contract (title, end date, value, terms, lines).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePurchaseContractDto dto)
    {
        try
        {
            var contract = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = "Contract updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating contract" }); }
    }

    /// <summary>Create a new purchase contract.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseContractDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var contract = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = contract.Id },
                new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = "Contract created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating contract"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating contract" }); }
    }

    /// <summary>Activate a contract (approve and sign).</summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var contract = await _service.ActivateAsync(id, userId);
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = "Contract activated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error activating contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error activating contract" }); }
    }

    /// <summary>Suspend an active contract temporarily.</summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Suspend(Guid id)
    {
        try
        {
            var contract = await _service.SuspendAsync(id);
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = "Contract suspended" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error suspending contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error suspending contract" }); }
    }

    /// <summary>Terminate a contract early.</summary>
    [HttpPost("{id:guid}/terminate")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Terminate(Guid id, [FromBody] TerminateContractDto dto)
    {
        try
        {
            var contract = await _service.TerminateAsync(id, dto);
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = "Contract terminated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error terminating contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error terminating contract" }); }
    }

    /// <summary>Renew a contract for another term (auto-renewal must be enabled).</summary>
    [HttpPost("{id:guid}/renew")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseContractDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Renew(Guid id)
    {
        try
        {
            var contract = await _service.RenewAsync(id);
            return Ok(new ApiResponse<PurchaseContractDto> { Success = true, Data = contract, Message = $"Contract renewed until {contract.EndDate:yyyy-MM-dd}" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error renewing contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error renewing contract" }); }
    }

    /// <summary>Delete a draft contract.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Contract deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Contract not found" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting contract {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting contract" }); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
    }
}
