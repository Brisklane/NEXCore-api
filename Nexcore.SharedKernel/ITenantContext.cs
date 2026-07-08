namespace Nexcore.SharedKernel;

/// <summary>
/// Ambient tenant for the current request. Resolved from the caller's token/host and
/// consumed by tenant-aware queries and audit stamping so callers never pass the tenant
/// id around by hand.
/// </summary>
public interface ITenantContext
{
    /// <summary>The active tenant, or null when the request is unauthenticated or host-level.</summary>
    Guid? TenantId { get; }

    /// <summary>True when a tenant is in scope.</summary>
    bool HasTenant { get; }
}
