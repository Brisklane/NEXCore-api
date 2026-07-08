namespace Auth.Application.DTOs;

public class RegisterUserRequest
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Password { get; set; }
    public required string Gender { get; set; }
    public required string DateofBirth { get; set; }
    public required string Address { get; set; }
    public required string ConfirmPassword { get; set; }
}

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }

    /// <summary>
    /// Optional company subdomain slug (<c>{slug}.{BaseDomain}</c>). When set, the session is scoped
    /// to that company (the user must have access) and the token carries the CompanySlug claim; when
    /// null, the user's default company is used.
    /// </summary>
    public string? CompanySlug { get; set; }
}

/// <summary>A company the signed-in user may enter — offered by login before company selection.</summary>
public class CompanyAccessDto
{
    public Guid CompanyId { get; set; }
    public required string CompanyName { get; set; }
    public required string CompanySlug { get; set; }
    public Guid TenantId { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>Login step-1 result: a short-lived pre-auth token plus the companies to choose from.</summary>
public class PreAuthResponse
{
    public required string PreAuthToken { get; set; }
    public int ExpiresIn { get; set; } = 300; // seconds (5 minutes)
    public List<CompanyAccessDto> Companies { get; set; } = [];
}

/// <summary>Login step-2 request: redeem the pre-auth token for the chosen company.</summary>
public class SelectCompanyRequest
{
    public required string PreAuthToken { get; set; }
    public Guid CompanyId { get; set; }
}

/// <summary>Login step-2 result: the full company-scoped session.</summary>
public class SelectCompanyResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public required string CompanySlug { get; set; }
    public required UserDto User { get; set; }
}

/// <summary>Session payload shared by the refresh and single-company login paths.</summary>
public class LoginResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }

    /// <summary>Active company slug when the session is company-scoped, so the SPA can confirm/redirect to the right subdomain.</summary>
    public string? CompanySlug { get; set; }
    public required UserDto User { get; set; }
}

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}

/// <summary>
/// Requests a one-time code that carries the authenticated session to another origin (a different
/// <c>{slug}.{BaseDomain}</c>). Needed because browser storage is per-origin — the destination
/// subdomain has no session of its own. Used after registration and when switching company.
/// </summary>
public class HandoffRequest
{
    /// <summary>Target company to scope the handed-off session to; the caller must have access to it.</summary>
    public Guid CompanyId { get; set; }
}

/// <summary>Handoff result: the short-lived code and the slug to redirect to (<c>https://{slug}.{BaseDomain}/auth/handoff?code=…</c>).</summary>
public class HandoffResponse
{
    public required string Code { get; set; }
    public int ExpiresIn { get; set; }
    public required string CompanySlug { get; set; }
}

public class HandoffExchangeRequest
{
    public required string Code { get; set; }
}

/// <summary>User projection returned to clients, including resolved role and permission codes.</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}

public class UpdateUserRequest
{
    public required string FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Admin invitation: creates a user directly inside the inviting admin's company.</summary>
public class InviteUserRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string FullName { get; set; }
    public required string UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }

    /// <summary>Role to grant immediately after the account is created.</summary>
    public Guid? RoleId { get; set; }
}

/// <summary>Requests a switch of the active company/branch/business-unit scope.</summary>
public class SwitchContextRequest
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid BusinessUnitId { get; set; }
}

/// <summary>New session scoped to the switched-into context.</summary>
public class SwitchContextResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
    public required string CompanySlug { get; set; }
    public required UserDto User { get; set; }
}

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}

public class CreateRoleRequest
{
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}

public class UpdateRoleRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}

public class RoleResponseDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<PermissionDto> Permissions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class PermissionDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
