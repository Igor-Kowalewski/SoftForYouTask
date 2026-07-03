using System.Text.RegularExpressions;
using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Waliduje model <see cref="Customer"/> przed zapisem.
/// </summary>
public static class CustomerValidator
{
    // Prosty, praktyczny wzorzec e-mail (nie pełny RFC, ale wystarczający dla katalogu).
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Zwraca listę komunikatów błędów. Pusta lista oznacza, że model jest poprawny.
    /// </summary>
    public static IReadOnlyList<string> Validate(Customer customer)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(customer.Name))
            errors.Add("Nazwa jest wymagana.");
        else if (customer.Name.Length > 200)
            errors.Add("Nazwa nie może przekraczać 200 znaków.");

        if (!NipValidator.IsValid(customer.Nip))
            errors.Add("NIP jest niepoprawny (wymagane 10 cyfr z prawidłową sumą kontrolną).");

        if (string.IsNullOrWhiteSpace(customer.Email))
            errors.Add("E-mail jest wymagany.");
        else if (!EmailRegex.IsMatch(customer.Email))
            errors.Add("E-mail ma niepoprawny format.");

        if (!string.IsNullOrWhiteSpace(customer.Phone) && customer.Phone.Length > 30)
            errors.Add("Numer telefonu nie może przekraczać 30 znaków.");

        return errors;
    }

    public static bool IsValid(Customer customer) => Validate(customer).Count == 0;
}
