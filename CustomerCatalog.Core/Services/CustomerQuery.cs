using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Services;

/// <summary>
/// In-memory filtering and sorting of customers. Extracted from the UI so the
/// logic can be unit tested independently of WinForms.
/// </summary>
public static class CustomerQuery
{
    /// <summary>
    /// Returns customers whose name, NIP, e-mail, phone or address contains
    /// <paramref name="term"/> (case-insensitive). An empty term returns all.
    /// </summary>
    public static IEnumerable<Customer> Filter(IEnumerable<Customer> source, string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return source;

        var needle = term.Trim();
        return source.Where(c =>
            Contains(c.Name, needle) ||
            Contains(c.Nip.Value, needle) ||
            Contains(c.Email.Value, needle) ||
            Contains(c.Phone, needle) ||
            Contains(c.Address.ToString(), needle));
    }

    /// <summary>
    /// Sorts customers by the given property name (matching <see cref="Customer"/>
    /// property names). Unknown names fall back to sorting by <see cref="Customer.Name"/>.
    /// </summary>
    public static IEnumerable<Customer> Sort(IEnumerable<Customer> source, string? propertyName, bool ascending)
    {
        Func<Customer, object?> key = propertyName switch
        {
            nameof(Customer.Nip) => c => c.Nip.Value,
            nameof(Customer.Address) => c => c.Address.ToString(),
            nameof(Customer.Phone) => c => c.Phone,
            nameof(Customer.Email) => c => c.Email.Value,
            nameof(Customer.CreatedAt) => c => c.CreatedAt,
            _ => c => c.Name
        };

        return ascending
            ? source.OrderBy(key)
            : source.OrderByDescending(key);
    }

    private static bool Contains(string? value, string term) =>
        value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
