using Bogus;
using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Validation;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Zapewnia istnienie bazy i tabeli oraz – gdy tabela jest pusta – wypełnia ją
/// danymi testowymi wygenerowanymi przez Bogus.
/// </summary>
public sealed class DatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly int _seedCount;

    public DatabaseInitializer(IDbConnectionFactory connectionFactory, int seedCount = 50)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _seedCount = seedCount;
    }

    /// <summary>
    /// Tworzy bazę (jeśli brak), tabelę Customers (jeśli brak) i seeduje dane, gdy tabela jest pusta.
    /// </summary>
    public void EnsureCreatedAndSeeded()
    {
        EnsureDatabaseExists();
        EnsureTableExists();
        SeedIfEmpty();
    }

    private void EnsureDatabaseExists()
    {
        var builder = new SqlConnectionStringBuilder(_connectionFactory.ConnectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Connection string nie zawiera nazwy bazy (Initial Catalog / Database).");

        // Łączymy się do 'master', aby móc utworzyć docelową bazę.
        builder.InitialCatalog = "master";
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();

        // Nazwa bazy jest kontrolowana przez konfigurację, ale QUOTENAME chroni przed wstrzyknięciem.
        var sql = $"""
            IF DB_ID(@dbName) IS NULL
            BEGIN
                DECLARE @cmd nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@dbName);
                EXEC(@cmd);
            END
            """;
        connection.Execute(sql, new { dbName = databaseName });
        Log.Information("Baza {Database} gotowa.", databaseName);
    }

    private void EnsureTableExists()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
            BEGIN
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
            END
            """;
        connection.Execute(sql);
    }

    private void SeedIfEmpty()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Customers;");
        if (count > 0)
            return;

        var customers = GenerateFakeCustomers(_seedCount);
        const string insert = """
            INSERT INTO Customers (Name, Nip, Address, Phone, Email, CreatedAt)
            VALUES (@Name, @Nip, @Address, @Phone, @Email, @CreatedAt);
            """;
        connection.Execute(insert, customers);
        Log.Information("Zaseedowano {Count} klientów danymi testowymi (Bogus).", customers.Count);
    }

    /// <summary>
    /// Generuje listę losowych klientów. Publiczne, aby mogło być użyte także w testach.
    /// Używa stałego seeda dla powtarzalności.
    /// </summary>
    public static IReadOnlyList<Customer> GenerateFakeCustomers(int count)
    {
        Randomizer.Seed = new Random(12345);

        var faker = new Faker<Customer>("pl")
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.Nip, f => GenerateValidNip(f))
            .RuleFor(c => c.Address, f => $"{f.Address.StreetAddress()}, {f.Address.ZipCode()} {f.Address.City()}")
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber("### ### ###"))
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(provider: "example.com"))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2));

        return faker.Generate(count);
    }

    /// <summary>Generuje poprawny NIP (9 losowych cyfr + wyliczona cyfra kontrolna).</summary>
    private static string GenerateValidNip(Faker f)
    {
        int[] weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };
        while (true)
        {
            var digits = new int[10];
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                digits[i] = f.Random.Int(0, 9);
                sum += digits[i] * weights[i];
            }

            var check = sum % 11;
            if (check == 10)
                continue; // Nieprawidłowa suma kontrolna – losujemy ponownie.

            digits[9] = check;
            var nip = string.Concat(digits);
            if (NipValidator.IsValid(nip))
                return nip;
        }
    }
}
