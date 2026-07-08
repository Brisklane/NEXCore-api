using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class RiderController : ControllerBase
{
    private readonly IRiderService _riderService;
    private readonly ILogger<RiderController> _logger;

    public RiderController(IRiderService riderService, ILogger<RiderController> logger)
    {
        _riderService = riderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _riderService.GetAllAsync();
            return Ok(new ApiResponse<List<RiderDto>> { Success = true, Data = list, Message = "Riders retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving riders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving riders" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var rider = await _riderService.GetByIdAsync(id);
            if (rider == null) return NotFound(new ApiErrorResponse { Message = "Rider not found" });
            return Ok(new ApiResponse<RiderDto> { Success = true, Data = rider, Message = "Rider retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving rider {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving rider" }); }
    }

    [HttpGet("by-store/{storeId}")]
    public async Task<IActionResult> GetByStore(Guid storeId)
    {
        try
        {
            var list = await _riderService.GetByBranchAsync(storeId);
            return Ok(new ApiResponse<List<RiderDto>> { Success = true, Data = list, Message = "Store riders" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving riders for store"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving riders" }); }
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable([FromQuery] Guid? storeId)
    {
        try
        {
            var list = await _riderService.GetAvailableAsync(storeId);
            return Ok(new ApiResponse<List<RiderDto>> { Success = true, Data = list, Message = "Available riders" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving available riders"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving riders" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRiderDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var rider = await _riderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = rider.Id },
                new ApiResponse<RiderDto> { Success = true, Data = rider, Message = "Rider created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating rider"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating rider" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRiderDto dto)
    {
        try
        {
            var rider = await _riderService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<RiderDto> { Success = true, Data = rider, Message = "Rider updated" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating rider {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating rider" }); }
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] CreateRiderAssignmentDto dto)
    {
        try
        {
            var assignment = await _riderService.AssignAsync(dto);
            return StatusCode(201, new ApiResponse<RiderAssignmentDto> { Success = true, Data = assignment, Message = "Rider assigned" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error assigning rider"); return StatusCode(500, new ApiErrorResponse { Message = "Error assigning rider" }); }
    }

    /// <summary>Get the current active assignment for an order - used for order tracking.</summary>
    [HttpGet("assignment/active/{orderId}")]
    public async Task<IActionResult> GetActiveAssignment(Guid orderId)
    {
        try
        {
            var assignment = await _riderService.GetActiveAssignmentAsync(orderId);
            if (assignment == null) return NotFound(new ApiErrorResponse { Message = "No active assignment found for this order" });
            return Ok(new ApiResponse<RiderAssignmentDto> { Success = true, Data = assignment, Message = "Active assignment retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active assignment"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving assignment" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _riderService.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Rider deleted" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting rider {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting rider" }); }
    }
}
