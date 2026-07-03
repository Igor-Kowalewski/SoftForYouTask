using CustomerCatalog.Core.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerCatalog.Tests;

/// <summary>
/// Fixture tworzący i sprzątający testową bazę na LocalDB. Współdzielony przez testy
/// repozytorium (xUnit tworzy jedną instancję na klasę testową).
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
        connection.Execute("""
            IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL
                DROP TABLE dbo.Customers;

            CREATE TABLE dbo.Customers
            (
                Id        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                Name      NVARCHAR(200)     NOT NULL,
                Nip       NVARCHAR(10)      NOT NULL,
                Address   NVARCHAR(300)     NULL,
                Phone     NVARCHAR(30)      NULL,
                Email     NVARCHAR(200)     NULL,
                CreatedAt DATETIME2         NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
            );
            """);
    }

    /// <summary>Czyści tabelę – wywoływane przez testy dla powtarzalnego stanu początkowego.</summary>
    public void ClearCustomers()
    {
        using var connection = ConnectionFactory.CreateOpenConnection();
        connection.Execute("DELETE FROM Customers; DBCC CHECKIDENT('Customers', RESEED, 0);");
    }

    public void Dispose()
    {
        // Zamknięcie połączeń puli, aby dało się usunąć bazę.
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
