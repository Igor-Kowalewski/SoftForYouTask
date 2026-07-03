using System.Text.RegularExpressions;

namespace CustomerCatalog.Core.Models;

/// <summary>
/// A postal address (street, postal code, city). Modeled as a value object rather than a
/// separate root entity: it has no identity of its own, is never shared between customers,
/// and is only ever meaningful as part of a <see cref="Customer"/>.
/// </summary>
public sealed record Address
{
    private static readonly Regex PostalCodeRegex = new(@"^\d{2}-\d{3}$", RegexOptions.Compiled);

    public string Street { get; }
    public string PostalCode { get; }
    public string City { get; }

    private Address(string street, string postalCode, string city)
    {
        Street = street;
        PostalCode = postalCode;
        City = city;
    }

    /// <summary>Parses an address from its parts. Throws <see cref="FormatException"/> on invalid input.</summary>
    public static Address Parse(string? street, string? postalCode, string? city)
    {
        if (!TryParse(street, postalCode, city, out var address, out var errors))
            throw new FormatException(string.Join(" ", errors));
        return address!;
    }

    /// <summary>
    /// Attempts to parse an address from its parts. Returns false without throwing when the
    /// input is invalid; <paramref name="errors"/> then contains one message per failed field
    /// (Polish, user-facing) so a caller can display them all at once.
    /// </summary>
    public static bool TryParse(string? street, string? postalCode, string? city, out Address? address, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        var streetValue = street?.Trim() ?? string.Empty;
        if (streetValue.Length == 0)
            errorList.Add("Ulica jest wymagana.");
        else if (streetValue.Length > 200)
            errorList.Add("Ulica nie może przekraczać 200 znaków.");

        var postalCodeValue = postalCode?.Trim() ?? string.Empty;
        if (!PostalCodeRegex.IsMatch(postalCodeValue))
            errorList.Add("Kod pocztowy jest niepoprawny (oczekiwany format NN-NNN).");

        var cityValue = city?.Trim() ?? string.Empty;
        if (cityValue.Length == 0)
            errorList.Add("Miasto jest wymagane.");
        else if (cityValue.Length > 100)
            errorList.Add("Miasto nie może przekraczać 100 znaków.");

        if (errorList.Count > 0)
        {
            address = null;
            errors = errorList;
            return false;
        }

        address = new Address(streetValue, postalCodeValue, cityValue);
        errors = Array.Empty<string>();
        return true;
    }

    public override string ToString() => $"{Street}, {PostalCode} {City}";
}
