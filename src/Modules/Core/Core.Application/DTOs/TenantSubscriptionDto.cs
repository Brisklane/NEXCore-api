using Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs;

public class TenantSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public BillingCycle BillingCycle { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public bool IsExpired => DateTime.UtcNow > EndDate;
    public int DaysRemaining => Math.Max(0, (int)(EndDate - DateTime.UtcNow).TotalDays);
}

public class SubscriptionPlanDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePerMonth { get; set; }
    public decimal PricePerYear { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
    public string? AllowedModules { get; set; }
    public int TrialDays { get; set; }
}

public class ChangePlanDto
{
    [Required]
    public Guid SubscriptionPlanId { get; set; }

    [Required]
    public BillingCycle BillingCycle { get; set; }

    public string? Notes { get; set; }
}

public class TenantLicenseInfoDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string PlanCode { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public bool IsExpired { get; set; }
    public int DaysRemaining { get; set; }
    public int MaxCompanies { get; set; }
    public int MaxUsers { get; set; }
    public int MaxBranches { get; set; }
    public string? AllowedModules { get; set; }
}
