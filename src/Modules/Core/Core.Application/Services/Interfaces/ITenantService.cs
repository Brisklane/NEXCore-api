using Core.Application.DTOs;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;

namespace Core.Application.Services.Interfaces;

/// <summary>Tenant administration and the licensing/subscription operations (plan lookup, change, renew).</summary>
public interface ITenantService
{
    // Tenant CRUD
    Task<Result<TenantDto>> GetTenantByIdAsync(Guid tenantId);
    Task<Result<IEnumerable<TenantDto>>> GetAllTenantsAsync();
    Task<Result<TenantDto>> UpdateTenantAsync(Guid tenantId, UpdateTenantDto request, Guid userId);
    Task<Result> DeactivateTenantAsync(Guid tenantId, Guid userId);

    // Subscription / Licensing
    Task<Result<IEnumerable<SubscriptionPlanDto>>> GetSubscriptionPlansAsync();
    Task<Result<TenantLicenseInfoDto>> GetLicenseInfoAsync(Guid tenantId);
    Task<Result<TenantSubscriptionDto>> ChangePlanAsync(Guid tenantId, ChangePlanDto request, Guid userId);
    Task<Result<TenantSubscriptionDto>> RenewSubscriptionAsync(Guid tenantId, Guid userId);
}
