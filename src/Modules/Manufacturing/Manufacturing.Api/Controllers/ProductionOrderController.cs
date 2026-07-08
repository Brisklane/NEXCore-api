using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/production-orders")]
[Produces("application/json")]
[Authorize]
public class ProductionOrderController : ControllerBase
{
    private readonly IProductionOrderService _service;
    private readonly ILogger<ProductionOrderController> _logger;

    public ProductionOrderController(IProductionOrderService service, ILogger<ProductionOrderController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductionOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ProductionOrderDto> { Success = true, Message = "Production order created", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating production order"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("produce-express")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> ProduceExpress([FromBody] ProduceExpressDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(User);
            var result = await _service.ProduceExpressAsync(
                request, companyId, branchId, businessUnitId ?? Guid.Empty, userId);
            return StatusCode(201, new ApiResponse<ProductionOrderDto>
            { Success = true, Message = "Produced", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error producing item via express flow"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "Production order not found" });
            return Ok(new ApiResponse<ProductionOrderDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving production order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving production orders"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetByProductAsync(productId, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving production orders for product {Id}", productId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetByStatusAsync(status, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving production orders by status"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ProductionOrderDto> { Success = true, Message = "Production order updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating production order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting production order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? Operations ?????????????????????????????????????????????????????????????

    [HttpPost("operations")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderOperationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddOperation([FromBody] CreateProductionOrderOperationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddOperationAsync(request, userId);
            return StatusCode(201, new ApiResponse<ProductionOrderOperationDto> { Success = true, Message = "Operation added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding operation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}/operations")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionOrderOperationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOperations(Guid id, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetOperationsByOrderAsync(id, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving operations for order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("operations/{operationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderOperationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateOperation(Guid operationId, [FromBody] UpdateProductionOrderOperationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateOperationAsync(operationId, request, userId);
            return Ok(new ApiResponse<ProductionOrderOperationDto> { Success = true, Message = "Operation updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating operation {Id}", operationId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? Components ?????????????????????????????????????????????????????????????

    [HttpPost("components")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderComponentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComponent([FromBody] CreateProductionOrderComponentDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddComponentAsync(request, userId);
            return StatusCode(201, new ApiResponse<ProductionOrderComponentDto> { Success = true, Message = "Component added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding component"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}/components")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionOrderComponentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComponents(Guid id, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetComponentsByOrderAsync(id, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving components for order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("components/{componentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionOrderComponentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateComponent(Guid componentId, [FromBody] UpdateProductionOrderComponentDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateComponentAsync(componentId, request, userId);
            return Ok(new ApiResponse<ProductionOrderComponentDto> { Success = true, Message = "Component updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating component {Id}", componentId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
