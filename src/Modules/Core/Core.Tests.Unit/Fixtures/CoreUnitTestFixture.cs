namespace Core.Tests.Unit.Fixtures;

/// <summary>
/// Base fixture for Core unit tests.
/// Provides lifecycle hooks; derive from this in all test classes that need shared setup.
/// </summary>
public abstract class CoreUnitTestFixture : IDisposable
{
    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
