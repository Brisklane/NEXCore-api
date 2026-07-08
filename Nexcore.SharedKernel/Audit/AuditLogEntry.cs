namespace Nexcore.SharedKernel.Audit;

/// <summary>
/// One row in the cross-module audit trail: what changed, who changed it, from where, and the
/// before/after snapshot. Written for compliance and security review, never mutated afterwards.
/// </summary>
public class AuditLogEntry : BaseEntity
{
    /// <summary>Owning module, e.g. "Auth", "Core", "Accounting".</summary>
    public required string ModuleName { get; set; }

    /// <summary>Entity that was touched, e.g. "Role", "Company", "Employee".</summary>
    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    /// <summary>Verb performed — Create, Update, Delete, Activate, Assign, …</summary>
    public required string Action { get; set; }

    /// <summary>JSON snapshot of the record before the change (null for creates).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the record after the change (null for deletes).</summary>
    public string? NewValues { get; set; }

    // ── Request context ──────────────────────────────────────────────────────
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? HttpMethod { get; set; }
    public string? Endpoint { get; set; }
    public int? HttpStatusCode { get; set; }

    // ── Actor & timing ───────────────────────────────────────────────────────

    /// <summary>UTC time the change happened — the audited event, distinct from this row's own <see cref="BaseEntity.CreatedAt"/>.</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public Guid ChangedByUserId { get; set; }

    /// <summary>Denormalized username so audit screens need not join back to the user table.</summary>
    public string? ChangedByUsername { get; set; }

    /// <summary>Ties together the entries produced by one logical operation.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Free-form JSON for extra context beyond the old/new snapshots.</summary>
    public string? Metadata { get; set; }

    /// <summary>Coarse grouping for filtering — Security, Financial, Personnel, System, …</summary>
    public string? Category { get; set; }

    public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
}

/// <summary>Risk banding for an audit entry, used for filtering and alerting.</summary>
public enum AuditSeverity
{
    /// <summary>Routine, expected activity.</summary>
    Info = 0,

    /// <summary>Notable but not dangerous — most edits.</summary>
    Warning = 1,

    /// <summary>High-risk: deletions, permission/security changes, credential resets.</summary>
    Critical = 2
}
