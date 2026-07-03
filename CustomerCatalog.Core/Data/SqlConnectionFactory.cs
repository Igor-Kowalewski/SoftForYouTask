using System.Data;
using Microsoft.Data.SqlClient;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Fabryka połączeń SQL Server (LocalDB) oparta na Microsoft.Data.SqlClient.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string nie może być pusty.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    public IDbConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
