using Microsoft.AspNetCore.Http;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Helpers;

namespace Core.Infrastructure.Services;

/// <summary>
/// Resolves the current tenant from the JWT claim "TenantId" in the HTTP request.
/// Returns null when no valid tenant claim is present (e.g., anonymous registration endpoints).
/// </summary>
public class HttpContextTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId => TenantContextHelper.ExtractTenantId(
        _httpContextAccessor.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());

    public bool HasTenant => TenantId.HasValue;
}
