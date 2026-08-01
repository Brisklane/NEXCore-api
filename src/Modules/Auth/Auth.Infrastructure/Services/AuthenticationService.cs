using System.Security.Cryptography;
using Auth.Application.DTOs;
using Auth.Application.Services.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Auth.Infrastructure.Persistence;
using Nexcore.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Orchestrates the full auth lifecycle: registration, two-step login, refresh-token rotation
/// (with a short reuse grace so a reconnecting till isn't force-logged-out), cross-subdomain
/// handoff, context switching, and the account actions. Company data lives in Core, so tenant
/// validation is requested over the in-process event bus rather than via a direct dependency.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly AuthDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IRoleService _roleService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserValidator _userValidator;
    private readonly IEventPublisher _events;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    /// <summary>
    /// Grace window during which a just-rotated (revoked) refresh token is still accepted.
    /// A reconnecting offline POS — or two tabs sharing storage — can present the same refresh
    /// token twice in quick succession; rotation revokes it on the first call, so without this
    /// window the second call would 401 and force an unnecessary re-login at the till.
    /// </summary>
    private static readonly TimeSpan RefreshReuseGrace = TimeSpan.FromSeconds(60);

    public AuthenticationService(
        AuthDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        IRoleService roleService,
        IPasswordHasher<User> passwordHasher,
        IUserValidator userValidator,
        IEventPublisher events,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _roleService = roleService;
        _passwordHasher = passwordHasher;
        _userValidator = userValidator;
        _events = events;
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    public async Task<ApiResponse> RegisterAsync(CreateUserDto request)
    {
        try
        {
            var validateRequest = new ValidateUserDto
            {
                Email = request.Email,
                Password = request.Password,
                FullName = request.FullName,
                UserName = request.UserName,
                Gender = request.Gender,
                Address = request.Address,
                DateOfBirth = request.DateOfBirth,
                PhoneNumber = request.PhoneNumber
            };

            var validationResult = await _userValidator.ValidateAsync(validateRequest);
            if (!validationResult.Success)
                return validationResult;

            DateTime? dob = string.IsNullOrWhiteSpace(request.DateOfBirth)
                ? null
                : DateTime.Parse(request.DateOfBirth);

            var user = new User
            {
                TenantId = request.TenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                BusinessUnitId = request.BusinessUnitId,
                Username = request.UserName,
                Email = request.Email,
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                Gender = request.Gender,
                DateOfBirth = dob,
                Address = request.Address,
                PasswordHash = _passwordHasher.HashPassword(null!, request.Password),
                IsActive = true,
                IsEmailVerified = false,
                CreatedByUserId = Guid.Parse(AppConstants.CreatedUserId),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create UserCompany record so this user can select this company at login
            var userCompany = new UserCompany
            {
                UserId = user.Id,
                CompanyId = request.CompanyId,
                CompanyName = request.CompanyName,
                CompanySlug = request.CompanySlug,
                TenantId = request.TenantId,
                DefaultBranchId = request.BranchId ?? Guid.Empty,
                DefaultBusinessUnitId = request.BusinessUnitId,
                IsDefault = true,
                IsActive = true,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserCompanies.Add(userCompany);
            await _context.SaveChangesAsync();

            return new ApiResponse
            {
                Success = true,
                Message = "User registered successfully"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Success = false,
                Message = $"User could not be registered due to: {ex.Message}"
            };
        }
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            // Sign-in matches on the normalized column, so the username is case-insensitive
            // exactly as it was under SQL Server's collation.
            var normalizedUsername = User.Normalize(request.Username);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UsernameNormalized == normalizedUsername && u.IsActive);

            if (user == null)
                return Result<LoginResponse>.Fail("Invalid username or password");

            if (user.IsLockedOut && user.LockoutEndAt > DateTime.UtcNow)
                return Result<LoginResponse>.Fail("Account is locked. Please try again later.");

            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.IsLockedOut = true;
                    user.LockoutEndAt = DateTime.UtcNow.AddMinutes(30);
                }
                await _context.SaveChangesAsync();
                return Result<LoginResponse>.Fail("Invalid username or password");
            }

            if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            user.FailedLoginAttempts = 0;
            user.IsLockedOut = false;
            user.LockoutEndAt = null;
            user.LastLoginAt = DateTime.UtcNow;

            var roles = await _roleService.GetUserRolesAsync(user.Id);
            var permissions = await _roleService.GetUserPermissionsAsync(user.Id);

            // Subdomain tenancy: when a company slug is supplied (login at {slug}.{BaseDomain}),
            // scope the session to THAT company — the user must have access to it, and the issued
            // token carries the CompanySlug claim. Without a slug, fall back to the user's default
            // company (legacy behavior), still surfacing its slug so the SPA can land on the right host.
            string accessToken;
            string? companySlug;
            Guid refreshCompanyId = user.CompanyId;
            Guid? refreshBranchId = user.BranchId;
            Guid? refreshBusinessUnitId = user.BusinessUnitId;

            if (!string.IsNullOrWhiteSpace(request.CompanySlug))
            {
                var slug = SubdomainRules.Normalize(request.CompanySlug);
                var userCompany = await _context.UserCompanies
                    .FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.CompanySlug == slug && uc.IsActive);

                if (userCompany == null)
                    return Result<LoginResponse>.Fail("You do not have access to this company workspace.");

                accessToken = _tokenService.GenerateAccessTokenForCompany(user, userCompany, roles, permissions);
                companySlug = userCompany.CompanySlug;
                refreshCompanyId = userCompany.CompanyId;
                refreshBranchId = userCompany.DefaultBranchId;
                refreshBusinessUnitId = userCompany.DefaultBusinessUnitId;
            }
            else
            {
                accessToken = _tokenService.GenerateAccessToken(user, roles, permissions);
                companySlug = (await _context.UserCompanies
                    .FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.CompanyId == user.CompanyId && uc.IsActive))?.CompanySlug;
            }

            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                IsRevoked = false,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                CompanyId = refreshCompanyId,
                BranchId = refreshBranchId,
                BusinessUnitId = refreshBusinessUnitId
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            var userDto = await MapToDtoWithRolesAsync(user);

            return Result<LoginResponse>.Ok(new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _accessTokenExpirationMinutes * 60,
                CompanySlug = companySlug,
                User = userDto
            }, "Login successful");
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Fail($"Error during login: {ex.Message}");
        }
    }

    public async Task<Result<SelectCompanyResponse>> SelectCompanyAsync(SelectCompanyRequest request)
    {
        try
        {
            // Validate the pre-auth token (must not be expired, must have type=pre-auth)
            var principal = _tokenService.GetPrincipalFromValidToken(request.PreAuthToken);
            if (principal == null)
                return Result<SelectCompanyResponse>.Fail("Invalid or expired pre-auth token.");

            var tokenType = principal.FindFirst("token_type")?.Value;
            if (tokenType != "pre-auth")
                return Result<SelectCompanyResponse>.Fail("Invalid token type.");

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Result<SelectCompanyResponse>.Fail("Invalid token claims.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return Result<SelectCompanyResponse>.Fail("User not found.");

            var userCompany = await _context.UserCompanies
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == request.CompanyId && uc.IsActive);

            if (userCompany == null)
                return Result<SelectCompanyResponse>.Fail("You do not have access to the selected company.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var roles = await _roleService.GetUserRolesAsync(user.Id);
            var permissions = await _roleService.GetUserPermissionsAsync(user.Id);
            var accessToken = _tokenService.GenerateAccessTokenForCompany(user, userCompany, roles, permissions);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                IsRevoked = false,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                CompanyId = userCompany.CompanyId,
                BranchId = userCompany.DefaultBranchId,
                BusinessUnitId = userCompany.DefaultBusinessUnitId
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            var userDto = await MapToDtoWithRolesAsync(user);

            return Result<SelectCompanyResponse>.Ok(new SelectCompanyResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _accessTokenExpirationMinutes * 60,
                CompanySlug = userCompany.CompanySlug,
                User = userDto
            }, "Company selected successfully.");
        }
        catch (Exception ex)
        {
            return Result<SelectCompanyResponse>.Fail($"Error selecting company: {ex.Message}");
        }
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Look the token up regardless of revocation state so we can apply the rotation
            // reuse grace below instead of treating a just-rotated token as invalid.
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || refreshToken.ExpiresAt < now)
                return Result<LoginResponse>.Fail("Invalid or expired refresh token");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId && u.IsActive);
            if (user == null)
                return Result<LoginResponse>.Fail("User not found");

            // Rotation reuse grace: a token revoked moments ago was rotated by a concurrent
            // refresh (offline POS reconnect burst, or two tabs sharing localStorage). Rather
            // than 401 — which forces a needless re-login at the till — re-issue an access token
            // and hand back the live successor token instead of minting yet another one.
            if (refreshToken.IsRevoked)
            {
                if (refreshToken.RevokedAt.HasValue && refreshToken.RevokedAt.Value >= now - RefreshReuseGrace)
                {
                    var successor = await _context.RefreshTokens
                        .Where(rt => rt.UserId == refreshToken.UserId
                                  && rt.CompanyId == refreshToken.CompanyId
                                  && !rt.IsRevoked
                                  && rt.ExpiresAt > now)
                        .OrderByDescending(rt => rt.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (successor != null)
                        return await BuildRefreshResponseAsync(user, successor.Token, successor.CompanyId);
                }

                return Result<LoginResponse>.Fail("Invalid or expired refresh token");
            }

            // Normal path — rotate: revoke the presented token and issue a fresh one.
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = now;

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                ExpiresAt = now.AddDays(_refreshTokenExpirationDays),
                IsRevoked = false,
                CreatedByUserId = user.Id,
                CreatedAt = now,
                CompanyId = refreshToken.CompanyId,
                BranchId = refreshToken.BranchId,
                BusinessUnitId = refreshToken.BusinessUnitId
            });

            await _context.SaveChangesAsync();

            return await BuildRefreshResponseAsync(user, newRefreshToken, refreshToken.CompanyId);
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Fail($"Error refreshing token: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds a refreshed-session response: a new access token for <paramref name="user"/>
    /// paired with the given (already-persisted) refresh token value. The access token is scoped
    /// to <paramref name="companyId"/> (the company the refresh token was issued for) so the
    /// CompanySlug claim SURVIVES refresh — critical under subdomain tenancy, where dropping the
    /// slug would fail the Host↔CompanySlug check and silently log the user out. Falls back to the
    /// plain (slug-less) token only for legacy sessions whose company has no UserCompany row.
    /// </summary>
    private async Task<Result<LoginResponse>> BuildRefreshResponseAsync(User user, string refreshTokenValue, Guid companyId)
    {
        var roles = await _roleService.GetUserRolesAsync(user.Id);
        var permissions = await _roleService.GetUserPermissionsAsync(user.Id);

        var userCompany = await _context.UserCompanies
            .FirstOrDefaultAsync(uc => uc.UserId == user.Id && uc.CompanyId == companyId && uc.IsActive);

        var accessToken = userCompany != null
            ? _tokenService.GenerateAccessTokenForCompany(user, userCompany, roles, permissions)
            : _tokenService.GenerateAccessToken(user, roles, permissions);

        var userDto = await MapToDtoWithRolesAsync(user);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = _accessTokenExpirationMinutes * 60,
            CompanySlug = userCompany?.CompanySlug,
            User = userDto
        }, "Token refreshed successfully");
    }

    public async Task<Result<HandoffResponse>> IssueHandoffAsync(Guid userId, Guid companyId)
    {
        try
        {
            var userCompany = await _context.UserCompanies
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId && uc.IsActive);

            if (userCompany == null)
                return Result<HandoffResponse>.Fail("You do not have access to the selected company.");

            // Stateless 60s handoff token; the real tokens are minted only on exchange.
            var code = _tokenService.GenerateHandoffToken(userId, companyId);

            return Result<HandoffResponse>.Ok(new HandoffResponse
            {
                Code = code,
                ExpiresIn = 60,
                CompanySlug = userCompany.CompanySlug
            }, "Handoff issued.");
        }
        catch (Exception ex)
        {
            return Result<HandoffResponse>.Fail($"Error issuing handoff: {ex.Message}");
        }
    }

    public async Task<Result<LoginResponse>> ExchangeHandoffAsync(string code)
    {
        try
        {
            var principal = _tokenService.GetPrincipalFromValidToken(code);
            if (principal == null)
                return Result<LoginResponse>.Fail("Invalid or expired handoff code.");

            if (principal.FindFirst("token_type")?.Value != "handoff")
                return Result<LoginResponse>.Fail("Invalid handoff code.");

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var companyIdClaim = principal.FindFirst("handoff_company")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(companyIdClaim, out var companyId))
                return Result<LoginResponse>.Fail("Invalid handoff code.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return Result<LoginResponse>.Fail("User not found.");

            // Re-validate access at redemption time (the user's company membership may have changed).
            var userCompany = await _context.UserCompanies
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId && uc.IsActive);
            if (userCompany == null)
                return Result<LoginResponse>.Fail("You do not have access to the selected company.");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await BuildCompanySessionAsync(user, userCompany);
        }
        catch (Exception ex)
        {
            return Result<LoginResponse>.Fail($"Error exchanging handoff: {ex.Message}");
        }
    }

    /// <summary>
    /// Mints a full company-scoped session (slug-bearing access token + persisted refresh token)
    /// for the given user/company. Shared by the handoff-exchange flow.
    /// </summary>
    private async Task<Result<LoginResponse>> BuildCompanySessionAsync(User user, UserCompany userCompany)
    {
        var roles = await _roleService.GetUserRolesAsync(user.Id);
        var permissions = await _roleService.GetUserPermissionsAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessTokenForCompany(user, userCompany, roles, permissions);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            IsRevoked = false,
            CreatedByUserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            CompanyId = userCompany.CompanyId,
            BranchId = userCompany.DefaultBranchId,
            BusinessUnitId = userCompany.DefaultBusinessUnitId
        });
        await _context.SaveChangesAsync();

        var userDto = await MapToDtoWithRolesAsync(user);

        return Result<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _accessTokenExpirationMinutes * 60,
            CompanySlug = userCompany.CompanySlug,
            User = userDto
        }, "Session established.");
    }

    public async Task<Result> LogoutAsync(Guid userId, string refreshToken)
    {
        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Result.Ok("Logout successful");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error during logout: {ex.Message}");
        }
    }

    public async Task<Result> VerifyEmailAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Result.Fail("User not found");
            }

            user.IsEmailVerified = true;
            await _context.SaveChangesAsync();

            return Result.Ok("Email verified successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error verifying email: {ex.Message}");
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        try
        {
            if (request.NewPassword != request.ConfirmPassword)
            {
                return Result.Fail("New passwords do not match");
            }

            if (request.NewPassword.Length < AppConstants.MinPasswordLength)
            {
                return Result.Fail($"Password must be at least {AppConstants.MinPasswordLength} characters long");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Result.Fail("User not found");
            }

            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return Result.Fail("Current password is incorrect");
            }

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            return Result.Ok("Password changed successfully");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error changing password: {ex.Message}");
        }
    }

    public async Task<Result<SwitchContextResponse>> SwitchContextAsync(Guid userId, Guid tenantId, SwitchContextRequest request)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            if (user == null)
                return Result<SwitchContextResponse>.Fail("User not found.");

            // Validate the company → branch → business-unit chain against the caller's tenant.
            // The Auth module owns no company data, so it asks Core over the in-process event bus.
            var validation = new ContextValidationRequestedEvent
            {
                TenantId = tenantId,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                BusinessUnitId = request.BusinessUnitId
            };
            await _events.PublishAsync(validation);
            var ctx = await validation.Result.Task;

            if (!ctx.IsValid)
                return Result<SwitchContextResponse>.Fail("The selected company, branch, or business unit is not valid for your tenant.");

            var companySlug = ctx.CompanySlug ?? string.Empty;

            var roles = await _roleService.GetUserRolesAsync(user.Id);
            var permissions = await _roleService.GetUserPermissionsAsync(user.Id);
            var accessToken = _tokenService.GenerateContextSwitchToken(
                user, tenantId, request.CompanyId, companySlug, request.BranchId, request.BusinessUnitId, roles, permissions);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Revoke any prior refresh tokens for this user+company, then issue a fresh one.
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.CompanyId == request.CompanyId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var t in existingTokens)
            {
                t.IsRevoked = true;
                t.RevokedAt = DateTime.UtcNow;
            }

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
                IsRevoked = false,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                CompanyId = request.CompanyId,
                BranchId = request.BranchId,
                BusinessUnitId = request.BusinessUnitId
            });

            await _context.SaveChangesAsync();

            var userDto = await MapToDtoWithRolesAsync(user);
            userDto.CompanyId = request.CompanyId;
            userDto.BranchId = request.BranchId;
            userDto.BusinessUnitId = request.BusinessUnitId;

            return Result<SwitchContextResponse>.Ok(new SwitchContextResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _accessTokenExpirationMinutes * 60,
                CompanySlug = companySlug,
                User = userDto
            }, "Context switched successfully.");
        }
        catch (Exception ex)
        {
            return Result<SwitchContextResponse>.Fail($"Error switching context: {ex.Message}");
        }
    }

    private async Task<UserDto> MapToDtoWithRolesAsync(User user)
    {
        var roles = await _roleService.GetUserRolesAsync(user.Id);
        var permissions = await _roleService.GetUserPermissionsAsync(user.Id);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            CompanyId = user.CompanyId,
            BranchId = user.BranchId,
            BusinessUnitId = user.BusinessUnitId,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            Roles = roles.ToList(),
            Permissions = permissions.ToList()
        };
    }
}