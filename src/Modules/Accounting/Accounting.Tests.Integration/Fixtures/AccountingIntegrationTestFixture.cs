using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Accounting.Tests.Integration.Fixtures;

/// <summary>
/// Base fixture for Accounting integration tests.
/// Spins up a fresh InMemory database per test class and tears it down after.
/// </summary>
public abstract class AccountingIntegrationTestFixture : IAsyncLifetime
{
    protected DbContextOptions<AccountingDbContext> DbContextOptions { get; private set; } = null!;
    protected AccountingDbContext DbContext { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AccountingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        DbContextOptions = options;
        DbContext = new AccountingDbContext(options);

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
    protected AccountingDbContext CreateDbContext()
    {
        return new AccountingDbContext(DbContextOptions);
    }
}
