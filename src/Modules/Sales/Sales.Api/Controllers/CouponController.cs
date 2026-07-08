using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class CouponController : ControllerBase
{
    private readonly ICouponService _couponService;
    private readonly ILogger<CouponController> _logger;

    public CouponController(ICouponService couponService, ILogger<CouponController> logger)
    {
        _couponService = couponService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _couponService.GetAllAsync();
            return Ok(new ApiResponse<List<CouponDto>> { Success = true, Data = list, Message = "Coupons retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving coupons"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving coupons" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var coupon = await _couponService.GetByIdAsync(id);
            if (coupon == null) return NotFound(new ApiErrorResponse { Message = "Coupon not found" });
            return Ok(new ApiResponse<CouponDto> { Success = true, Data = coupon, Message = "Coupon retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving coupon {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving coupon" }); }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _couponService.GetActiveAsync();
            return Ok(new ApiResponse<List<CouponDto>> { Success = true, Data = list, Message = "Active coupons" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active coupons"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving coupons" }); }
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Validate(string code, [FromQuery] Guid? customerId)
    {
        try
        {
            var result = await _couponService.ValidateAsync(code, customerId);
            if (!result.IsValid)
                return BadRequest(new ApiErrorResponse { Message = result.ErrorMessage ?? "Coupon is invalid" });
            return Ok(new ApiResponse<CouponDto> { Success = true, Data = result.Coupon, Message = "Coupon is valid" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error validating coupon {Code}", code); return StatusCode(500, new ApiErrorResponse { Message = "Error validating coupon" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCouponDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var coupon = await _couponService.CreateAsync(dto);
            return StatusCode(201, new ApiResponse<CouponDto> { Success = true, Data = coupon, Message = "Coupon created" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating coupon"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating coupon" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _couponService.DeleteAsync(id);
            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Coupon deleted" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting coupon {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting coupon" }); }
    }
}
