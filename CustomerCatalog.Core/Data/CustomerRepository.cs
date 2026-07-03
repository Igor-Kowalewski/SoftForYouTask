using CustomerCatalog.Core.Models;
using Dapper;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Dapper-based repository implementation (parameterized SQL queries).
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
            SELECT Id, Name, Nip, Address, Phone, Email, CreatedAt
            FROM Customers
            ORDER BY Name;
            """;
        return connection.Query<Customer>(sql).ToList();
    }

    public Customer? GetById(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            SELECT Id, Name, Nip, Address, Phone, Email, CreatedAt
            FROM Customers
            WHERE Id = @Id;
            """;
        return connection.QuerySingleOrDefault<Customer>(sql, new { Id = id });
    }

    public int Insert(Customer customer)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = """
            INSERT INTO Customers (Name, Nip, Address, Phone, Email, CreatedAt)
            VALUES (@Name, @Nip, @Address, @Phone, @Email, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS int);
            """;

        if (customer.CreatedAt == default)
            customer.CreatedAt = DateTime.Now;

        var id = connection.QuerySingle<int>(sql, customer);
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
                Address = @Address,
                Phone = @Phone,
                Email = @Email
            WHERE Id = @Id;
            """;
        return connection.Execute(sql, customer) > 0;
    }

    public bool Delete(int id)
    {
        using var connection = _connectionFactory.CreateOpenConnection();
        const string sql = "DELETE FROM Customers WHERE Id = @Id;";
        return connection.Execute(sql, new { Id = id }) > 0;
    }
}
