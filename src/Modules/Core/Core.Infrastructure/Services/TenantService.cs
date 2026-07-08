using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Helpers;

namespace Core.Infrastructure.Services;

/// <summary>
/// Tenant lifecycle plus licensing. Provisions a tenant with a trial subscription during company
/// registration, and drives the plan change/renewal flow (each mutation opens a new subscription
/// row and retires the old one, keeping full history). Slugs are made unique across tenants+companies.
/// </summary>
public class TenantService : ITenantService
{
    private readonly CoreDbContext _context;
    private readonly ILogger<TenantService> _logger;

    // Seeded plan IDs (must match CoreDbContext seed data)
    private static readonly Guid TrialPlanId = new("00000000-0000-0000-0000-000000000001");

    public TenantService(CoreDbContext context, ILogger<TenantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ── Internal: called by CompanyService during registration ─────────────────

    /// <summary>
    /// Creates a new Tenant and assigns it a Trial subscription.
    /// Called inside the company-creation transaction.
    /// </summary>
    public async Task<Tenant> CreateTenantInternalAsync(string name, string? email, Guid createdByUserId, string? requestedSlug = null)
    {
        // Use caller-supplied slug if provided (already validated in CompanyValidator),
        // otherwise auto-generate a DNS-safe, non-reserved, unique slug from the name.
        var slug = await EnsureUniqueSlugAsync(requestedSlug, name);

        var tenant = new Tenant
        {
            Name = name,
            Slug = slug,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        // Assign Trial subscription automatically
        var trial = new TenantSubscription
        {
            TenantId = tenant.Id,
            SubscriptionPlanId = TrialPlanId,
            Status = SubscriptionStatus.Trial,
            BillingCycle = BillingCycle.Monthly,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(15),
            TrialEndDate = DateTime.UtcNow.AddDays(15),
            LicenseKey = GenerateLicenseKey(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _context.TenantSubscriptions.Add(trial);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} ({Slug}) created with Trial subscription", tenant.Id, tenant.Slug);

        return tenant;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public async Task<Result<TenantDto>> GetTenantByIdAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (tenant == null)
            return Result<TenantDto>.Fail("Tenant not found");

        return Result<TenantDto>.Ok(MapToDto(tenant));
    }

    public async Task<Result<IEnumerable<TenantDto>>> GetAllTenantsAsync()
    {
        var tenants = await _context.Tenants
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Plan)
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return Result<IEnumerable<TenantDto>>.Ok(tenants.Select(MapToDto));
    }

    public async Task<Result<TenantDto>> UpdateTenantAsync(Guid tenantId, UpdateTenantDto request, Guid userId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);
        if (tenant == null)
            return Result<TenantDto>.Fail("Tenant not found");

        tenant.Name = request.Name;
        tenant.Email = request.Email;
        tenant.PhoneNumber = request.PhoneNumber;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        return await GetTenantByIdAsync(tenantId);
    }

    public async Task<Result> DeactivateTenantAsync(Guid tenantId, Guid userId)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);
        if (tenant == null)
            return Result.Fail("Tenant not found");

