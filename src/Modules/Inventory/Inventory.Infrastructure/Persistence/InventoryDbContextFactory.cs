using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct <see cref="InventoryDbContext"/> WITHOUT building the
/// API host. Without this, EF falls back to Nexcore.Api's Program.cs, which requires Jwt:Secret (and
/// other runtime config) and throws during migrations. Mirrors CoreDbContextFactory.
///
/// For <c>database update</c>, supply the real connection via the
/// <c>ConnectionStrings__DefaultConnection</c> environment variable (the CI/CD pipeline sets this).
/// The placeholder below is only a last-resort default for offline scaffolding.
/// </summary>
public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=DesignTime;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseSqlServer(connection)
            .Options;

        return new InventoryDbContext(options);
    }
}
