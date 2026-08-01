using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;

namespace Core.Domain.Entities;

/// <summary>
/// A legal entity owned by a <see cref="Tenant"/>; one tenant can own several (e.g. a holding group).
/// Carries its own audit/soft-delete fields rather than deriving from <see cref="BaseEntity"/>, since
/// a company sits above the company/branch/business-unit scoping that base type assumes.
/// </summary>
public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Owning tenant — the top-level isolation boundary.</summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// URL-safe per-company tenancy identifier (e.g. "acme-corp" → acme-corp.nexcore.app).
    /// Derived from <see cref="CompanyName"/> when not supplied at registration.
    /// </summary>
    public required string Slug { get; set; }

    public string? Code { get; set; }
    public required string CompanyName { get; set; }
    public required string LegalName { get; set; }

    /// <summary>Registration/tax number (VAT, TAN, …).</summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    public string BaseCurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Headquarters address (value object).</summary>
    public Address Address { get; set; } = new Address();

    // ── Contact ──────────────────────────────────────────────────────────────
    public string PhoneNumber { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;

    // ── Location ─────────────────────────────────────────────────────────────

    /// <summary>Geofence radius, in metres, around the HQ coordinates.</summary>
    public int RadiusInMeters { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public byte[]? CompanyLogo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Month (1–12) the financial year begins.</summary>
    public int FiscalYearStartMonth { get; set; } = 1;

    // ── Soft delete ──────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // ── Audit stamps ─────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>PostgreSQL <c>xmin</c> system column, used as the optimistic-concurrency token.</summary>
    public uint RowVersion { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public Tenant? Tenant { get; set; }
    public ICollection<Branch> Branches { get; set; } = [];
    public ICollection<BusinessUnit> BusinessUnits { get; set; } = [];
}
