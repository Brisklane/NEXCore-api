using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/rework-orders")]
[Produces("application/json")]
[Authorize]
public class ReworkOrderController : ControllerBase
{
    private readonly IReworkOrderService _service;
    private readonly ILogger<ReworkOrderController> _logger;

    public ReworkOrderController(IReworkOrderService service, ILogger<ReworkOrderController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReworkOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateReworkOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ReworkOrderDto> { Success = true, Message = "Rework order created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating rework order"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReworkOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Rework order not found" });
        return Ok(new ApiResponse<ReworkOrderDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ReworkOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ReworkOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReworkOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReworkOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ReworkOrderDto> { Success = true, Message = "Rework order updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating rework order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting rework order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/material-planning")]
[Produces("application/json")]
[Authorize]
public class MaterialPlanningDataController : ControllerBase
{
    private readonly IMaterialPlanningDataService _service;
    private readonly ILogger<MaterialPlanningDataController> _logger;

    public MaterialPlanningDataController(IMaterialPlanningDataService service, ILogger<MaterialPlanningDataController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MaterialPlanningDataDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMaterialPlanningDataDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<MaterialPlanningDataDto> { Success = true, Message = "MRP data created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating MRP data"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialPlanningDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "MRP data not found" });
        return Ok(new ApiResponse<MaterialPlanningDataDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<MaterialPlanningDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialPlanningDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var result = await _service.GetByProductAsync(productId);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "MRP data not found for this product" });
        return Ok(new ApiResponse<MaterialPlanningDataDto> { Success = true, Data = result });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialPlanningDataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMaterialPlanningDataDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<MaterialPlanningDataDto> { Success = true, Message = "MRP data updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating MRP data {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting MRP data {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/standard-costs")]
[Produces("application/json")]
[Authorize]
public class StandardCostController : ControllerBase
{
    private readonly IStandardCostService _service;
    private readonly ILogger<StandardCostController> _logger;

    public StandardCostController(IStandardCostService service, ILogger<StandardCostController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StandardCostDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateStandardCostDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<StandardCostDto> { Success = true, Message = "Standard cost created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating standard cost"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StandardCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Standard cost not found" });
        return Ok(new ApiResponse<StandardCostDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<StandardCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<StandardCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductAsync(productId, pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}/active")]
    [ProducesResponseType(typeof(ApiResponse<StandardCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveByProduct(Guid productId)
    {
        var result = await _service.GetActiveByProductAsync(productId);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "No active standard cost for this product" });
        return Ok(new ApiResponse<StandardCostDto> { Success = true, Data = result });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StandardCostDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStandardCostDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<StandardCostDto> { Success = true, Message = "Standard cost updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating standard cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting standard cost {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/overhead-rules")]
[Produces("application/json")]
[Authorize]
public class OverheadRuleController : ControllerBase
{
    private readonly IOverheadRuleService _service;
    private readonly ILogger<OverheadRuleController> _logger;

    public OverheadRuleController(IOverheadRuleService service, ILogger<OverheadRuleController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OverheadRuleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateOverheadRuleDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<OverheadRuleDto> { Success = true, Message = "Overhead rule created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating overhead rule"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OverheadRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Overhead rule not found" });
        return Ok(new ApiResponse<OverheadRuleDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<OverheadRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("work-center/{workCenterId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<OverheadRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkCenter(Guid workCenterId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByWorkCenterAsync(workCenterId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OverheadRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOverheadRuleDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<OverheadRuleDto> { Success = true, Message = "Overhead rule updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating overhead rule {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting overhead rule {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/capacity-loads")]
[Produces("application/json")]
[Authorize]
public class CapacityLoadController : ControllerBase
{
    private readonly ICapacityLoadService _service;
    private readonly ILogger<CapacityLoadController> _logger;

    public CapacityLoadController(ICapacityLoadService service, ILogger<CapacityLoadController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CapacityLoadDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCapacityLoadDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<CapacityLoadDto> { Success = true, Message = "Capacity load created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating capacity load"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CapacityLoadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Capacity load not found" });
        return Ok(new ApiResponse<CapacityLoadDto> { Success = true, Data = result });
    }

    [HttpGet("work-center/{workCenterId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<CapacityLoadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkCenter(Guid workCenterId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByWorkCenterAsync(workCenterId, pagination);
        return Ok(result);
    }

    [HttpGet("work-center/{workCenterId:guid}/date-range")]
    [ProducesResponseType(typeof(PaginatedResponse<CapacityLoadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByDateRange(Guid workCenterId, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByDateRangeAsync(workCenterId, from, to, pagination);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<CapacityLoadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CapacityLoadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCapacityLoadDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<CapacityLoadDto> { Success = true, Message = "Capacity load updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating capacity load {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting capacity load {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/demands")]
[Produces("application/json")]
[Authorize]
public class DemandController : ControllerBase
{
    private readonly IDemandService _service;
    private readonly ILogger<DemandController> _logger;

    public DemandController(IDemandService service, ILogger<DemandController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DemandDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDemandDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<DemandDto> { Success = true, Message = "Demand created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating demand"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DemandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Demand not found" });
        return Ok(new ApiResponse<DemandDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DemandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("open")]
    [ProducesResponseType(typeof(PaginatedResponse<DemandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpen([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetOpenAsync(pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<DemandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductAsync(productId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DemandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDemandDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<DemandDto> { Success = true, Message = "Demand updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating demand {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting demand {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/inventory-transactions")]
[Produces("application/json")]
[Authorize]
public class InventoryTransactionController : ControllerBase
{
    private readonly IInventoryTransactionService _service;
    private readonly ILogger<InventoryTransactionController> _logger;

    public InventoryTransactionController(IInventoryTransactionService service, ILogger<InventoryTransactionController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInventoryTransactionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<InventoryTransactionDto> { Success = true, Message = "Transaction created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating inventory transaction"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Transaction not found" });
        return Ok(new ApiResponse<InventoryTransactionDto> { Success = true, Data = result });
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductAsync(productId, pagination);
        return Ok(result);
    }

    [HttpGet("reference/{referenceId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByReference(Guid referenceId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByReferenceAsync(referenceId, pagination);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInventoryTransactionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InventoryTransactionDto> { Success = true, Message = "Transaction updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating transaction {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting transaction {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
