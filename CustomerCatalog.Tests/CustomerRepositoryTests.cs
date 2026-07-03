using CustomerCatalog.Core.Data;
using CustomerCatalog.Core.Models;
using FluentAssertions;
using Xunit;

namespace CustomerCatalog.Tests;

/// <summary>
/// Testy integracyjne repozytorium (Dapper na LocalDB). Wymagają zainstalowanego SQL Server LocalDB.
/// </summary>
public class CustomerRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearCustomers();
        _repository = new CustomerRepository(_fixture.ConnectionFactory);
    }

    private static Customer NewCustomer(string name = "Firma testowa") => new()
    {
        Name = name,
        Nip = "5260250274",
        Address = "ul. Testowa 1, 00-001 Warszawa",
        Phone = "123 456 789",
        Email = "kontakt@example.com"
    };

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
        loaded.Email.Should().Be(customer.Email);
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
        customer.Email = "nowy@example.com";
        var updated = _repository.Update(customer);

        updated.Should().BeTrue();
        var loaded = _repository.GetById(customer.Id);
        loaded!.Name.Should().Be("Nowa nazwa");
        loaded.Email.Should().Be("nowy@example.com");
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
        _repository.Insert(NewCustomer("Zeta"));
        _repository.Insert(NewCustomer("Alfa"));
        _repository.Insert(NewCustomer("Beta"));

        var all = _repository.GetAll();

        all.Should().HaveCount(3);
        all.Select(c => c.Name).Should().ContainInOrder("Alfa", "Beta", "Zeta");
    }
}
