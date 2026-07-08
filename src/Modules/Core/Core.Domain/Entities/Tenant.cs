namespace Core.Domain.Entities;

/// <summary>
/// The top-level isolation boundary — one tenant is one SaaS customer (the subscription holder).
/// A tenant may own several <see cref="Company"/> entities, e.g. a holding group.
/// </summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name, usually the organization's name.</summary>
    public required string Name { get; set; }

    /// <summary>URL-safe unique identifier (e.g. "acme-corp") used for routing.</summary>
    public required string Slug { get; set; }

    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Audit / soft delete ──────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public byte[]? RowVersion { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<Company> Companies { get; set; } = [];
    public ICollection<TenantSubscription> Subscriptions { get; set; } = [];
}
