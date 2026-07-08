using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>
/// A named bundle of <see cref="Permission"/>s that can be granted to users. Roles are company-scoped
/// and ranked by <see cref="HierarchyLevel"/> so higher tiers can manage lower ones.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>Stable identifier such as "ADMIN", "USER", "VIEWER". Shadows the base <c>Code</c> to make it required.</summary>
    public new required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>Rank in the access hierarchy: 0 = System, 1 = Super Admin, 2 = Company Admin, 3 = Branch Admin, …</summary>
    public int HierarchyLevel { get; set; } = 3;

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<Permission> Permissions { get; set; } = [];
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
