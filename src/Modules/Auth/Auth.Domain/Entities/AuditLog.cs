using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>
/// Auth-local audit record for changes to roles, permissions and role assignments, with the
/// before/after snapshots and request context needed for a compliance trail.
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>Entity that changed, e.g. "Role", "Permission", "UserRole".</summary>
    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    /// <summary>Verb performed: Create, Update, Delete, Assign, Remove.</summary>
    public required string Action { get; set; }

    /// <summary>Before-state JSON (null for creates).</summary>
    public string? OldValues { get; set; }

    /// <summary>After-state JSON (null for deletes).</summary>
    public string? NewValues { get; set; }

    // ── Request context ──────────────────────────────────────────────────────
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // ── Actor & timing ───────────────────────────────────────────────────────

    /// <summary>UTC time of the audited change — distinct from this row's own <see cref="BaseEntity.CreatedAt"/>.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Guid ChangedByUserId { get; set; }

    /// <summary>Denormalized username so audit views need not join to the user table.</summary>
    public string? ChangedByUsername { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public User? ChangedByUser { get; set; }
}
