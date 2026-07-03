using System.Data;
using Microsoft.Data.SqlClient;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// SQL Server (LocalDB) connection factory based on Microsoft.Data.SqlClient.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    public string ConnectionString { get; }

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    public IDbConnection CreateOpenConnection()
    {
        var connection = new SqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
