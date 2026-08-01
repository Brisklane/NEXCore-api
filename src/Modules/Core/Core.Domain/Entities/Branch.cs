using Nexcore.SharedKernel.ValueObjects;

namespace Core.Domain.Entities;

/// <summary>
/// A physical or operational location within a <see cref="Company"/>. Branches group business units
/// and anchor transactions to a place; they carry their own audit/soft-delete fields.
/// </summary>
public class Branch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }

    /// <summary>Short code unique within the company (e.g. "NYC", "LA").</summary>
    public string? Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Branch address (value object).</summary>
    public Address Address { get; set; } = new Address();

    // ── Contact ──────────────────────────────────────────────────────────────
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }

    /// <summary>Classification such as "Headquarters", "Regional", "Sales", "Distribution".</summary>
    public string? BranchType { get; set; }

    public byte[]? BranchLogo { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Location ─────────────────────────────────────────────────────────────
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

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
    public ICollection<BusinessUnit> BusinessUnits { get; set; } = [];
}
