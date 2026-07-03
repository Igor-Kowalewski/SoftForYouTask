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
    /// (Polish, user-facing) so a caller can display them all at once. Delegates to the
    /// per-field Validate* methods below, which are also used for inline field validation in
    /// the UI, so both places agree on the exact same rules.
    /// </summary>
    public static bool TryParse(string? street, string? postalCode, string? city, out Address? address, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        var streetError = ValidateStreet(street);
        if (streetError is not null)
            errorList.Add(streetError);

        var postalCodeError = ValidatePostalCode(postalCode);
        if (postalCodeError is not null)
            errorList.Add(postalCodeError);

        var cityError = ValidateCity(city);
        if (cityError is not null)
            errorList.Add(cityError);

        if (errorList.Count > 0)
        {
            address = null;
            errors = errorList;
            return false;
        }

        address = new Address(street!.Trim(), postalCode!.Trim(), city!.Trim());
        errors = Array.Empty<string>();
        return true;
    }

    /// <summary>Validates the street. Returns null when valid, otherwise a Polish error message.</summary>
    public static string? ValidateStreet(string? street)
    {
        var value = street?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return "Ulica jest wymagana.";
        if (value.Length > 200)
            return "Ulica nie może przekraczać 200 znaków.";
        return null;
    }

    /// <summary>Validates the postal code (Polish NN-NNN format). Returns null when valid, otherwise a Polish error message.</summary>
    public static string? ValidatePostalCode(string? postalCode)
    {
        var value = postalCode?.Trim() ?? string.Empty;
        return PostalCodeRegex.IsMatch(value) ? null : "Kod pocztowy jest niepoprawny (oczekiwany format NN-NNN).";
    }

    /// <summary>Validates the city. Returns null when valid, otherwise a Polish error message.</summary>
    public static string? ValidateCity(string? city)
    {
        var value = city?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return "Miasto jest wymagane.";
        if (value.Length > 100)
            return "Miasto nie może przekraczać 100 znaków.";
        return null;
    }

    public override string ToString() => $"{Street}, {PostalCode} {City}";
}
