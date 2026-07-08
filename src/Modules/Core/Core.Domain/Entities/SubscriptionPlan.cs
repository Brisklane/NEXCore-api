namespace Core.Domain.Entities;

/// <summary>
/// A subscription tier. Seeded as static reference data (not tenant-editable). In the numeric caps
/// below, <c>-1</c> means unlimited.
/// </summary>
public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Internal code used in logic, e.g. "TRIAL", "STARTER", "PRO", "ENTERPRISE".</summary>
    public required string Code { get; set; }

    /// <summary>Display name shown in the UI.</summary>
    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal PricePerMonth { get; set; }
    public decimal PricePerYear { get; set; }

    public int MaxCompanies { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }

    /// <summary>Comma-separated allowed module codes (e.g. "Core,HR,Accounting"); null means all modules.</summary>
    public string? AllowedModules { get; set; }

    /// <summary>Trial length in days — only meaningful for the trial plan.</summary>
    public int TrialDays { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Audit stamps ─────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = [];
}
