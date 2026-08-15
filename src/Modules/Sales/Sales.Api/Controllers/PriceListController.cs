using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

// ── Price List & Coupons ──────────────────────────────────────────────────────

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PriceListController : ControllerBase
{
    private readonly IPriceListService _priceListService;
    private readonly ILogger<PriceListController> _logger;

    public PriceListController(IPriceListService priceListService, ILogger<PriceListController> logger)
    {
        _priceListService = priceListService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _priceListService.GetAllAsync();
            return Ok(new ApiResponse<List<PriceListDto>> { Success = true, Data = list, Message = "Price lists retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving price lists"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving price lists" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var pl = await _priceListService.GetByIdAsync(id);
            if (pl == null) return NotFound(new ApiErrorResponse { Message = "Price list not found" });
            return Ok(new ApiResponse<PriceListDto> { Success = true, Data = pl, Message = "Price list retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving price list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving price list" }); }
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        try
        {
            var pl = await _priceListService.GetByCodeAsync(code);
            if (pl == null) return NotFound(new ApiErrorResponse { Message = "Price list not found" });
            return Ok(new ApiResponse<PriceListDto> { Success = true, Data = pl, Message = "Price list retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving price list {Code}", code); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving price list" }); }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _priceListService.GetActiveAsync();
            return Ok(new ApiResponse<List<PriceListDto>> { Success = true, Data = list, Message = "Active price lists" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active price lists"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving price lists" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePriceListDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var pl = await _priceListService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = pl.Id },
                new ApiResponse<PriceListDto> { Success = true, Data = pl, Message = "Price list created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating price list"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating price list" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePriceListDto dto)
    {
        try
        {
            var pl = await _priceListService.UpdateAsync(id, dto);
            return Ok(new ApiResponse<PriceListDto> { Success = true, Data = pl, Message = "Price list updated" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating price list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating price list" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _priceListService.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Price list deleted" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting price list {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting price list" }); }
    }

    // ── Lines ─────────────────────────────────────────────────────────────
    // The PriceListItem entity has always existed; without these, a price list could be
    // created but never populated, so nothing it defined ever reached the till.

    /// <summary>Priced lines on a list, ordered by quantity break.</summary>
    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(ApiResponse<List<PriceListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems(Guid id)
    {
        try
        {
            var items = await _priceListService.GetItemsAsync(id);
            return Ok(new ApiResponse<List<PriceListItemDto>>
            {
                Success = true, Data = items, Message = $"{items.Count} line(s)",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading price list lines for {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error loading price list lines" });
        }
    }

    /// <summary>
    /// Add a priced line. Quantity bands for the same product may not overlap — the
    /// pricing engine would otherwise have to choose between them arbitrarily.
    /// </summary>
    [HttpPost("{id}/items")]
    [ProducesResponseType(typeof(ApiResponse<PriceListItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] CreatePriceListItemDto dto)
    {
        try
        {
            var item = await _priceListService.AddItemAsync(id, dto);
            return StatusCode(201, new ApiResponse<PriceListItemDto>
            {
                Success = true, Data = item, Message = "Line added",
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding a line to price list {Id}", id);
            return StatusCode(500, new ApiErrorResponse { Message = "Error adding price list line" });
        }
    }

    [HttpPut("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<PriceListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdatePriceListItemDto dto)
    {
        try
        {
            var item = await _priceListService.UpdateItemAsync(itemId, dto);
            return Ok(new ApiResponse<PriceListItemDto>
            {
                Success = true, Data = item, Message = "Line updated",
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating price list line {ItemId}", itemId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating price list line" });
        }
    }

    [HttpDelete("items/{itemId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        try
        {
            await _priceListService.DeleteItemAsync(itemId);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Line removed" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing price list line {ItemId}", itemId);
            return StatusCode(500, new ApiErrorResponse { Message = "Error removing price list line" });
        }
    }
}
