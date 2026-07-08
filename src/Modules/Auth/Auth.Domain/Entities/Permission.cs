using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>
/// A single, system-wide access grant that roles collect. Shared across every company — a role
/// decides which permissions apply where.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Grant code following the <c>MODULE_ACTION</c> convention, e.g. "CREATE_COMPANY". Shadows the base <c>Code</c> to make it required.</summary>
    public new required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Owning area used to group permissions in the UI, e.g. "Company", "User", "Accounting".</summary>
    public string? Module { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<Role> Roles { get; set; } = [];
}
