using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auth.Tests.Integration.Fixtures;

/// <summary>
/// Base fixture for Auth integration tests
/// Provides database setup and cleanup
/// </summary>
public abstract class AuthIntegrationTestFixture : IAsyncLifetime
{
    protected DbContextOptions<AuthDbContext> DbContextOptions { get; private set; } = null!;
    protected AuthDbContext DbContext { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        // Use in-memory database for testing
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContextOptions = options;
        DbContext = new AuthDbContext(options);
        
        // Ensure database is created
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

    protected AuthDbContext CreateDbContext()
    {
        return new AuthDbContext(DbContextOptions);
    }
}
