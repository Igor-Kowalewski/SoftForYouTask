using CustomerCatalog.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class CustomerValidatorTests
{
    private static CustomerInput ValidInput() => new(
        Name: "Firma testowa Sp. z o.o.",
        Nip: "5260250274",
        Street: "ul. Testowa 1",
        PostalCode: "00-001",
        City: "Warszawa",
        Phone: "123 456 789",
        Email: "kontakt@example.com");

    [Fact]
    public void TryValidate_Succeeds_ForValidInput()
    {
        CustomerValidator.TryValidate(ValidInput(), out var customer, out var errors).Should().BeTrue();

        errors.Should().BeEmpty();
        customer.Should().NotBeNull();
        customer!.Name.Should().Be("Firma testowa Sp. z o.o.");
        customer.Nip.Value.Should().Be("5260250274");
        customer.Address.City.Should().Be("Warszawa");
        customer.Email.Value.Should().Be("kontakt@example.com");
    }

    [Fact]
    public void TryValidate_RequiresName()
    {
        var input = ValidInput() with { Name = "   " };

        CustomerValidator.TryValidate(input, out var customer, out var errors).Should().BeFalse();

        customer.Should().BeNull();
        errors.Should().ContainSingle().Which.Should().Contain("Nazwa");
    }

    [Fact]
    public void TryValidate_RejectsInvalidEmail()
    {
        var input = ValidInput() with { Email = "nieprawidlowy-email" };

        CustomerValidator.TryValidate(input, out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("E-mail"));
    }

    [Fact]
    public void TryValidate_RejectsInvalidNip()
    {
        var input = ValidInput() with { Nip = "0000000000" };

        CustomerValidator.TryValidate(input, out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("NIP"));
    }

    [Fact]
    public void TryValidate_RejectsInvalidAddress()
    {
        var input = ValidInput() with { PostalCode = "invalid" };

        CustomerValidator.TryValidate(input, out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Kod pocztowy"));
    }

    [Fact]
    public void TryValidate_RejectsTooLongPhone()
    {
        var input = ValidInput() with { Phone = new string('1', 31) };

        CustomerValidator.TryValidate(input, out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("telefonu"));
    }

    [Fact]
    public void TryValidate_ReportsAllErrors_WhenMultipleFieldsInvalid()
    {
        var input = ValidInput() with { Name = "", Nip = "invalid", Email = "invalid" };

        CustomerValidator.TryValidate(input, out _, out var errors).Should().BeFalse();
        errors.Should().HaveCount(3);
    }

    [Theory]
    [InlineData("Firma testowa", null)]
    [InlineData("", "Nazwa jest wymagana.")]
    [InlineData("   ", "Nazwa jest wymagana.")]
    [InlineData(null, "Nazwa jest wymagana.")]
    public void ValidateName_MatchesTryValidate(string? name, string? expectedMessage)
    {
        CustomerValidator.ValidateName(name).Should().Be(expectedMessage);
    }

    [Fact]
    public void ValidateName_RejectsTooLong()
    {
        CustomerValidator.ValidateName(new string('a', 201)).Should().Contain("200 znaków");
    }

    [Theory]
    [InlineData("123 456 789", null)]
    [InlineData("", null)] // phone is optional
    [InlineData(null, null)] // phone is optional
    public void ValidatePhone_MatchesTryValidate(string? phone, string? expectedMessage)
    {
        CustomerValidator.ValidatePhone(phone).Should().Be(expectedMessage);
    }

    [Fact]
    public void ValidatePhone_RejectsTooLong()
    {
        CustomerValidator.ValidatePhone(new string('1', 31)).Should().Contain("30 znaków");
    }
}
