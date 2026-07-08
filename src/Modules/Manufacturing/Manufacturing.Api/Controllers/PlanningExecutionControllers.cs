using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/planned-orders")]
[Produces("application/json")]
[Authorize]
public class PlannedOrderController : ControllerBase
{
    private readonly IPlannedOrderService _service;
    private readonly ILogger<PlannedOrderController> _logger;

    public PlannedOrderController(IPlannedOrderService service, ILogger<PlannedOrderController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PlannedOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePlannedOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<PlannedOrderDto> { Success = true, Message = "Planned order created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating planned order"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlannedOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "Planned order not found" });
            return Ok(new ApiResponse<PlannedOrderDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving planned order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<PlannedOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<PlannedOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductAsync(productId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PlannedOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlannedOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<PlannedOrderDto> { Success = true, Message = "Planned order updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating planned order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting planned order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/material-issues")]
[Produces("application/json")]
[Authorize]
public class MaterialIssueController : ControllerBase
{
    private readonly IMaterialIssueService _service;
    private readonly ILogger<MaterialIssueController> _logger;

    public MaterialIssueController(IMaterialIssueService service, ILogger<MaterialIssueController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MaterialIssueDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMaterialIssueDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<MaterialIssueDto> { Success = true, Message = "Material issue created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating material issue"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Material issue not found" });
        return Ok(new ApiResponse<MaterialIssueDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<MaterialIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<MaterialIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MaterialIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMaterialIssueDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<MaterialIssueDto> { Success = true, Message = "Material issue updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating material issue {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting material issue {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/wip")]
[Produces("application/json")]
[Authorize]
public class WorkInProgressController : ControllerBase
{
    private readonly IWorkInProgressService _service;
    private readonly ILogger<WorkInProgressController> _logger;

    public WorkInProgressController(IWorkInProgressService service, ILogger<WorkInProgressController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkInProgressDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWorkInProgressDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<WorkInProgressDto> { Success = true, Message = "WIP record created", Data = result });
        }
        catch (InvalidOperationException ex) 
        { 
            _logger.LogWarning(ex, "Duplicate WIP record for ProductionOrder: {ProductionOrderId}", request.ProductionOrderId);
            return Conflict(new ApiErrorResponse { Message = ex.Message, Errors = new List<string> { "DUPLICATE_WIP_RECORD" } }); 
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating WIP record"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkInProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "WIP record not found" });
        return Ok(new ApiResponse<WorkInProgressDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkInProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "WIP record not found for this order" });
        return Ok(new ApiResponse<WorkInProgressDto> { Success = true, Data = result });
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WorkInProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkInProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkInProgressDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<WorkInProgressDto> { Success = true, Message = "WIP updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating WIP {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting WIP {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/subcontract-orders")]
[Produces("application/json")]
[Authorize]
public class SubContractOrderController : ControllerBase
{
    private readonly ISubContractOrderService _service;
    private readonly ILogger<SubContractOrderController> _logger;

    public SubContractOrderController(ISubContractOrderService service, ILogger<SubContractOrderController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SubContractOrderDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSubContractOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<SubContractOrderDto> { Success = true, Message = "Subcontract order created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating subcontract order"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SubContractOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Subcontract order not found" });
        return Ok(new ApiResponse<SubContractOrderDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<SubContractOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SubContractOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SubContractOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubContractOrderDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<SubContractOrderDto> { Success = true, Message = "Subcontract order updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating subcontract order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting subcontract order {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/production-schedules")]
[Produces("application/json")]
[Authorize]
public class ProductionScheduleController : ControllerBase
{
    private readonly IProductionScheduleService _service;
    private readonly ILogger<ProductionScheduleController> _logger;

    public ProductionScheduleController(IProductionScheduleService service, ILogger<ProductionScheduleController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductionScheduleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductionScheduleDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ProductionScheduleDto> { Success = true, Message = "Schedule created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating schedule"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Schedule not found" });
        return Ok(new ApiResponse<ProductionScheduleDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpGet("work-center/{workCenterId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkCenter(Guid workCenterId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByWorkCenterAsync(workCenterId, pagination);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionScheduleDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ProductionScheduleDto> { Success = true, Message = "Schedule updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating schedule {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting schedule {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
