using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerCatalog.Tests;

/// <summary>
/// Fixture for <see cref="DatabaseInitializerTests"/>. Unlike <see cref="DatabaseFixture"/>,
/// it does NOT pre-create the database or table - the point of these tests is to let
/// <c>DatabaseInitializer.EnsureCreatedAndSeeded()</c> do that itself. Uses its own database
/// name so it never collides with <see cref="DatabaseFixture"/>'s "CustomerCatalog_Test" when
/// xUnit runs test classes in parallel.
/// </summary>
public sealed class DatabaseInitializerFixture : IDisposable
{
    private const string DatabaseName = "CustomerCatalog_InitializerTest";
    private const string MasterConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    public string ConnectionString { get; } =
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    public DatabaseInitializerFixture()
    {
        // Defensive: drop any leftover database from a previous crashed run.
        DropIfExists();
    }

    public void Dispose()
    {
        DropIfExists();
    }

    private static void DropIfExists()
    {
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
