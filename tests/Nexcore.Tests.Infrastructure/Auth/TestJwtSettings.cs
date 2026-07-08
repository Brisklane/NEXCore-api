namespace Nexcore.Tests.Infrastructure.Auth;

public static class TestJwtSettings
{
    // Must match the issuer/audience in appsettings.Testing.json
    public const string Issuer   = "nexcore-api";
    public const string Audience = "nexcore-clients";

    // Long enough for HMAC-SHA256 (>= 32 bytes)
    public const string Secret = "nexcore-integration-test-secret-key-at-least-32-chars!";

    // Default tenant context — must match the JWT defaults in JwtTokenHelper.GenerateToken.
    // Any entity seeded directly via DbContext (bypassing TenantAwareRepository) must use
    // these same values so the tenant filter in GetAll/FindAsync can find them.
    public static readonly Guid TenantId       = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid UserId         = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid CompanyId      = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid BranchId       = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid BusinessUnitId = new("00000000-0000-0000-0000-000000000001");
}
