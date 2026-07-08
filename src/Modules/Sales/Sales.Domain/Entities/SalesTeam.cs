using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Sales Team — groups sales reps and tracks team performance.
/// Mirrors Odoo's crm.team / sales team concept.
/// TeamLeaderUserId is a cross-module reference to Auth/HR user; never navigate.
/// </summary>
public class SalesTeam : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Email alias for inbound leads (e.g. sales@company.com).</summary>
    public string? Alias { get; set; }

    /// <summary>Cross-module user reference — team leader. Never navigate.</summary>
    public Guid? TeamLeaderUserId { get; set; }

    /// <summary>Snapshot of team leader name for fast reads.</summary>
    public string? TeamLeaderName { get; set; }

    /// <summary>Members (cross-module user IDs). Stored as comma-separated for simplicity.</summary>
    public ICollection<SalesTeamMember> Members { get; set; } = new List<SalesTeamMember>();
}

/// <summary>Member of a SalesTeam — cross-module user reference.</summary>
public class SalesTeamMember : BaseEntity
{
    public Guid SalesTeamId { get; set; }
    public SalesTeam SalesTeam { get; set; } = null!;

    /// <summary>Cross-module reference to Auth user / HR employee.</summary>
    public Guid UserId { get; set; }
    public string? UserName { get; set; }

}
