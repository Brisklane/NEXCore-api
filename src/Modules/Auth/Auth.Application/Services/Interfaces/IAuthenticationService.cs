using Auth.Application.DTOs;
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Api;

namespace Auth.Application.Services.Interfaces;

/// <summary>
/// The authentication flow end to end: registration, the two-step login (credentials → company
/// selection), token refresh, cross-subdomain session handoff, context switching, and the account
/// actions (logout, email verification, password change).
/// </summary>
public interface IAuthenticationService
{
    Task<ApiResponse> RegisterAsync(CreateUserDto request);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);

    /// <summary>Login step 2: trade a pre-auth token plus the chosen company for a fully scoped JWT.</summary>
    Task<Result<SelectCompanyResponse>> SelectCompanyAsync(SelectCompanyRequest request);

    Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    /// <summary>
    /// Issues a short-lived, single-use handoff code that carries the caller's session to another
    /// origin (a different <c>{slug}.{BaseDomain}</c> subdomain). Requires access to the target
    /// company; redeemed on the destination via <see cref="ExchangeHandoffAsync"/>.
    /// </summary>
    Task<Result<HandoffResponse>> IssueHandoffAsync(Guid userId, Guid companyId);

    /// <summary>Redeems a handoff code (anonymously) for a company-scoped session, re-checking access first.</summary>
    Task<Result<LoginResponse>> ExchangeHandoffAsync(string code);

    Task<Result> LogoutAsync(Guid userId, string refreshToken);

    Task<Result> VerifyEmailAsync(Guid userId);

    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>
    /// Switches the active company/branch/business-unit context. The tenant is read from the caller's
    /// current JWT (never client-supplied) and the requested chain is validated against it before a
    /// new scoped token is minted.
    /// </summary>
    Task<Result<SwitchContextResponse>> SwitchContextAsync(Guid userId, Guid tenantId, SwitchContextRequest request);
}
