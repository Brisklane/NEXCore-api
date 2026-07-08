using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Application.DTOs;
using Manufacturing.Application.Services.Interfaces;

namespace Manufacturing.Api.Controllers;

[ApiController]
[Route("api/v1/manufacturing/bom")]
[Produces("application/json")]
[Authorize]
public class BillOfMaterialController : ControllerBase
{
    private readonly IBillOfMaterialService _service;
    private readonly ILogger<BillOfMaterialController> _logger;

    public BillOfMaterialController(IBillOfMaterialService service, ILogger<BillOfMaterialController> logger)
    { _service = service; _logger = logger; }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BillOfMaterialDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateBillOfMaterialDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<BillOfMaterialDto> { Success = true, Message = "BOM created", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating BOM"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BillOfMaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new ApiErrorResponse { Message = "BOM not found" });
            return Ok(new ApiResponse<BillOfMaterialDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving BOM {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<BillOfMaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetAllAsync(pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving BOMs"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResponse<BillOfMaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var result = await _service.GetByProductAsync(productId, pagination);
            return Ok(result);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving BOMs for product {Id}", productId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BillOfMaterialDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBillOfMaterialDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<BillOfMaterialDto> { Success = true, Message = "BOM updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating BOM {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting BOM {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? BOM Items ??????????????????????????????????????????????????????????????

    [HttpPost("{bomId:guid}/items")]
    [ProducesResponseType(typeof(ApiResponse<BOMItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddItem(Guid bomId, [FromBody] CreateBOMItemDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddItemAsync(bomId, request, userId);
            return StatusCode(201, new ApiResponse<BOMItemDto> { Success = true, Message = "BOM item added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding BOM item"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{bomId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BOMItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(Guid bomId, Guid itemId, [FromBody] UpdateBOMItemDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateItemAsync(itemId, request, userId);
            return Ok(new ApiResponse<BOMItemDto> { Success = true, Message = "BOM item updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating BOM item {Id}", itemId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{bomId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(Guid bomId, Guid itemId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteItemAsync(itemId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting BOM item {Id}", itemId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // ?? BOM By-Products ????????????????????????????????????????????????????????

    [HttpPost("{bomId:guid}/by-products")]
    [ProducesResponseType(typeof(ApiResponse<BOMByProductDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddByProduct(Guid bomId, [FromBody] CreateBOMByProductDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddByProductAsync(bomId, request, userId);
            return StatusCode(201, new ApiResponse<BOMByProductDto> { Success = true, Message = "By-product added", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding BOM by-product"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{bomId:guid}/by-products/{byProductId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BOMByProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateByProduct(Guid bomId, Guid byProductId, [FromBody] UpdateBOMByProductDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateByProductAsync(byProductId, request, userId);
            return Ok(new ApiResponse<BOMByProductDto> { Success = true, Message = "By-product updated", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating BOM by-product {Id}", byProductId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{bomId:guid}/by-products/{byProductId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteByProduct(Guid bomId, Guid byProductId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteByProductAsync(byProductId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting BOM by-product {Id}", byProductId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
