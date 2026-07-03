using System.Data;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Creates database connections. The abstraction makes testing easier
/// (a different connection string, e.g. a test database, can be injected).
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Connection string pointing at the target catalog database.</summary>
    string ConnectionString { get; }

    /// <summary>Returns a new, open connection.</summary>
    IDbConnection CreateOpenConnection();
}
