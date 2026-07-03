using CustomerCatalog.Core.Validation;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class NipValidatorTests
{
    [Theory]
    [InlineData("5260250274")]    // valid NIP (correct checksum)
    [InlineData("1234563218")]
    [InlineData("526-025-02-74")] // with separators – still valid
    public void IsValid_ReturnsTrue_ForValidNip(string nip)
    {
        NipValidator.IsValid(nip).Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890")]  // checksum = 10 → invalid
    [InlineData("5260250275")]  // wrong check digit
    [InlineData("0000000000")]  // all zeros
    [InlineData("123")]         // too short
    [InlineData("12345678901")] // too long
    [InlineData("abcdefghij")]  // no digits
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
