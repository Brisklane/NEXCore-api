using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService service, ILogger<OrdersController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try { var (items, total) = await _service.GetPagedAsync(page, pageSize); return Ok(items.ToPaginatedResponse(total, page, pageSize)); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Order not found" });
            return Ok(new ApiResponse<OrderDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-account/{accountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAccount(Guid accountId)
    {
        try { var result = await _service.GetByAccountIdAsync(accountId); return Ok(new ApiResponse<IEnumerable<OrderDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving orders for account {Id}", accountId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<OrderDto> { Success = true, Data = result, Message = "Order created successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating order"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<OrderDto> { Success = true, Data = result, Message = "Order updated successfully" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
