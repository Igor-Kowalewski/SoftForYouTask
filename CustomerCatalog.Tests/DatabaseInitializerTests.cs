using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Validation;
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
    public void GenerateFakeCustomers_ProducesOnlyValidCustomers()
    {
        var customers = DatabaseInitializer.GenerateFakeCustomers(50);

        customers.Should().OnlyContain(c => CustomerValidator.IsValid(c));
        customers.Should().OnlyContain(c => NipValidator.IsValid(c.Nip));
    }
}
