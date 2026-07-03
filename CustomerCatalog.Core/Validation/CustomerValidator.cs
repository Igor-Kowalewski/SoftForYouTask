using System.Text.RegularExpressions;
using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Validates a <see cref="Customer"/> before it is saved.
/// </summary>
public static class CustomerValidator
{
    // Simple, practical e-mail pattern (not full RFC, but sufficient for the catalog).
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Returns a list of error messages. An empty list means the model is valid.
    /// The messages are user-facing and therefore in Polish (the application UI language).
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
