using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Central pricing &amp; promotion preview.
///
/// Both the POS screen and the Sales Order screen call this as items are added/changed to get a
/// live, server-authoritative quote — resolved prices, applied promotions, and coupon — without
/// persisting anything. The same engine runs again when the order is actually created, so what the
/// cashier/salesperson sees here is exactly what the order will be charged.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricing;
    private readonly ILogger<PricingController> _logger;

    public PricingController(IPricingService pricing, ILogger<PricingController> logger)
    {
        _pricing = pricing;
        _logger  = logger;
    }

    /// <summary>Price a basket (no persistence) — returns resolved prices, promotions and totals.</summary>
    [HttpPost("quote")]
    [ProducesResponseType(typeof(ApiResponse<PricedOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Quote([FromBody] PriceOrderRequestDto dto)
    {
        try
        {
            if (dto.Lines is null || dto.Lines.Count == 0)
                return BadRequest(new ApiErrorResponse { Message = "At least one line is required" });

            var result = await _pricing.PriceOrderAsync(dto);
            return Ok(new ApiResponse<PricedOrderDto> { Success = true, Data = result, Message = "Quote calculated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating price quote");
            return StatusCode(500, new ApiErrorResponse { Message = "Error calculating quote" });
        }
    }
}
