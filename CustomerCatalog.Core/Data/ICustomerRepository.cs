using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Customer repository – data access layer (CRUD).
/// </summary>
public interface ICustomerRepository
{
    IReadOnlyList<Customer> GetAll();

    Customer? GetById(int id);

    /// <summary>Inserts a new customer and returns the assigned Id.</summary>
    int Insert(Customer customer);

    /// <summary>Updates an existing customer. Returns true if the record existed.</summary>
    bool Update(Customer customer);

    /// <summary>Deletes a customer by Id. Returns true if the record existed.</summary>
    bool Delete(int id);

    /// <summary>
    /// Checks whether a customer other than <paramref name="excludeId"/> already has this NIP.
    /// Pass null for <paramref name="excludeId"/> when checking a brand new customer.
    /// </summary>
    bool ExistsByNip(string nip, int? excludeId);
}
