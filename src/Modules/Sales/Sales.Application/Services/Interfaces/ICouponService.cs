using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Service layer for Coupon business logic.
/// Handles validation rules, usage checks, and coupon lifecycle.
/// </summary>
public interface ICouponService
{
    Task<List<CouponDto>> GetAllAsync();
    Task<CouponDto?> GetByIdAsync(Guid id);
    Task<List<CouponDto>> GetActiveAsync();
    Task<CouponValidationResult> ValidateAsync(string code, Guid? customerId);
    Task<CouponDto> CreateAsync(CreateCouponDto dto);
    Task DeleteAsync(Guid id);
}

public class CouponValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public CouponDto? Coupon { get; set; }
}
