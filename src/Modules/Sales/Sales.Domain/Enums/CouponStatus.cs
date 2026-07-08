namespace Sales.Domain.Enums;

/// <summary>
/// Status of a coupon / promo code.
/// </summary>
public enum CouponStatus
{
    Active = 0,
    Used = 1,
    Expired = 2,
    Revoked = 3
}
