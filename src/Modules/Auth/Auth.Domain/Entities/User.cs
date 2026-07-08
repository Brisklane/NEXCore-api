using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>
/// An application account: login credentials, profile, and the lockout counters that back the
/// failed-attempt policy. Roles and refresh tokens hang off the navigation collections. Kept in
/// the Auth module (not Core) so identity concerns stay isolated from organizational master data.
/// </summary>
public class User : UserBaseEntity
{
    // ── Identity & contact ───────────────────────────────────────────────────
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Address { get; set; }

    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }

    // ── Credentials & verification ───────────────────────────────────────────
    public required string PasswordHash { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // ── Lockout policy ───────────────────────────────────────────────────────
    // Failed sign-ins increment the counter; once locked, sign-in is blocked until LockoutEndAt.
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public int FailedLoginAttempts { get; set; }

    // ── HR / org profile ─────────────────────────────────────────────────────
    public string? EmployeeId { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }

    /// <summary>Self-reference to this user's manager, forming the org hierarchy.</summary>
    public Guid? ManagerId { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
