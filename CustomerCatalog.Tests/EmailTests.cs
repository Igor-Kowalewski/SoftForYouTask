using CustomerCatalog.Core.Models;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("first.last@sub.example.co.uk")]
    [InlineData("  padded@example.com  ")] // trimmed before validation
    public void TryParse_Succeeds_ForValidEmail(string value)
    {
        Email.TryParse(value, out var result).Should().BeTrue();
        result!.Value.Should().Be(value.Trim());
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at.example.com")]
    [InlineData("no-domain@")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void TryParse_Fails_ForInvalidEmail(string? value)
    {
        Email.TryParse(value, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Parse_Throws_ForInvalidEmail()
    {
        var act = () => Email.Parse("invalid");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Email.Parse("test@example.com").Should().Be(Email.Parse("test@example.com"));
    }
}
