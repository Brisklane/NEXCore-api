using Microsoft.Extensions.Logging;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Coupon service - handles validation rules, usage limits, and coupon lifecycle.
/// </summary>
public class CouponService : ICouponService
{
    private readonly ICouponRepository _coupons;
    private readonly ILogger<CouponService> _logger;

    public CouponService(ICouponRepository coupons, ILogger<CouponService> logger)
    {
        _coupons = coupons;
        _logger = logger;
    }

    public async Task<List<CouponDto>> GetAllAsync()
    {
        var list = await _coupons.GetAllAsync();
        return list.OrderByDescending(c => c.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<CouponDto?> GetByIdAsync(Guid id)
    {
        var coupon = await _coupons.GetByIdAsync(id);
        return coupon is null ? null : MapToDto(coupon);
    }

    public async Task<List<CouponDto>> GetActiveAsync()
    {
        var list = await _coupons.GetActiveAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<CouponValidationResult> ValidateAsync(string code, Guid? customerId)
    {
        var coupon = await _coupons.GetByCodePublicAsync(code);
        if (coupon is null || coupon.Status != CouponStatus.Active)
            return Fail("Coupon not found or inactive");

        var now = DateTime.UtcNow;
        if (coupon.ValidFrom > now)
            return Fail("Coupon is not yet valid");

        if (coupon.ValidTo.HasValue && coupon.ValidTo < now)
            return Fail("Coupon has expired");

        if (coupon.MaxUsageCount.HasValue && coupon.UsageCount >= coupon.MaxUsageCount.Value)
            return Fail("Coupon usage limit reached");

        if (customerId.HasValue)
        {
            var alreadyUsed = await _coupons.IsCodeUsedByContactAsync(code, customerId.Value);
            if (alreadyUsed)
                return Fail("Coupon already used by this customer");
        }

        return new CouponValidationResult { IsValid = true, Coupon = MapToDto(coupon) };
    }

    public async Task<CouponDto> CreateAsync(CreateCouponDto dto)
    {
        var existing = await _coupons.GetByCodeAsync(dto.Code);
        if (existing is not null)
            throw new InvalidOperationException("Coupon code already exists");

        var coupon = new Coupon
        {
            Code = dto.Code.ToUpper(),
            Description = dto.Description,
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            MaxUsageCount = dto.UsageLimit,
            MaxUsagePerCustomer = dto.PerCustomerLimit ?? 1,
            ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
            ValidTo = dto.ValidTo,
            Status = CouponStatus.Active,
        };

        await _coupons.AddAsync(coupon);
        await _coupons.SaveChangesAsync();

        _logger.LogInformation("Coupon created: {Code} (ID: {Id})", coupon.Code, coupon.Id);
        return MapToDto(coupon);
    }

    public async Task DeleteAsync(Guid id)
    {
        var coupon = await _coupons.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Coupon not found");

        _coupons.Delete(coupon);
        await _coupons.SaveChangesAsync();

        _logger.LogInformation("Coupon deleted: {Id}", id);
    }

    // Helpers

    private static CouponValidationResult Fail(string message)
        => new() { IsValid = false, ErrorMessage = message };

    internal static CouponDto MapToDto(Coupon c) => new()
    {
        Id = c.Id, Code = c.Code, Description = c.Description, Status = c.Status,
        DiscountType = c.DiscountType, DiscountValue = c.DiscountValue,
        MinOrderAmount = c.MinOrderAmount, MaxDiscountAmount = c.MaxDiscountAmount,
        UsageLimit = c.MaxUsageCount, UsedCount = c.UsageCount,
        ValidFrom = c.ValidFrom, ValidTo = c.ValidTo, IsActive = c.Status == CouponStatus.Active,
    };
}