        tenant.IsActive = false;
        tenant.IsDeleted = true;
        tenant.DeletedAt = DateTime.UtcNow;
        tenant.DeletedByUserId = userId;
        tenant.UpdatedAt = DateTime.UtcNow;
        tenant.UpdatedByUserId = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} deactivated by {UserId}", tenantId, userId);
        return Result.Ok("Tenant deactivated successfully");
    }

    // ── Subscription / Licensing ───────────────────────────────────────────────

    public async Task<Result<IEnumerable<SubscriptionPlanDto>>> GetSubscriptionPlansAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PricePerMonth)
            .ToListAsync();

        return Result<IEnumerable<SubscriptionPlanDto>>.Ok(plans.Select(MapToPlanDto));
    }

    public async Task<Result<TenantLicenseInfoDto>> GetLicenseInfoAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (tenant == null)
            return Result<TenantLicenseInfoDto>.Fail("Tenant not found");

        var active = GetActiveSubscription(tenant);
        if (active == null)
            return Result<TenantLicenseInfoDto>.Fail("No active subscription found");

        var dto = new TenantLicenseInfoDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            PlanCode = active.Plan.Code,
            PlanName = active.Plan.Name,
            Status = active.Status,
            LicenseKey = active.LicenseKey,
            EndDate = active.EndDate,
            IsExpired = DateTime.UtcNow > active.EndDate,
            DaysRemaining = Math.Max(0, (int)(active.EndDate - DateTime.UtcNow).TotalDays),
            MaxCompanies = active.Plan.MaxCompanies,
            MaxUsers = active.Plan.MaxUsers,
            MaxBranches = active.Plan.MaxBranches,
            AllowedModules = active.Plan.AllowedModules
        };

        return Result<TenantLicenseInfoDto>.Ok(dto);
    }

    public async Task<Result<TenantSubscriptionDto>> ChangePlanAsync(Guid tenantId, ChangePlanDto request, Guid userId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (tenant == null)
            return Result<TenantSubscriptionDto>.Fail("Tenant not found");

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == request.SubscriptionPlanId && p.IsActive);

        if (plan == null)
            return Result<TenantSubscriptionDto>.Fail("Subscription plan not found or inactive");

        if (plan.Code == "TRIAL")
            return Result<TenantSubscriptionDto>.Fail("Cannot switch to the Trial plan. Trial is only available on first registration.");

        // Cancel current active subscription
        var current = GetActiveSubscription(tenant);
        if (current != null)
        {
            current.Status = SubscriptionStatus.Cancelled;
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedByUserId = userId;
        }

        // Create new subscription
        var endDate = request.BillingCycle == BillingCycle.Annual
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);

        var newSub = new TenantSubscription
        {
            TenantId = tenantId,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            BillingCycle = request.BillingCycle,
            StartDate = DateTime.UtcNow,
            EndDate = endDate,
            LicenseKey = GenerateLicenseKey(),
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        _context.TenantSubscriptions.Add(newSub);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantId} plan changed to {PlanCode} by {UserId}", tenantId, plan.Code, userId);

        // Reload with plan for mapping
        await _context.Entry(newSub).Reference(s => s.Plan).LoadAsync();

        return Result<TenantSubscriptionDto>.Ok(MapToSubscriptionDto(newSub));
    }

    public async Task<Result<TenantSubscriptionDto>> RenewSubscriptionAsync(Guid tenantId, Guid userId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Subscriptions)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);

        if (tenant == null)
            return Result<TenantSubscriptionDto>.Fail("Tenant not found");

        var current = GetActiveSubscription(tenant)
            ?? tenant.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();

        if (current == null)
            return Result<TenantSubscriptionDto>.Fail("No subscription found to renew");

        if (current.Plan.Code == "TRIAL")
            return Result<TenantSubscriptionDto>.Fail("Trial subscriptions cannot be renewed. Please upgrade to a paid plan.");

        // Extend from current end date (or now if already expired)
        var renewFrom = current.EndDate > DateTime.UtcNow ? current.EndDate : DateTime.UtcNow;
        var newEnd = current.BillingCycle == BillingCycle.Annual
            ? renewFrom.AddYears(1)
            : renewFrom.AddMonths(1);

        var renewal = new TenantSubscription
        {
            TenantId = tenantId,
            SubscriptionPlanId = current.SubscriptionPlanId,
            Status = SubscriptionStatus.Active,
            BillingCycle = current.BillingCycle,
            StartDate = renewFrom,
            EndDate = newEnd,
            LicenseKey = GenerateLicenseKey(),
            Notes = "Renewal",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        // Mark old as expired if not already
        if (current.Status != SubscriptionStatus.Cancelled)
        {
            current.Status = SubscriptionStatus.Expired;
            current.UpdatedAt = DateTime.UtcNow;
            current.UpdatedByUserId = userId;
        }

        _context.TenantSubscriptions.Add(renewal);
        await _context.SaveChangesAsync();

        await _context.Entry(renewal).Reference(s => s.Plan).LoadAsync();

        _logger.LogInformation("Tenant {TenantId} subscription renewed until {EndDate}", tenantId, newEnd);

        return Result<TenantSubscriptionDto>.Ok(MapToSubscriptionDto(renewal));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TenantSubscription? GetActiveSubscription(Tenant tenant) =>
        tenant.Subscriptions
            .Where(s => s.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

    private static string GenerateLicenseKey()
    {
        // Format: XXXX-XXXX-XXXX-XXXX (uppercase alphanumeric)
        var raw = Guid.NewGuid().ToString("N").ToUpper();
        return $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}-{raw[12..16]}";
    }

    /// <summary>
    /// Resolves the tenant/company slug. Caller-supplied slugs are already validated
    /// (format + reserved + uniqueness) by CompanyValidator and are honored as-is. When none is
    /// supplied, derives a DNS-safe slug from the company name, avoids reserved subdomains, and
    /// appends a numeric suffix until it is unique across both Companies and Tenants.
    /// </summary>
    private async Task<string> EnsureUniqueSlugAsync(string? requestedSlug, string name)
    {
        if (!string.IsNullOrWhiteSpace(requestedSlug))
            return SubdomainRules.Normalize(requestedSlug);

        var baseSlug = SubdomainRules.Slugify(name);

        // Guarantee a usable, non-reserved base (very short / empty / reserved names get a prefix).
        if (baseSlug.Length < SubdomainRules.MinLength)
            baseSlug = $"co-{baseSlug}".Trim('-');
        if (baseSlug.Length < SubdomainRules.MinLength)
            baseSlug = $"co-{Guid.NewGuid():N}"[..12];
        if (SubdomainRules.IsReserved(baseSlug))
            baseSlug = $"co-{baseSlug}";
        if (baseSlug.Length > SubdomainRules.MaxLength)
            baseSlug = baseSlug[..SubdomainRules.MaxLength].Trim('-');

        var candidate = baseSlug;
        var suffix = 1;
        while (await _context.Companies.AnyAsync(c => c.Slug == candidate && !c.IsDeleted)
               || await _context.Tenants.AnyAsync(t => t.Slug == candidate))
        {
            suffix++;
            var tail = $"-{suffix}";
            var head = baseSlug.Length + tail.Length > SubdomainRules.MaxLength
                ? baseSlug[..(SubdomainRules.MaxLength - tail.Length)].Trim('-')
                : baseSlug;
            candidate = $"{head}{tail}";
        }

        return candidate;
    }

    private static TenantDto MapToDto(Tenant tenant)
    {
        var active = tenant.Subscriptions
            .Where(s => s.Status is SubscriptionStatus.Active or SubscriptionStatus.Trial)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            Email = tenant.Email,
            PhoneNumber = tenant.PhoneNumber,
            IsActive = tenant.IsActive,
            CreatedAt = tenant.CreatedAt,
            ActiveSubscription = active != null ? MapToSubscriptionDto(active) : null
        };
    }

    private static TenantSubscriptionDto MapToSubscriptionDto(TenantSubscription s) => new()
    {
        Id = s.Id,
        TenantId = s.TenantId,
        SubscriptionPlanId = s.SubscriptionPlanId,
        PlanName = s.Plan?.Name ?? string.Empty,
        PlanCode = s.Plan?.Code ?? string.Empty,
        Status = s.Status,
        BillingCycle = s.BillingCycle,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        TrialEndDate = s.TrialEndDate,
        LicenseKey = s.LicenseKey
    };

    private static SubscriptionPlanDto MapToPlanDto(SubscriptionPlan p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Description = p.Description,
        PricePerMonth = p.PricePerMonth,
        PricePerYear = p.PricePerYear,
        MaxCompanies = p.MaxCompanies,
        MaxUsers = p.MaxUsers,
        MaxBranches = p.MaxBranches,
        AllowedModules = p.AllowedModules,
        TrialDays = p.TrialDays
    };
}
