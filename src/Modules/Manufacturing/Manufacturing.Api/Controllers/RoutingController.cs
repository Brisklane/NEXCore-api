using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/routings")]
[Produces("application/json")]
[Authorize]
public class RoutingController : ControllerBase
{
    private readonly IRoutingService _service;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(IRoutingService service, ILogger<RoutingController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoutingDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRoutingDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<RoutingDto> { Success = true, Message = "Routing created", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating routing"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoutingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "Routing not found" });
            return Ok(new ApiResponse<RoutingDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving routing {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<RoutingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving routings"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<RoutingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetByProductAsync(productId, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving routings for product {Id}", productId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoutingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoutingDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<RoutingDto> { Success = true, Message = "Routing updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating routing {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting routing {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? Operations ?????????????????????????????????????????????????????????????

    [HttpPost("{routingId:guid}/operations")]
    [ProducesResponseType(typeof(ApiResponse<RoutingOperationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOperation(Guid routingId, [FromBody] CreateRoutingOperationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddOperationAsync(routingId, request, userId);
            return StatusCode(201, new ApiResponse<RoutingOperationDto> { Success = true, Message = "Operation added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding routing operation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{routingId:guid}/operations/{operationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoutingOperationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOperation(Guid routingId, Guid operationId, [FromBody] UpdateRoutingOperationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateOperationAsync(operationId, request, userId);
            return Ok(new ApiResponse<RoutingOperationDto> { Success = true, Message = "Operation updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating operation {Id}", operationId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{routingId:guid}/operations/{operationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteOperation(Guid routingId, Guid operationId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteOperationAsync(operationId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting operation {Id}", operationId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
