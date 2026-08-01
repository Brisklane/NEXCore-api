namespace Nexcore.SharedKernel;

/// <summary>
/// Base type for identity/account entities in the Auth module. It carries the same
/// audit, soft-delete, concurrency and numbering fields as <see cref="BaseEntity"/>,
/// but scopes rows by <see cref="TenantId"/> + company rather than branch/business unit,
/// because accounts live at the tenant level and only optionally belong to a branch.
/// </summary>
public abstract class UserBaseEntity
{
    /// <summary>Surrogate primary key, assigned client-side.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Tenant scope ─────────────────────────────────────────────────────────

    /// <summary>Outermost isolation boundary an account belongs to.</summary>
    public Guid TenantId { get; set; }

    public Guid CompanyId { get; set; }

    /// <summary>Null for company-wide accounts that are not pinned to a branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Null for company-level accounts.</summary>
    public Guid? BusinessUnitId { get; set; }

    // ── Common master-data fields ────────────────────────────────────────────

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    // ── Soft delete ──────────────────────────────────────────────────────────

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // ── Audit stamps ─────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    /// <summary>Null until the account is first changed after creation.</summary>
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // ── Optimistic concurrency ───────────────────────────────────────────────

    /// <summary>
    /// Maps to PostgreSQL's <c>xmin</c> system column, maintained by the database on every
    /// write. See <see cref="BaseEntity.RowVersion"/>. Never assign it.
    /// </summary>
    public uint RowVersion { get; set; }

    // ── External sync ────────────────────────────────────────────────────────

    public bool IsSynced { get; set; }
    public DateTime? SyncedAt { get; set; }

    // ── Business numbering ───────────────────────────────────────────────────

    public string? Code { get; set; }
    public long? CodeInt { get; set; }
}
