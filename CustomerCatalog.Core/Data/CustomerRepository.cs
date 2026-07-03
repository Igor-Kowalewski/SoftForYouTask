using CustomerCatalog.Core.Models;
using Dapper;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Dapper-based repository implementation (parameterized SQL queries). Dapper only ever
/// sees the flat <see cref="CustomerRow"/> shape; mapping to and from the richer
/// <see cref="Customer"/> domain model (with its Nip/Address/Email value objects) happens
/// explicitly here, rather than through Dapper type-handler magic.
/// </summary>
public sealed class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CustomerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IReadOnlyList<Customer> GetAll()
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT Id, Name, Nip, Street, PostalCode, City, Phone, Email, CreatedAt
            FROM Customers
            ORDER BY Name;
            """;
        return connection.Query<CustomerRow>(sql).Select(ToCustomer).ToList();
    }

    public Customer? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT Id, Name, Nip, Street, PostalCode, City, Phone, Email, CreatedAt
            FROM Customers
            WHERE Id = @Id;
            """;
        var row = connection.QuerySingleOrDefault<CustomerRow>(sql, new { Id = id });
        return row is null ? null : ToCustomer(row);
    }

    public int Insert(Customer customer)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Customers (Name, Nip, Street, PostalCode, City, Phone, Email, CreatedAt)
            VALUES (@Name, @Nip, @Street, @PostalCode, @City, @Phone, @Email, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        if (customer.CreatedAt == default)
            customer.CreatedAt = DateTime.Now;

        var id = connection.QuerySingle<int>(sql, ToParameters(customer));
        customer.Id = id;
        return id;
    }

    public bool Update(Customer customer)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            UPDATE Customers
            SET Name = @Name,
                Nip = @Nip,
                Street = @Street,
                PostalCode = @PostalCode,
                City = @City,
                Phone = @Phone,
                Email = @Email
            WHERE Id = @Id;
            """;
        return connection.Execute(sql, ToParameters(customer)) > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Customers WHERE Id = @Id;";
        return connection.Execute(sql, new { Id = id }) > 0;
    }

    public bool ExistsByNip(string nip, int? excludeId)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "SELECT COUNT(1) FROM Customers WHERE Nip = @Nip AND Id <> @ExcludeId;";
        var count = connection.ExecuteScalar<int>(sql, new { Nip = nip, ExcludeId = excludeId ?? 0 });
        return count > 0;
    }

    private static Customer ToCustomer(CustomerRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Nip = Nip.Parse(row.Nip),
        Address = Address.Parse(row.Street, row.PostalCode, row.City),
        Phone = row.Phone,
        Email = Email.Parse(row.Email),
        CreatedAt = row.CreatedAt
    };

    private static object ToParameters(Customer customer) => new
    {
        customer.Id,
        customer.Name,
        Nip = customer.Nip.Value,
        Street = customer.Address.Street,
        PostalCode = customer.Address.PostalCode,
        City = customer.Address.City,
        customer.Phone,
        Email = customer.Email.Value,
        customer.CreatedAt
    };

    /// <summary>Flat shape matching the Customers table columns 1:1, used only for Dapper hydration.</summary>
    private sealed record CustomerRow(
        int Id,
        string Name,
        string Nip,
        string Street,
        string PostalCode,
        string City,
        string Phone,
        string Email,
        DateTime CreatedAt);
}
