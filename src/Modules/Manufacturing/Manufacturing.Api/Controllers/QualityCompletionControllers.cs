using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/inspections")]
[Produces("application/json")]
[Authorize]
public class InspectionController : ControllerBase
{
    private readonly IInspectionService _service;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(IInspectionService service, ILogger<InspectionController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InspectionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateInspectionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<InspectionDto> { Success = true, Message = "Inspection created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating inspection"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InspectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Inspection not found" });
        return Ok(new ApiResponse<InspectionDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<InspectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InspectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInspectionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InspectionDto> { Success = true, Message = "Inspection updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating inspection {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting inspection {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<InspectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }

    // ?? Characteristics ????????????????????????????????????????????????????????

    [HttpPost("{inspectionId:guid}/characteristics")]
    [ProducesResponseType(typeof(ApiResponse<InspectionCharacteristicDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddCharacteristic(Guid inspectionId, [FromBody] CreateInspectionCharacteristicDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddCharacteristicAsync(inspectionId, request, userId);
            return StatusCode(201, new ApiResponse<InspectionCharacteristicDto> { Success = true, Message = "Characteristic added", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error adding characteristic"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{inspectionId:guid}/characteristics/{characteristicId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InspectionCharacteristicDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCharacteristic(Guid inspectionId, Guid characteristicId, [FromBody] UpdateInspectionCharacteristicDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateCharacteristicAsync(characteristicId, request, userId);
            return Ok(new ApiResponse<InspectionCharacteristicDto> { Success = true, Message = "Characteristic updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating characteristic {Id}", characteristicId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{inspectionId:guid}/characteristics/{characteristicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCharacteristic(Guid inspectionId, Guid characteristicId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteCharacteristicAsync(characteristicId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting characteristic {Id}", characteristicId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/finished-goods-receipts")]
[Produces("application/json")]
[Authorize]
public class FinishedGoodsReceiptController : ControllerBase
{
    private readonly IFinishedGoodsReceiptService _service;
    private readonly ILogger<FinishedGoodsReceiptController> _logger;

    public FinishedGoodsReceiptController(IFinishedGoodsReceiptService service, ILogger<FinishedGoodsReceiptController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FinishedGoodsReceiptDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateFinishedGoodsReceiptDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<FinishedGoodsReceiptDto> { Success = true, Message = "Receipt created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating receipt"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FinishedGoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Receipt not found" });
        return Ok(new ApiResponse<FinishedGoodsReceiptDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<FinishedGoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FinishedGoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFinishedGoodsReceiptDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<FinishedGoodsReceiptDto> { Success = true, Message = "Receipt updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting receipt {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<FinishedGoodsReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }
}

// ?????????????????????????????????????????????????????????????????????????????

[ApiController]
[Route("api/v1/manufacturing/production-batches")]
[Produces("application/json")]
[Authorize]
public class ProductionBatchController : ControllerBase
{
    private readonly IProductionBatchService _service;
    private readonly ILogger<ProductionBatchController> _logger;

    public ProductionBatchController(IProductionBatchService service, ILogger<ProductionBatchController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductionBatchDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProductionBatchDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ProductionBatchDto> { Success = true, Message = "Batch created", Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating batch"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Batch not found" });
        return Ok(new ApiResponse<ProductionBatchDto> { Success = true, Data = result });
    }

    [HttpGet("number/{batchNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBatchNumber(string batchNumber)
    {
        var result = await _service.GetByBatchNumberAsync(batchNumber);
        if (result == null) return NotFound(new ApiErrorResponse { Message = "Batch not found" });
        return Ok(new ApiResponse<ProductionBatchDto> { Success = true, Data = result });
    }

    [HttpGet("order/{productionOrderId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByOrder(Guid productionOrderId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductionOrderAsync(productionOrderId, pagination);
        return Ok(result);
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetByProductAsync(productId, pagination);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductionBatchDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ProductionBatchDto> { Success = true, Message = "Batch updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating batch {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting batch {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ProductionBatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        var result = await _service.GetAllAsync(pagination);
        return Ok(result);
    }
}
