using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Models;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CustomerCatalog.Tests;

/// <summary>
/// Repository integration tests (Dapper against LocalDB). Require SQL Server LocalDB to be installed.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class CustomerRepositoryTests
{
    private readonly DatabaseFixture _fixture;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearCustomers();
        _repository = new CustomerRepository(_fixture.ConnectionFactory);
    }

    // Distinct, checksum-valid NIPs (Nip is a UNIQUE column) so tests that insert more than
    // one customer don't collide with each other.
    private static Customer NewCustomer(string name = "Firma testowa", string nip = "5260250274") => new()
    {
        Name = name,
        Nip = Nip.Parse(nip),
        Address = Address.Parse("ul. Testowa 1", "00-001", "Warszawa"),
        Phone = "123 456 789",
        Email = Email.Parse("kontakt@example.com")
    };

    [Fact]
    public void Constructor_NullConnectionFactory_Throws()
    {
        var act = () => new CustomerRepository(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Insert_AssignsId_AndPersists()
    {
        var customer = NewCustomer();

        var id = _repository.Insert(customer);

        id.Should().BeGreaterThan(0);
        customer.Id.Should().Be(id);

        var loaded = _repository.GetById(id);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(customer.Name);
        loaded.Nip.Should().Be(customer.Nip);
        loaded.Address.Should().Be(customer.Address);
        loaded.Email.Should().Be(customer.Email);
    }

    [Fact]
    public void Insert_PreservesExplicitCreatedAt_WhenAlreadySet()
    {
        var customer = NewCustomer();
        var explicitCreatedAt = new DateTime(2020, 5, 17, 10, 30, 0);
        customer.CreatedAt = explicitCreatedAt;

        var id = _repository.Insert(customer);

        _repository.GetById(id)!.CreatedAt.Should().Be(explicitCreatedAt);
    }

    [Fact]
    public void Insert_Throws_WhenNipAlreadyExists()
    {
        _repository.Insert(NewCustomer("Pierwszy", "5260250274"));

        var act = () => _repository.Insert(NewCustomer("Drugi", "5260250274"));

        act.Should().Throw<SqlException>();
    }

    [Fact]
    public void GetById_ReturnsNull_WhenMissing()
    {
        _repository.GetById(999999).Should().BeNull();
    }

    [Fact]
    public void Update_ChangesValues()
    {
        var customer = NewCustomer();
        _repository.Insert(customer);

        customer.Name = "Nowa nazwa";
        customer.Email = Email.Parse("nowy@example.com");
        customer.Address = Address.Parse("ul. Nowa 5", "11-222", "Kraków");
        var updated = _repository.Update(customer);

        updated.Should().BeTrue();
        var loaded = _repository.GetById(customer.Id);
        loaded!.Name.Should().Be("Nowa nazwa");
        loaded.Email.Should().Be(Email.Parse("nowy@example.com"));
        loaded.Address.Should().Be(Address.Parse("ul. Nowa 5", "11-222", "Kraków"));
    }

    [Fact]
    public void Update_ReturnsFalse_WhenMissing()
    {
        var customer = NewCustomer();
        customer.Id = 123456;

        _repository.Update(customer).Should().BeFalse();
    }

    [Fact]
    public void Delete_RemovesRecord()
    {
        var customer = NewCustomer();
        _repository.Insert(customer);

        _repository.Delete(customer.Id).Should().BeTrue();
        _repository.GetById(customer.Id).Should().BeNull();
    }

    [Fact]
    public void Delete_ReturnsFalse_WhenMissing()
    {
        _repository.Delete(999999).Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsInsertedCustomers_OrderedByName()
    {
        _repository.Insert(NewCustomer("Zeta", "1357924688"));
        _repository.Insert(NewCustomer("Alfa", "5222222229"));
        _repository.Insert(NewCustomer("Beta", "9876543210"));

        var all = _repository.GetAll();

        all.Should().HaveCount(3);
        all.Select(c => c.Name).Should().ContainInOrder("Alfa", "Beta", "Zeta");
    }

    [Fact]
    public void ExistsByNip_ReturnsFalse_WhenNipUnused()
    {
        _repository.ExistsByNip("5260250274", excludeId: null).Should().BeFalse();
    }

    [Fact]
    public void ExistsByNip_ReturnsTrue_WhenAnotherCustomerHasSameNip()
    {
        _repository.Insert(NewCustomer("Pierwszy", "5260250274"));

        _repository.ExistsByNip("5260250274", excludeId: null).Should().BeTrue();
    }

    [Fact]
    public void ExistsByNip_ReturnsFalse_WhenOnlyMatchIsTheExcludedCustomer()
    {
        var customer = NewCustomer("Ktoś", "5260250274");
        _repository.Insert(customer);

        _repository.ExistsByNip("5260250274", excludeId: customer.Id).Should().BeFalse();
    }
}
