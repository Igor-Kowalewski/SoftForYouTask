using CustomerCatalog.Core.Models;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class AddressTests
{
    [Fact]
    public void TryParse_Succeeds_ForValidInput()
    {
        Address.TryParse("ul. Kwiatowa 5", "00-001", "Warszawa", out var address, out var errors)
            .Should().BeTrue();

        errors.Should().BeEmpty();
        address!.Street.Should().Be("ul. Kwiatowa 5");
        address.PostalCode.Should().Be("00-001");
        address.City.Should().Be("Warszawa");
    }

    [Theory]
    [InlineData("00001")]   // missing dash
    [InlineData("00-0011")] // too many digits
    [InlineData("AB-CDE")]  // not digits
    [InlineData("")]
    public void TryParse_Fails_ForInvalidPostalCode(string postalCode)
    {
        Address.TryParse("ul. Kwiatowa 5", postalCode, "Warszawa", out var address, out var errors)
            .Should().BeFalse();

        address.Should().BeNull();
        errors.Should().Contain(e => e.Contains("Kod pocztowy"));
    }

    [Fact]
    public void TryParse_Fails_WhenStreetMissing()
    {
        Address.TryParse("   ", "00-001", "Warszawa", out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Ulica"));
    }

    [Fact]
    public void TryParse_Fails_WhenCityMissing()
    {
        Address.TryParse("ul. Kwiatowa 5", "00-001", "   ", out _, out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Miasto"));
    }

    [Fact]
    public void TryParse_ReportsAllErrors_WhenMultipleFieldsInvalid()
    {
        Address.TryParse("", "invalid", "", out _, out var errors).Should().BeFalse();
        errors.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_Throws_ForInvalidInput()
    {
        var act = () => Address.Parse("", "invalid", "");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void ToString_CombinesFields()
    {
        var address = Address.Parse("ul. Testowa 1", "00-001", "Warszawa");
        address.ToString().Should().Be("ul. Testowa 1, 00-001 Warszawa");
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Address.Parse("ul. Testowa 1", "00-001", "Warszawa")
            .Should().Be(Address.Parse("ul. Testowa 1", "00-001", "Warszawa"));
    }

    [Theory]
    [InlineData("ul. Kwiatowa 5", null)]
    [InlineData("", "Ulica jest wymagana.")]
    [InlineData("   ", "Ulica jest wymagana.")]
    [InlineData(null, "Ulica jest wymagana.")]
    public void ValidateStreet_MatchesTryParse(string? street, string? expectedMessage)
    {
        Address.ValidateStreet(street).Should().Be(expectedMessage);
    }

    [Fact]
    public void ValidateStreet_RejectsTooLong()
    {
        Address.ValidateStreet(new string('a', 201)).Should().Contain("200 znaków");
    }

    [Theory]
    [InlineData("00-001", null)]
    [InlineData("invalid", "Kod pocztowy jest niepoprawny (oczekiwany format NN-NNN).")]
    [InlineData(null, "Kod pocztowy jest niepoprawny (oczekiwany format NN-NNN).")]
    public void ValidatePostalCode_MatchesTryParse(string? postalCode, string? expectedMessage)
    {
        Address.ValidatePostalCode(postalCode).Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData("Warszawa", null)]
    [InlineData("", "Miasto jest wymagane.")]
    [InlineData(null, "Miasto jest wymagane.")]
    public void ValidateCity_MatchesTryParse(string? city, string? expectedMessage)
    {
        Address.ValidateCity(city).Should().Be(expectedMessage);
    }

    [Fact]
    public void ValidateCity_RejectsTooLong()
    {
        Address.ValidateCity(new string('a', 101)).Should().Contain("100 znaków");
    }
}
