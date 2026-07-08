using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Vendor Pricelists — agreed item prices and quantity breaks per vendor, used when creating POs.</summary>
[ApiController]
[Route("api/v1/vendors/{vendorId:guid}/pricelists")]
[Produces("application/json")]
[Authorize]
public class VendorPricelistController : ControllerBase
{
    private readonly IVendorPricelistService _service;
    private readonly ILogger<VendorPricelistController> _logger;

    public VendorPricelistController(IVendorPricelistService service, ILogger<VendorPricelistController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get all pricelists for a vendor.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorPricelistDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByVendor(Guid vendorId)
    {
        try
        {
            var list = await _service.GetByVendorAsync(vendorId);
            return Ok(new ApiResponse<List<VendorPricelistDto>> { Success = true, Data = list });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pricelists"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving pricelists" }); }
    }

    /// <summary>Get a pricelist by ID (with all line items).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPricelistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid vendorId, Guid id)
    {
        try
        {
            var pl = await _service.GetByIdAsync(id);
            if (pl is null) return NotFound(new ApiErrorResponse { Message = "Pricelist not found" });
            return Ok(new ApiResponse<VendorPricelistDto> { Success = true, Data = pl });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving pricelist {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving pricelist" }); }
    }

    /// <summary>Create a new pricelist for a vendor.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VendorPricelistDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid vendorId, [FromBody] CreateVendorPricelistDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var pl = await _service.CreateAsync(vendorId, dto);
            return StatusCode(201, new ApiResponse<VendorPricelistDto> { Success = true, Data = pl, Message = "Pricelist created" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating pricelist"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating pricelist" }); }
    }

    /// <summary>Update a vendor pricelist (name, validity dates, line items).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VendorPricelistDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid vendorId, Guid id, [FromBody] CreateVendorPricelistDto dto)
    {
        try
        {
            var pl = await _service.UpdateAsync(id, dto);
            return Ok(new ApiResponse<VendorPricelistDto> { Success = true, Data = pl, Message = "Pricelist updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Pricelist not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating pricelist {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating pricelist" }); }
    }

    /// <summary>Delete a vendor pricelist.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid vendorId, Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Pricelist deleted" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Pricelist not found" }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting pricelist {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting pricelist" }); }
    }
}
