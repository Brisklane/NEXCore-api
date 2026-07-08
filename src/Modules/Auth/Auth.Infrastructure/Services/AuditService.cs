using System.Linq.Expressions;
using System.Text.Json;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Persists the auth audit trail (role, permission and assignment changes) and reads it back paged.
/// Every write funnels through <see cref="WriteAsync"/>, and both read paths share one projection,
/// so the entry shape and ordering stay identical across the module.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AuthDbContext _context;

    public AuditService(AuthDbContext context)
    {
        _context = context;
    }

    public Task LogRoleCreatedAsync(Guid roleId, string roleName, Guid userId, string? ipAddress, string? userAgent) =>
        WriteAsync("Role", roleId, "Create", $"Created role: {roleName}", userId, ipAddress, userAgent,
            newValues: JsonSerializer.Serialize(new { RoleId = roleId, RoleName = roleName }));

    public Task LogRoleUpdatedAsync(Guid roleId, string oldValues, string newValues, Guid userId, string? ipAddress, string? userAgent) =>
        WriteAsync("Role", roleId, "Update", "Updated role", userId, ipAddress, userAgent,
            oldValues: oldValues, newValues: newValues);

    public Task LogPermissionAddedToRoleAsync(Guid roleId, Guid permissionId, string permissionCode, Guid userId, string? ipAddress, string? userAgent) =>
        WriteAsync("RolePermission", roleId, "AddPermission", $"Added permission {permissionCode} to role", userId, ipAddress, userAgent,
            newValues: JsonSerializer.Serialize(new { PermissionId = permissionId, PermissionCode = permissionCode }));

    public Task LogPermissionRemovedFromRoleAsync(Guid roleId, Guid permissionId, string permissionCode, Guid userId, string? ipAddress, string? userAgent) =>
        WriteAsync("RolePermission", roleId, "RemovePermission", $"Removed permission {permissionCode} from role", userId, ipAddress, userAgent,
            oldValues: JsonSerializer.Serialize(new { PermissionId = permissionId, PermissionCode = permissionCode }));

    public Task LogRoleAssignedToUserAsync(Guid userId, Guid roleId, string roleName, Guid assignedByUserId, string? ipAddress, string? userAgent) =>
        WriteAsync("UserRole", userId, "AssignRole", $"Assigned role {roleName} to user", assignedByUserId, ipAddress, userAgent,
            newValues: JsonSerializer.Serialize(new { UserId = userId, RoleId = roleId, RoleName = roleName }));

    public Task LogRoleRemovedFromUserAsync(Guid userId, Guid roleId, string roleName, Guid removedByUserId, string? ipAddress, string? userAgent) =>
        WriteAsync("UserRole", userId, "RemoveRole", $"Removed role {roleName} from user", removedByUserId, ipAddress, userAgent,
            oldValues: JsonSerializer.Serialize(new { UserId = userId, RoleId = roleId, RoleName = roleName }));

    public Task<List<AuditLogDto>> GetAuditLogsAsync(Guid entityId, string entityType, int pageNumber = 1, int pageSize = 20) =>
        PagedQuery(a => a.EntityId == entityId && a.EntityType == entityType, pageNumber, pageSize);

    public Task<List<AuditLogDto>> GetUserActivityAsync(Guid userId, int pageNumber = 1, int pageSize = 20) =>
        PagedQuery(a => a.ChangedByUserId == userId, pageNumber, pageSize);

    // ── Internals ────────────────────────────────────────────────────────────

    /// <summary>Builds and saves one audit row. These entries are cross-cutting, so they carry no tenant scope.</summary>
    private async Task WriteAsync(
        string entityType,
        Guid entityId,
        string action,
        string description,
        Guid actorId,
        string? ipAddress,
        string? userAgent,
        string? oldValues = null,
        string? newValues = null)
    {
        var now = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Description = description,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedByUserId = actorId,
            ChangedAt = now,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CompanyId = Guid.Empty,
            BranchId = Guid.Empty,
            CreatedByUserId = actorId,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>Runs a filtered, newest-first page of the audit log through the shared projection.</summary>
    private Task<List<AuditLogDto>> PagedQuery(Expression<Func<AuditLog, bool>> predicate, int pageNumber, int pageSize) =>
        _context.AuditLogs
            .Where(predicate)
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.ChangedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();

    private static readonly Expression<Func<AuditLog, AuditLogDto>> ToDto = a => new AuditLogDto
    {
        Id = a.Id,
        EntityType = a.EntityType,
        EntityId = a.EntityId,
        Action = a.Action,
        Description = a.Description,
        OldValues = a.OldValues,
        NewValues = a.NewValues,
        IpAddress = a.IpAddress,
        ChangedAt = a.ChangedAt,
        ChangedByUserId = a.ChangedByUserId,
        ChangedByUsername = a.ChangedByUsername
    };
}
