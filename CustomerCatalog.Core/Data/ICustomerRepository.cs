using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Data;

/// <summary>
/// Repozytorium klientów – warstwa dostępu do danych (CRUD).
/// </summary>
public interface ICustomerRepository
{
    IReadOnlyList<Customer> GetAll();

    Customer? GetById(int id);

    /// <summary>Wstawia nowego klienta i zwraca nadane Id.</summary>
    int Insert(Customer customer);

    /// <summary>Aktualizuje istniejącego klienta. Zwraca true, jeśli rekord istniał.</summary>
    bool Update(Customer customer);

    /// <summary>Usuwa klienta po Id. Zwraca true, jeśli rekord istniał.</summary>
    bool Delete(int id);
}
