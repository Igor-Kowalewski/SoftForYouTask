using CustomerCatalog.Core.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerCatalog.Tests;

/// <summary>
/// Fixture that creates and tears down a test database on LocalDB. Shared by the
/// repository tests (xUnit creates a single instance per test class).
/// </summary>
public sealed class DatabaseFixture : IDisposable
{
    private const string DatabaseName = "CustomerCatalog_Test";
    private const string MasterConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    public IDbConnectionFactory ConnectionFactory { get; }

    public DatabaseFixture()
    {
        var connectionString =
            $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
        ConnectionFactory = new SqlConnectionFactory(connectionString);

        CreateDatabase();
        CreateTable();
    }

    private static void CreateDatabase()
    {
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        connection.Execute($"""
            IF DB_ID(N'{DatabaseName}') IS NULL
                CREATE DATABASE [{DatabaseName}];
            """);
    }

    private void CreateTable()
    {
        using var connection = ConnectionFactory.CreateOpenConnection();
        // Start from a clean, known schema (shared DDL from Core).
        connection.Execute("IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL DROP TABLE dbo.Customers;");
        connection.Execute(CustomerSchema.CreateTableSql);
    }

    /// <summary>Clears the table – called by tests for a repeatable initial state.</summary>
    public void ClearCustomers()
    {
        using var connection = ConnectionFactory.CreateOpenConnection();
        connection.Execute("DELETE FROM Customers; DBCC CHECKIDENT('Customers', RESEED, 0);");
    }

    public void Dispose()
    {
        // Close pooled connections so the database can be dropped.
        SqlConnection.ClearAllPools();

        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        connection.Execute($"""
            IF DB_ID(N'{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """);
    }
}
