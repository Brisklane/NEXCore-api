namespace Auth.Tests.Unit.Fixtures;

/// <summary>
/// Base fixture for Auth unit tests
/// Provides common setup and mock dependencies
/// </summary>
public abstract class AuthUnitTestFixture : IDisposable
{
    public virtual void SetUp()
    {
        // Override in derived classes for custom setup
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup resources if needed
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
