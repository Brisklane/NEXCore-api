using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/cost-entries")]
[Produces("application/json")]
[Authorize]
public class CostEntryController : ControllerBase
{
    private readonly ICostEntryService _service;
    private readonly ILogger<CostEntryController> _logger;

    public CostEntryController(ICostEntryService service, ILogger<CostEntryController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CostEntryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCostEntryDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<CostEntryDto> { Success = true, Message = "Cost entry created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating cost entry"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CostEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Cost entry not found" });
        return Ok(new ApiResponse<CostEntryDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CostEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Cost entry not found for this order" });
        return Ok(new ApiResponse<CostEntryDto> { Success = true, Data = result });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CostEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCostEntryDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<CostEntryDto> { Success = true, Message = "Cost entry updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating cost entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting cost entry {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<CostEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/production-variances")]
[Produces("application/json")]
[Authorize]
public class ProductionVarianceController : ControllerBase
{
    private readonly IProductionVarianceService _service;
    private readonly ILogger<ProductionVarianceController> _logger;

    public ProductionVarianceController(IProductionVarianceService service, ILogger<ProductionVarianceController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionVarianceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductionVarianceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductionVarianceDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ProductionVarianceDto> { Success = true, Message = "Variance created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating variance"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionVarianceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Variance not found" });
        return Ok(new ApiResponse<ProductionVarianceDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionVarianceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Variance not found for this order" });
        return Ok(new ApiResponse<ProductionVarianceDto> { Success = true, Data = result });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionVarianceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionVarianceDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ProductionVarianceDto> { Success = true, Message = "Variance updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating variance {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting variance {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/machine-downtimes")]
[Produces("application/json")]
[Authorize]
public class MachineDowntimeController : ControllerBase
{
    private readonly IMachineDowntimeService _service;
    private readonly ILogger<MachineDowntimeController> _logger;

    public MachineDowntimeController(IMachineDowntimeService service, ILogger<MachineDowntimeController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MachineDowntimeDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMachineDowntimeDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<MachineDowntimeDto> { Success = true, Message = "Downtime record created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating downtime record"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MachineDowntimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Downtime record not found" });
        return Ok(new ApiResponse<MachineDowntimeDto> { Success = true, Data = result });
    }

    [HttpGet("work-center/{workCenterId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<MachineDowntimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByWorkCenter(Guid workCenterId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByWorkCenterAsync(workCenterId, pagination);
        return Ok(result);
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<MachineDowntimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MachineDowntimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMachineDowntimeDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<MachineDowntimeDto> { Success = true, Message = "Downtime updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating downtime {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting downtime {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<MachineDowntimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }
}
