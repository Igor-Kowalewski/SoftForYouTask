using CustomerCatalog.Core.Data;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class DatabaseInitializerTests
{
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
