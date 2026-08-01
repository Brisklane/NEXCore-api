using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexcore.SharedKernel.Persistence;

namespace Sales.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct <see cref="SalesDbContext"/> WITHOUT building the
/// API host (which requires Jwt:Secret and other runtime config). Mirrors InventoryDbContextFactory /
/// CoreDbContextFactory for consistency.
///
/// For <c>database update</c>, supply the real connection via the
/// <c>ConnectionStrings__DefaultConnection</c> environment variable (the CI/CD pipeline sets this).
/// The placeholder below is only a last-resort default for offline scaffolding (<c>migrations add</c>
/// never opens a connection).
/// </summary>
public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=nexcore;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNexcorePostgres(connection, typeof(SalesDbContext).Assembly)
            .Options;

        return new SalesDbContext(options);
    }
}
