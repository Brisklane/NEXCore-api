using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexcore.SharedKernel.Persistence;

namespace Distribution.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can build <see cref="DistributionDbContext"/> without
/// starting the API host (which needs Jwt:Secret and the rest of the runtime configuration).
/// Mirrors the other modules' factories.
///
/// For <c>database update</c>, supply the real connection through the
/// <c>ConnectionStrings__DefaultConnection</c> environment variable; the placeholder below only
/// serves offline scaffolding, where <c>migrations add</c> never opens a connection.
/// </summary>
public class DistributionDbContextFactory : IDesignTimeDbContextFactory<DistributionDbContext>
{
    public DistributionDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=nexcore;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<DistributionDbContext>()
            .UseNexcorePostgres(connection, typeof(DistributionDbContext).Assembly)
            .Options;

        return new DistributionDbContext(options);
    }
}
