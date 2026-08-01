namespace Nexcore.SharedKernel;

/// <summary>
/// Root type every persisted domain entity derives from. It bundles the cross-cutting
/// concerns shared by every table — identity, tenant scoping, soft-delete, audit stamps,
/// optimistic concurrency, external-sync state and business numbering — so individual
/// entities only declare their own domain fields.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Surrogate primary key. Assigned client-side so new object graphs can be wired up before they are saved.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Tenant scope ─────────────────────────────────────────────────────────
    // Every row belongs to one company; branch and business unit narrow it further.

    public Guid CompanyId { get; set; }

    public Guid BranchId { get; set; }

    /// <summary>Left unset for company-level records (ledgers, GL accounts, fiscal calendars) that are not tied to a single business unit.</summary>
    public Guid BusinessUnitId { get; set; }

    // ── Common master-data fields ────────────────────────────────────────────

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    // ── Soft delete ──────────────────────────────────────────────────────────
    // Rows are flagged instead of physically deleted, which preserves history and referential integrity.

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // ── Audit stamps ─────────────────────────────────────────────────────────

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    /// <summary>Null until the row is first changed after it was created.</summary>
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // ── Optimistic concurrency ───────────────────────────────────────────────

    /// <summary>
    /// Maps to PostgreSQL's <c>xmin</c> system column — the id of the transaction that last
    /// wrote the row. PostgreSQL maintains it itself on every write, so lost updates are
    /// detected without storing an extra column or running a trigger. Never assign it.
    /// </summary>
    /// <remarks>
    /// Deliberately still named <c>RowVersion</c> rather than <c>Version</c>: several entities
    /// (BillOfMaterial, Routing, StandardCost, CommunicationTemplate) already carry a business
    /// <c>int Version</c>, which a base member of that name would hide.
    /// </remarks>
    public uint RowVersion { get; set; }

    // ── External sync ────────────────────────────────────────────────────────

    /// <summary>True once the row has been pushed to an external system (accounting, e-commerce, offline till, …).</summary>
    public bool IsSynced { get; set; }

    /// <summary>UTC time of the last successful outbound sync, or null if it has never synced.</summary>
    public DateTime? SyncedAt { get; set; }

    // ── Business numbering ───────────────────────────────────────────────────

    /// <summary>
    /// Human-facing reference produced by the document-sequence engine (for example <c>POS-ISB-000001</c>).
    /// Null for child/line records that do not receive their own number.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Sortable numeric companion to <see cref="Code"/> (<c>YYMMDDnnnnn</c>, stored as bigint) for fast range scans.
    /// Null wherever <see cref="Code"/> is null.
    /// </summary>
    public long? CodeInt { get; set; }
}
