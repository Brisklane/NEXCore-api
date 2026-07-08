namespace Core.Domain.Entities;

/// <summary>
/// An operational unit within a company — a department, division, or cost centre. It belongs to a
/// branch but can span several, and carries the accounting cost/profit-centre codes.
/// </summary>
public class BusinessUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public string? Code { get; set; }
    public required string Name { get; set; }

    /// <summary>Classification such as "Department", "Division", "CostCenter".</summary>
    public string? UnitType { get; set; }

    public string? Description { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Cost-centre code used by accounting.</summary>
    public string? CostCenterCode { get; set; }

    /// <summary>Profit-centre code used for reporting.</summary>
    public string? ProfitCenterCode { get; set; }

    // ── Soft delete ──────────────────────────────────────────────────────────
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    // ── Audit stamps ─────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>SQL Server rowversion for optimistic concurrency.</summary>
    public byte[]? RowVersion { get; set; }
}
