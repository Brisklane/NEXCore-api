using Nexcore.SharedKernel;

namespace Auth.Domain.Entities;

/// <summary>
/// A persisted refresh token backing JWT re-issuance. Tokens are never deleted on logout — they are
/// marked revoked with a timestamp, and the originating IP/agent are kept for security review.
/// </summary>
public class RefreshToken : UserBaseEntity
{
    public Guid UserId { get; set; }

    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }

    // ── Revocation ───────────────────────────────────────────────────────────
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }

    // ── Issue context (security audit) ───────────────────────────────────────
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public User? User { get; set; }
}
