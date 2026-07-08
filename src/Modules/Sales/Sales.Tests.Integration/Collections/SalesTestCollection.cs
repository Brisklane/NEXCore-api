namespace Sales.Tests.Integration.Collections;

/// <summary>
/// xUnit collection definition that ties all Sales integration test classes to a single
/// SalesCollectionFixture instance — one SQL container, one migrated database, shared
/// across the entire collection.
/// </summary>
[CollectionDefinition(Name)]
public class SalesTestCollection : ICollectionFixture<SalesCollectionFixture>
{
    public const string Name = "Sales Integration Tests";
}
