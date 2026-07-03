using CustomerCatalog.Core.Models;
using CustomerCatalog.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class CustomerValidatorTests
{
    private static Customer ValidCustomer() => new()
    {
        Name = "Firma testowa Sp. z o.o.",
        Nip = "5260250274",
        Address = "ul. Testowa 1, 00-001 Warszawa",
        Phone = "123 456 789",
        Email = "kontakt@example.com"
    };

    [Fact]
    public void Validate_ReturnsNoErrors_ForValidCustomer()
    {
        CustomerValidator.Validate(ValidCustomer()).Should().BeEmpty();
    }

    [Fact]
    public void Validate_RequiresName()
    {
        var customer = ValidCustomer();
        customer.Name = "   ";

        CustomerValidator.Validate(customer).Should().ContainSingle()
            .Which.Should().Contain("Nazwa");
    }

    [Fact]
    public void Validate_RejectsInvalidEmail()
    {
        var customer = ValidCustomer();
        customer.Email = "nieprawidlowy-email";

        CustomerValidator.Validate(customer).Should().Contain(e => e.Contains("E-mail"));
    }

    [Fact]
    public void Validate_RejectsInvalidNip()
    {
        var customer = ValidCustomer();
        customer.Nip = "0000000000";

        CustomerValidator.Validate(customer).Should().Contain(e => e.Contains("NIP"));
    }
}
