using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Services;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class CustomerQueryTests
{
    // NIPs below are real, checksum-valid values (not just distinct digit strings),
    // since Nip.Parse now enforces the checksum at construction time.
    private static List<Customer> Sample() => new()
    {
        new Customer
        {
            Name = "Zeta", Nip = Nip.Parse("1357924688"), Email = Email.Parse("z@example.com"),
            Phone = "111", Address = Address.Parse("ul. Zetowa 1", "30-001", "Kraków"),
            CreatedAt = new DateTime(2024, 1, 3)
        },
        new Customer
        {
            Name = "Alfa", Nip = Nip.Parse("5222222229"), Email = Email.Parse("a@test.com"),
            Phone = "222", Address = Address.Parse("ul. Alfowa 1", "00-001", "Warszawa"),
            CreatedAt = new DateTime(2024, 1, 1)
        },
        new Customer
        {
            Name = "Beta", Nip = Nip.Parse("9876543210"), Email = Email.Parse("b@example.com"),
            Phone = "333", Address = Address.Parse("ul. Betowa 1", "80-001", "Gdańsk"),
            CreatedAt = new DateTime(2024, 1, 2)
        },
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
        // Alfa's NIP (5222222229) is the only one containing "2222".
        CustomerQuery.Filter(Sample(), "2222").Should().ContainSingle()
            .Which.Name.Should().Be("Alfa");
    }

    [Fact]
    public void Filter_MatchesAddress()
    {
        CustomerQuery.Filter(Sample(), "Gdańsk").Should().ContainSingle()
            .Which.Name.Should().Be("Beta");
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
