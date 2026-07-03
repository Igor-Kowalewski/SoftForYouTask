using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Services;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class CustomerQueryTests
{
    private static List<Customer> Sample() => new()
    {
        new Customer { Name = "Zeta", Nip = "1111111111", Email = "z@example.com", Phone = "111", Address = "Kraków", CreatedAt = new DateTime(2024, 1, 3) },
        new Customer { Name = "Alfa", Nip = "2222222222", Email = "a@test.com", Phone = "222", Address = "Warszawa", CreatedAt = new DateTime(2024, 1, 1) },
        new Customer { Name = "Beta", Nip = "3333333333", Email = "b@example.com", Phone = "333", Address = "Gdańsk", CreatedAt = new DateTime(2024, 1, 2) },
    };

    [Fact]
    public void Filter_EmptyTerm_ReturnsAll()
    {
        CustomerQuery.Filter(Sample(), "  ").Should().HaveCount(3);
    }

    [Fact]
    public void Filter_MatchesName_CaseInsensitive()
    {
        CustomerQuery.Filter(Sample(), "alf").Should().ContainSingle()
            .Which.Name.Should().Be("Alfa");
    }

    [Fact]
    public void Filter_MatchesAcrossMultipleFields()
    {
        // "example.com" matches the e-mail of Zeta and Beta.
        CustomerQuery.Filter(Sample(), "example.com")
            .Select(c => c.Name).Should().BeEquivalentTo(new[] { "Zeta", "Beta" });
    }

    [Fact]
    public void Filter_MatchesNip()
    {
        CustomerQuery.Filter(Sample(), "2222").Should().ContainSingle()
            .Which.Name.Should().Be("Alfa");
    }

    [Fact]
    public void Sort_ByName_Ascending()
    {
        CustomerQuery.Sort(Sample(), nameof(Customer.Name), ascending: true)
            .Select(c => c.Name).Should().ContainInOrder("Alfa", "Beta", "Zeta");
    }

    [Fact]
    public void Sort_ByName_Descending()
    {
        CustomerQuery.Sort(Sample(), nameof(Customer.Name), ascending: false)
            .Select(c => c.Name).Should().ContainInOrder("Zeta", "Beta", "Alfa");
    }

    [Fact]
    public void Sort_ByCreatedAt_Ascending()
    {
        CustomerQuery.Sort(Sample(), nameof(Customer.CreatedAt), ascending: true)
            .Select(c => c.Name).Should().ContainInOrder("Alfa", "Beta", "Zeta");
    }

    [Fact]
    public void Sort_UnknownProperty_FallsBackToName()
    {
        CustomerQuery.Sort(Sample(), "DoesNotExist", ascending: true)
            .Select(c => c.Name).Should().ContainInOrder("Alfa", "Beta", "Zeta");
    }
}
