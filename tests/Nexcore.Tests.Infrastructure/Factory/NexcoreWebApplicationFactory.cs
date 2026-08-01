using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Nexcore.Tests.Infrastructure.Factory;

/// <summary>
/// Shared WebApplicationFactory that spins up a real PostgreSQL container via TestContainers.
/// All module integration test collections derive from this or use it directly.
///
/// Usage:
///   1. Extend this class in each module's fixture and call ApplyMigrationsAsync for that module.
///   2. Use CreateAuthenticatedClient() to get an HttpClient with a pre-set JWT bearer token.
/// </summary>
public class NexcoreWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Pinned to the same major version as the local/dev server (17) so tests exercise the
    // behaviour developers actually run against. Waiting on the port is enough — EF
    // MigrateAsync retries while the server finishes its first-start initialisation.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("nexcore_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    public virtual async Task InitializeAsync() => await _postgres.StartAsync();

    // Explicit implementation avoids hiding WebApplicationFactory.DisposeAsync() (ValueTask)
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Point every module DbContext at the TestContainers PostgreSQL
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,

                // JWT settings used to sign and validate test tokens
                ["Jwt:Secret"] = TestJwtSettings.Secret,
                ["Jwt:Issuer"] = TestJwtSettings.Issuer,
                ["Jwt:Audience"] = TestJwtSettings.Audience,

                // Stub out external dependencies so tests never hit real services
                ["BaseUrls:AuthApiBaseUrl"] = "http://localhost/api/auth/",
            }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient pre-authorised with a JWT token containing the given permissions
    /// and full tenant context (companyId, branchId, businessUnitId).
    /// </summary>
    // AllowAutoRedirect=false prevents the HTTPS redirect (app.UseHttpsRedirection) from
    // causing HttpClient to follow the 307 and silently drop the Authorization header.
    private static readonly WebApplicationFactoryClientOptions NoRedirectOptions = new()
    {
        AllowAutoRedirect = false,
    };

    public HttpClient CreateAuthenticatedClient(
        string[] permissions,
        string tenantId       = "00000000-0000-0000-0000-000000000001",
        string companyId      = "00000000-0000-0000-0000-000000000001",
        string branchId       = "00000000-0000-0000-0000-000000000001",
        string businessUnitId = "00000000-0000-0000-0000-000000000001",
        string userId         = "00000000-0000-0000-0000-000000000001")
    {
        var client = CreateClient(NoRedirectOptions);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                JwtTokenHelper.GenerateToken(permissions, userId, tenantId, companyId, branchId, businessUnitId));
        return client;
    }

    /// <summary>
    /// Convenience overload — creates an authenticated client with no specific permissions
    /// but full default tenant context.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
        => CreateAuthenticatedClient([]);

    /// <inheritdoc cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/>
    public new HttpClient CreateClient()
        => base.CreateClient(NoRedirectOptions);

    /// <summary>
    /// Applies EF Core migrations for the given DbContext types.
    /// Call this once per test collection after the container has started.
    /// </summary>
    public async Task ApplyMigrationsAsync(params Type[] dbContextTypes)
    {
        using var scope = Services.CreateScope();
        foreach (var contextType in dbContextTypes)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            await db.Database.MigrateAsync();
        }
    }
}
