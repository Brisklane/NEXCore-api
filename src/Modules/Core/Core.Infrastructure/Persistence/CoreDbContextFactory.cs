using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nexcore.SharedKernel.Persistence;

namespace Core.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct <see cref="CoreDbContext"/> without the API host
/// (Core.Infrastructure is a class library with no Program/DI).
///
/// `migrations add` / `migrations script` do not connect to the database, so any string works there.
/// For `database update`, supply the REAL connection so it targets the correct database — either:
///   • set the env var  ConnectionStrings__DefaultConnection  before running, or
///   • pass  --connection "&lt;real connection string&gt;"  on the dotnet-ef command (this overrides the factory).
/// The placeholder below is only a last-resort default for offline scaffolding.
/// </summary>
public class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CORE_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=nexcore;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNexcorePostgres(connection, typeof(CoreDbContext).Assembly)
            .Options;

        return new CoreDbContext(options);
    }
}
