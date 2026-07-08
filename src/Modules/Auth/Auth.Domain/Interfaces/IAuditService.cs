namespace Auth.Domain.Interfaces;

/// <summary>
/// Records the auth-sensitive events — role edits, permission grants/revokes, and role
/// assignments — and reads the trail back for a given entity or user. Each write captures the
/// actor plus the request's IP/user-agent for the security log.
/// </summary>
public interface IAuditService
{
    Task LogRoleCreatedAsync(Guid roleId, string roleName, Guid userId, string? ipAddress, string? userAgent);

    Task LogRoleUpdatedAsync(Guid roleId, string oldValues, string newValues, Guid userId, string? ipAddress, string? userAgent);

    Task LogPermissionAddedToRoleAsync(Guid roleId, Guid permissionId, string permissionCode, Guid userId, string? ipAddress, string? userAgent);

    Task LogPermissionRemovedFromRoleAsync(Guid roleId, Guid permissionId, string permissionCode, Guid userId, string? ipAddress, string? userAgent);

    Task LogRoleAssignedToUserAsync(Guid userId, Guid roleId, string roleName, Guid assignedByUserId, string? ipAddress, string? userAgent);

    Task LogRoleRemovedFromUserAsync(Guid userId, Guid roleId, string roleName, Guid removedByUserId, string? ipAddress, string? userAgent);

    Task<List<AuditLogDto>> GetAuditLogsAsync(Guid entityId, string entityType, int pageNumber = 1, int pageSize = 20);

    Task<List<AuditLogDto>> GetUserActivityAsync(Guid userId, int pageNumber = 1, int pageSize = 20);
}

/// <summary>Flattened audit row shaped for API responses.</summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? ChangedByUsername { get; set; }
}
