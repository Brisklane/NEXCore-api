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

    private string _username = null!;
    private string _email = null!;

    /// <summary>
    /// Login handle as the user typed it. Assigning it also refreshes
    /// <see cref="UsernameNormalized"/>, so the two can never disagree.
    /// </summary>
    public required string Username
    {
        get => _username;
        set
        {
            _username = value;
            UsernameNormalized = Normalize(value);
        }
    }

    public required string Email
    {
        get => _email;
        set
        {
            _email = value;
            EmailNormalized = Normalize(value);
        }
    }

    /// <summary>
    /// Lower-cased <see cref="Username"/>; carries the unique index and is what every lookup
    /// compares against.
    /// <para>
    /// SQL Server's default collation compared case-insensitively, so <c>Alice</c> and
    /// <c>alice</c> were one account. PostgreSQL compares case-sensitively, which would both
    /// break sign-in on a case mismatch and let two accounts differ only by case. Deriving the
    /// value in the setter keeps it correct under every provider — including the in-memory one
    /// the unit tests use — while a database CHECK constraint rejects any row where it
    /// disagrees with <see cref="Username"/>, so even a raw-SQL write cannot bypass it.
    /// </para>
    /// </summary>
    public string UsernameNormalized { get; private set; } = null!;

    /// <summary>Lower-cased <see cref="Email"/>. See <see cref="UsernameNormalized"/>.</summary>
    public string EmailNormalized { get; private set; } = null!;

    /// <summary>
    /// Canonicalises a user-supplied username or email for comparison against the
    /// <c>*Normalized</c> columns. Invariant casing deliberately: the current culture would map
    /// a dotted capital I differently under a Turkish locale and disagree with PostgreSQL's
    /// <c>lower()</c>.
    /// </summary>
    public static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

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
