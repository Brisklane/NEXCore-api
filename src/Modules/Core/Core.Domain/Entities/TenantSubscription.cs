using Core.Domain.Enums;

namespace Core.Domain.Entities;

/// <summary>
/// Tracks a tenant's subscription to a plan, including licensing and billing cycle.
/// A tenant may have only one Active subscription at a time but retains history.
/// </summary>
public class TenantSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public Guid SubscriptionPlanId { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>
    /// When the trial expires (only set for Trial status)
    /// </summary>
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Unique license key issued to the tenant for this subscription period
    /// </summary>
    public required string LicenseKey { get; set; }

    /// <summary>
    /// Notes added by platform admins (e.g., manual overrides, special terms)
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[]? RowVersion { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
}
