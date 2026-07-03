using CustomerCatalog.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class NipValidatorTests
{
    [Theory]
    [InlineData("5260250274")] // poprawny NIP (suma kontrolna OK)
    [InlineData("1234563218")]
    [InlineData("526-025-02-74")] // z separatorami – też poprawny
    public void IsValid_ReturnsTrue_ForValidNip(string nip)
    {
        NipValidator.IsValid(nip).Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")] // suma kontrolna = 10 → niepoprawny
    [InlineData("5260250275")] // zła cyfra kontrolna
    [InlineData("0000000000")] // same zera
    [InlineData("123")]        // za krótki
    [InlineData("12345678901")] // za długi
    [InlineData("abcdefghij")] // brak cyfr
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_ReturnsFalse_ForInvalidNip(string? nip)
    {
        NipValidator.IsValid(nip).Should().BeFalse();
    }

    [Fact]
    public void Normalize_StripsSeparators()
    {
        NipValidator.Normalize("526-025-02-74").Should().Be("5260250274");
    }
}
