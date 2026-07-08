using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/work-centers")]
[Produces("application/json")]
[Authorize]
public class WorkCenterController : ControllerBase
{
    private readonly IWorkCenterService _service;
    private readonly ILogger<WorkCenterController> _logger;

    public WorkCenterController(IWorkCenterService service, ILogger<WorkCenterController> logger)
    { _service = service; _logger = logger; }

    // ?? Work Center CRUD ???????????????????????????????????????????????????????

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWorkCenterDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<WorkCenterDto> { Success = true, Message = "Work center created", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating work center"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "Work center not found" });
            return Ok(new ApiResponse<WorkCenterDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving work center {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WorkCenterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving work centers"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkCenterDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<WorkCenterDto> { Success = true, Message = "Work center updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating work center {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting work center {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? Shifts ????????????????????????????????????????????????????????????????

    [HttpPost("{workCenterId:guid}/shifts")]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterShiftDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddShift(Guid workCenterId, [FromBody] CreateWorkCenterShiftDto request)
    {
        try
        {
            request.WorkCenterId = workCenterId;
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddShiftAsync(request, userId);
            return CreatedAtAction(nameof(GetShiftById), new { workCenterId, shiftId = result.Id },
                new ApiResponse<WorkCenterShiftDto> { Success = true, Message = "Shift added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding shift"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{workCenterId:guid}/shifts/{shiftId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShiftById(Guid workCenterId, Guid shiftId)
    {
        try
        {
            var result = await _service.GetShiftByIdAsync(shiftId);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "Shift not found" });
            return Ok(new ApiResponse<WorkCenterShiftDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving shift {Id}", shiftId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{workCenterId:guid}/shifts")]
    [ProducesResponseType(typeof(PaginatedResponse<WorkCenterShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetShifts(Guid workCenterId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetShiftsByWorkCenterAsync(workCenterId, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving shifts"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{workCenterId:guid}/shifts/{shiftId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkCenterShiftDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateShift(Guid workCenterId, Guid shiftId, [FromBody] UpdateWorkCenterShiftDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateShiftAsync(shiftId, request, userId);
            return Ok(new ApiResponse<WorkCenterShiftDto> { Success = true, Message = "Shift updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating shift {Id}", shiftId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{workCenterId:guid}/shifts/{shiftId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteShift(Guid workCenterId, Guid shiftId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteShiftAsync(shiftId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting shift {Id}", shiftId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
