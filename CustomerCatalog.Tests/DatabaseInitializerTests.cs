using CustomerCatalog.Core.Data;
using Dapper;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class DatabaseInitializerTests : IClassFixture<DatabaseInitializerFixture>
{
    private readonly DatabaseInitializerFixture _fixture;

    public DatabaseInitializerTests(DatabaseInitializerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Constructor_NullConnectionFactory_Throws()
    {
        var act = () => new DatabaseInitializer(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureCreatedAndSeeded_ConnectionStringMissingDatabase_Throws()
    {
        var factory = new SqlConnectionFactory(
            "Server=(localdb)\\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;");
        var initializer = new DatabaseInitializer(factory);

        var act = initializer.EnsureCreatedAndSeeded;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureCreatedAndSeeded_CreatesSchemaAndSeeds_ThenIsIdempotent()
    {
        var factory = new SqlConnectionFactory(_fixture.ConnectionString);

        new DatabaseInitializer(factory, seedCount: 5).EnsureCreatedAndSeeded();

        using (var connection = factory.CreateOpenConnection())
        {
            connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Customers;").Should().Be(5);
        }

        // Calling it again against an already-created, already-seeded database must not
        // create the DB/table a second time nor reseed - the row count stays the same
        // even though a different seed count is requested.
        new DatabaseInitializer(factory, seedCount: 7).EnsureCreatedAndSeeded();

        using (var connection = factory.CreateOpenConnection())
        {
            connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Customers;").Should().Be(5);
        }
    }

    [Fact]
    public void GenerateFakeCustomers_ProducesRequestedCount()
    {
        DatabaseInitializer.GenerateFakeCustomers(50).Should().HaveCount(50);
    }

    [Fact]
    public void GenerateFakeCustomers_ProducesCustomersWithRequiredFields()
    {
        // Nip/Email/Address are valid by construction – generation would already have
        // thrown a FormatException if any of them were invalid. This just sanity-checks
        // the remaining plain fields that have no value object of their own. Runs at the
        // real production seed count so it also confirms generation doesn't throw at scale.
        var customers = DatabaseInitializer.GenerateFakeCustomers(DatabaseInitializer.DefaultSeedCount);

        customers.Should().HaveCount(DatabaseInitializer.DefaultSeedCount);
        customers.Should().OnlyContain(c => !string.IsNullOrWhiteSpace(c.Name) && c.Name.Length <= 200);
        customers.Should().OnlyContain(c => c.Phone.Length <= 30);
    }

    [Fact]
    public void GenerateFakeCustomers_ProducesUniqueNips()
    {
        // Nip is a UNIQUE database column - a collision here would make seeding fail outright.
        var customers = DatabaseInitializer.GenerateFakeCustomers(DatabaseInitializer.DefaultSeedCount);

        customers.Select(c => c.Nip.Value).Should().OnlyHaveUniqueItems();
    }
}
