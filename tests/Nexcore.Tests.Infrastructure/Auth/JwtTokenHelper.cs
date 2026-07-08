using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Nexcore.Tests.Infrastructure.Auth;

public static class JwtTokenHelper
{
    public static string GenerateToken(
        string[] permissions,
        string userId          = "00000000-0000-0000-0000-000000000001",
        string tenantId        = "00000000-0000-0000-0000-000000000001",
        string companyId       = "00000000-0000-0000-0000-000000000001",
        string branchId        = "00000000-0000-0000-0000-000000000001",
        string businessUnitId  = "00000000-0000-0000-0000-000000000001")
    {
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TestJwtSettings.Secret));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, "Integration Test User"),
            new("TenantId",       tenantId),
            new("CompanyId",      companyId),
            new("BranchId",       branchId),
            new("BusinessUnitId", businessUnitId),
        };

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var token = new JwtSecurityToken(
            issuer: TestJwtSettings.Issuer,
            audience: TestJwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
