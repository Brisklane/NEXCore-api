using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>Join row linking a <see cref="User"/> to a <see cref="Role"/> — a user may hold several roles.</summary>
public class UserRole : UserBaseEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public User? User { get; set; }
    public Role? Role { get; set; }
}
