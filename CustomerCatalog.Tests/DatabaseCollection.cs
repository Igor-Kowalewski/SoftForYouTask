using Xunit;

namespace CustomerCatalog.Tests;

/// <summary>
/// Groups every test class that shares <see cref="DatabaseFixture"/> (and therefore the same
/// "CustomerCatalog_Test" LocalDB database) into one xUnit collection. xUnit creates a single
/// shared fixture instance per collection and never runs test classes within the same
/// collection in parallel with each other - without this, two independently-created
/// <see cref="DatabaseFixture"/> instances (one per test class) would race to create/drop the
/// same database.
/// </summary>
[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database collection";
}
