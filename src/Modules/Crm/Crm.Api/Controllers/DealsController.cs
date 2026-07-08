using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

/// <summary>Deals (Opportunities) - sales opportunities tracked through pipeline stages.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class DealsController : ControllerBase
{
    private readonly IDealService _service;
    private readonly ILogger<DealsController> _logger;

    public DealsController(IDealService service, ILogger<DealsController> logger)
    { _service = service; _logger = logger; }

    // Deal CRUD

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] string? stage = null)
    {
        try { var result = await _service.GetPagedAsync(pagination, stage); return Ok(result); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deals"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DealDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Deal not found" });
            return Ok(new ApiResponse<DealDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deal {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-account/{accountId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DealDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAccount(Guid accountId)
    {
        try { var result = await _service.GetByAccountIdAsync(accountId); return Ok(new ApiResponse<IEnumerable<DealDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deals for account {Id}", accountId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DealDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDealDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<DealDto> { Success = true, Data = result, Message = "Deal created successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating deal"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DealDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDealDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<DealDto> { Success = true, Data = result, Message = "Deal updated successfully" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating deal {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting deal {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // Deal Products

    [HttpGet("{id:guid}/products")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DealProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(Guid id)
    {
        try { var result = await _service.GetProductsAsync(id); return Ok(new ApiResponse<IEnumerable<DealProductDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deal products {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/products")]
    [ProducesResponseType(typeof(ApiResponse<DealProductDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddProduct(Guid id, [FromBody] CreateDealProductDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddProductAsync(id, dto, userId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DealProductDto> { Success = true, Data = result, Message = "Product added to deal" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding product to deal {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}/products/{dealProductId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DealProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProduct(Guid id, Guid dealProductId, [FromBody] UpdateDealProductDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateProductAsync(id, dealProductId, dto, userId);
            return Ok(new ApiResponse<DealProductDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating deal product"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/products/{dealProductId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveProduct(Guid id, Guid dealProductId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.RemoveProductAsync(id, dealProductId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing deal product"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    // Deal Contacts (Roles)

    [HttpGet("{id:guid}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DealContactDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContacts(Guid id)
    {
        try { var result = await _service.GetContactsAsync(id); return Ok(new ApiResponse<IEnumerable<DealContactDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving deal contacts {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/contacts")]
    [ProducesResponseType(typeof(ApiResponse<DealContactDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] CreateDealContactDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddContactAsync(id, dto, userId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DealContactDto> { Success = true, Data = result, Message = "Contact role added to deal" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding contact to deal {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/contacts/{dealContactId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveContact(Guid id, Guid dealContactId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.RemoveContactAsync(id, dealContactId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing deal contact"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
