namespace Auth.Domain.Entities;

/// <summary>
/// Records which companies a user may sign in to. Company fields (name, slug, tenant) are stored
/// denormalized on purpose: Auth owns users and Core owns companies, so there is no cross-module
/// foreign key — and login can return the company picker without an extra call into Core.
/// </summary>
public class UserCompany
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>Target company. Deliberately not an FK — this crosses the Auth/Core module boundary.</summary>
    public Guid CompanyId { get; set; }

    /// <summary>Denormalized company name, so the login company list needs no round-trip to Core.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Tenancy URL slug for this company, e.g. "acme-corp".</summary>
    public string CompanySlug { get; set; } = string.Empty;

    /// <summary>Denormalized tenant id, needed to mint the scoped JWT once this company is chosen.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Branch this user lands in by default within this company.</summary>
    public Guid DefaultBranchId { get; set; }

    /// <summary>Optional default business-unit context.</summary>
    public Guid? DefaultBusinessUnitId { get; set; }

    /// <summary>Marks the user's primary company — pre-selected at login (and implicit when there is only one).</summary>
    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Audit stamps ─────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public User User { get; set; } = null!;
}
