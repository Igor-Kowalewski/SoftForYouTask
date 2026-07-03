using CustomerCatalog.Core.Models;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class NipTests
{
    [Theory]
    [InlineData("5260250274")]    // valid NIP (correct checksum)
    [InlineData("1234563218")]
    [InlineData("526-025-02-74")] // with separators – still valid
    public void TryParse_Succeeds_ForValidNip(string nip)
    {
        Nip.TryParse(nip, out var result).Should().BeTrue();
        result!.Value.Should().Be(new string(nip.Where(char.IsDigit).ToArray()));
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
    public void TryParse_Fails_ForInvalidNip(string? nip)
    {
        Nip.TryParse(nip, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_Throws_ForInvalidNip()
    {
        var act = () => Nip.Parse("invalid");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void IsValid_MatchesTryParse()
    {
        Nip.IsValid("5260250274").Should().BeTrue();
        Nip.IsValid("0000000000").Should().BeFalse();
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Nip.Parse("526-025-02-74").Should().Be(Nip.Parse("5260250274"));
    }
}
