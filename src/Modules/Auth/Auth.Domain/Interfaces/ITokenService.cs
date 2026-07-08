using System.Security.Claims;
using Auth.Domain.Entities;

namespace Auth.Domain.Interfaces;

/// <summary>
/// Issues and validates the JWTs the auth flow relies on — the initial pre-auth token, the
/// company-scoped access token, cross-origin handoff/context-switch tokens, and opaque refresh
/// tokens — plus the helpers for reading a principal back out of a token.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);

    string GenerateAccessTokenForCompany(User user, UserCompany company, IEnumerable<string> roles, IEnumerable<string> permissions);

    string GenerateContextSwitchToken(User user, Guid tenantId, Guid companyId, string companySlug, Guid branchId, Guid businessUnitId, IEnumerable<string> roles, IEnumerable<string> permissions);

    string GeneratePreAuthToken(User user);

    /// <summary>
    /// Short-lived (~60s), single-use token for transferring an authenticated session to another
    /// origin (a different <c>{slug}.{BaseDomain}</c> subdomain). Encodes the user and target company;
    /// redeemed at <c>/auth/handoff/exchange</c>, which re-checks access and mints the real tokens.
    /// </summary>
    string GenerateHandoffToken(Guid userId, Guid companyId);

    string GenerateRefreshToken();

    bool ValidateToken(string token);

    /// <summary>Reads the principal from a token whose signature is valid but which may have expired (used during refresh).</summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    /// <summary>Reads the principal from a fully valid (unexpired, signed) token.</summary>
    ClaimsPrincipal? GetPrincipalFromValidToken(string token);
}
