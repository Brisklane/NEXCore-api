using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Domain.Entities;
using Auth.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Infrastructure.Services;

/// <summary>
/// HMAC-SHA256 JWT issuer/validator. All five token variants funnel through <see cref="BuildToken"/>
/// and share the claim builders, and all three validation paths share one set of validation
/// parameters — so signing, issuer/audience and claim shape stay consistent by construction.
/// </summary>
public class TokenService : ITokenService
{
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;

    public TokenService(IConfiguration configuration)
    {
        _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        _jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured");
        _jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured");
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = BaseIdentityClaims(user);
        claims.Add(new Claim("TenantId", user.TenantId.ToString()));
        claims.Add(new Claim("CompanyId", user.CompanyId.ToString()));
        claims.Add(new Claim("BranchId", user.BranchId.ToString() ?? string.Empty));
        if (user.BusinessUnitId.HasValue)
            claims.Add(new Claim("BusinessUnitId", user.BusinessUnitId.Value.ToString()));
        AddRolesAndPermissions(claims, roles, permissions);

        return BuildToken(claims, TimeSpan.FromMinutes(_accessTokenExpirationMinutes));
    }

    public string GenerateAccessTokenForCompany(User user, UserCompany company, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = BaseIdentityClaims(user);
        claims.Add(new Claim("TenantId", company.TenantId.ToString()));
        claims.Add(new Claim("CompanyId", company.CompanyId.ToString()));
        claims.Add(new Claim("CompanySlug", company.CompanySlug));
        claims.Add(new Claim("BranchId", company.DefaultBranchId.ToString()));
        if (company.DefaultBusinessUnitId.HasValue)
            claims.Add(new Claim("BusinessUnitId", company.DefaultBusinessUnitId.Value.ToString()));
        AddRolesAndPermissions(claims, roles, permissions);

        return BuildToken(claims, TimeSpan.FromMinutes(_accessTokenExpirationMinutes));
    }

    public string GenerateContextSwitchToken(User user, Guid tenantId, Guid companyId, string companySlug, Guid branchId, Guid businessUnitId, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = BaseIdentityClaims(user);
        claims.Add(new Claim("TenantId", tenantId.ToString()));
        claims.Add(new Claim("CompanyId", companyId.ToString()));
        claims.Add(new Claim("CompanySlug", companySlug));
        claims.Add(new Claim("BranchId", branchId.ToString()));
        claims.Add(new Claim("BusinessUnitId", businessUnitId.ToString()));
        AddRolesAndPermissions(claims, roles, permissions);

        return BuildToken(claims, TimeSpan.FromMinutes(_accessTokenExpirationMinutes));
    }

    public string GeneratePreAuthToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("token_type", "pre-auth"),
        };

        return BuildToken(claims, TimeSpan.FromMinutes(5));
    }

    public string GenerateHandoffToken(Guid userId, Guid companyId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("token_type", "handoff"),
            new("handoff_company", companyId.ToString()),
        };

        return BuildToken(claims, TimeSpan.FromSeconds(60));
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public bool ValidateToken(string token) => ReadPrincipal(token, validateLifetime: true, requireJwt: true) != null;

    public ClaimsPrincipal? GetPrincipalFromValidToken(string token) => ReadPrincipal(token, validateLifetime: true, requireJwt: true);

    /// <summary>Reads the principal without enforcing expiry — used during refresh to identify an expired token's owner.</summary>
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) => ReadPrincipal(token, validateLifetime: false, requireJwt: false);

    // ── Internals ────────────────────────────────────────────────────────────

    private static List<Claim> BaseIdentityClaims(User user) => new()
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Email, user.Email),
        new("FullName", user.FullName),
    };

    private static void AddRolesAndPermissions(List<Claim> claims, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));
    }

    private string BuildToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var key = Encoding.UTF8.GetBytes(_jwtSecret);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(lifetime),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private TokenValidationParameters BuildValidationParameters(bool validateLifetime) => new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = _jwtIssuer,
        ValidateAudience = true,
        ValidAudience = _jwtAudience,
        ValidateLifetime = validateLifetime,
        ClockSkew = TimeSpan.Zero
    };

    private ClaimsPrincipal? ReadPrincipal(string token, bool validateLifetime, bool requireJwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, BuildValidationParameters(validateLifetime), out SecurityToken validatedToken);
            return requireJwt && validatedToken is not JwtSecurityToken ? null : principal;
        }
        catch
        {
            return null;
        }
    }
}
