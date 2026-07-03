using System.Data;
using CustomerCatalog.Core.Data;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

[Collection(DatabaseCollection.Name)]
public class SqlConnectionFactoryTests
{
    private readonly DatabaseFixture _fixture;

    public SqlConnectionFactoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsArgumentException_ForNullEmptyOrWhitespaceConnectionString(string? connectionString)
    {
        var act = () => new SqlConnectionFactory(connectionString!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_SetsConnectionString()
    {
        var factory = new SqlConnectionFactory(_fixture.ConnectionFactory.ConnectionString);
        factory.ConnectionString.Should().Be(_fixture.ConnectionFactory.ConnectionString);
    }

    [Fact]
    public void CreateOpenConnection_ReturnsOpenConnection()
    {
        var factory = new SqlConnectionFactory(_fixture.ConnectionFactory.ConnectionString);

        using var connection = factory.CreateOpenConnection();

        connection.State.Should().Be(ConnectionState.Open);
    }
}
