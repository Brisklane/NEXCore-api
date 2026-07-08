using Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Core.Tests.Integration.Fixtures;

/// <summary>
/// Base fixture for Core integration tests.
/// Spins up a fresh InMemory database per test class and tears it down after.
/// </summary>
public abstract class CoreIntegrationTestFixture : IAsyncLifetime
{
    protected DbContextOptions<CoreDbContext> DbContextOptions { get; private set; } = null!;
    protected CoreDbContext DbContext { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        DbContextOptions = options;
        DbContext = new CoreDbContext(options);

        await DbContext.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a second independent DbContext instance pointing at the same InMemory database.
    /// Useful for verifying persisted state without using the same tracked context.
    /// </summary>
    protected CoreDbContext CreateDbContext()
    {
        return new CoreDbContext(DbContextOptions);
    }
}
