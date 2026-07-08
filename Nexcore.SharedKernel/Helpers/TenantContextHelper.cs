using System.Security.Claims;

namespace Nexcore.SharedKernel.Helpers;

/// <summary>
/// Helper class for extracting tenant information from JWT claims
/// Provides cross-module access to tenant context extraction
/// </summary>
public static class TenantContextHelper
{
    /// <summary>
    /// Extract tenant context (CompanyId, BranchId, BusinessUnitId) from claims principal
    /// </summary>
    /// <param name="principal">ClaimsPrincipal from HttpContext.User</param>
    /// <returns>Tuple of (CompanyId, BranchId, BusinessUnitId)</returns>
    /// <exception cref="InvalidOperationException">Thrown when CompanyId or BranchId claims are invalid or missing</exception>
    public static (Guid CompanyId, Guid BranchId, Guid? BusinessUnitId) ExtractTenantContext(ClaimsPrincipal principal)
    {
        var companyIdClaim = principal.FindFirst("CompanyId")?.Value;
        var branchIdClaim = principal.FindFirst("BranchId")?.Value;
        var businessUnitIdClaim = principal.FindFirst("BusinessUnitId")?.Value;

        if (!Guid.TryParse(companyIdClaim, out var companyId))
            throw new InvalidOperationException("Invalid or missing CompanyId claim");

        if (!Guid.TryParse(branchIdClaim, out var branchId))
            throw new InvalidOperationException("Invalid or missing BranchId claim");

        Guid? businessUnitId = null;
        if (!string.IsNullOrEmpty(businessUnitIdClaim) && Guid.TryParse(businessUnitIdClaim, out var parsedBusinessUnitId))
        {
            businessUnitId = parsedBusinessUnitId;
        }

        return (companyId, branchId, businessUnitId);
    }

    /// <summary>
    /// Extract tenant ID from claims principal
    /// </summary>
    public static Guid? ExtractTenantId(ClaimsPrincipal principal)
    {
        var tenantIdClaim = principal.FindFirst("TenantId")?.Value;
        if (Guid.TryParse(tenantIdClaim, out var tenantId))
            return tenantId;
        return null;
    }

    /// <summary>
    /// Extract user ID from claims principal
    /// </summary>
    /// <param name="principal">ClaimsPrincipal from HttpContext.User</param>
    /// <returns>User ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when user ID claim is invalid or missing</exception>
    public static Guid ExtractUserId(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!Guid.TryParse(userIdClaim, out var userId))
            throw new InvalidOperationException("Invalid or missing user ID claim");

        return userId;
    }
}
