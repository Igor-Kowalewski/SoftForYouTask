using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Validates raw form input and, when it is valid, produces a ready-to-save
/// <see cref="Customer"/>. Field-format rules (NIP checksum, e-mail format, postal code
/// format) live on the respective value objects (<see cref="Nip"/>, <see cref="Email"/>,
/// <see cref="Address"/>) — this type owns the remaining plain fields (Name, Phone) and
/// aggregates every error message so the UI can display them all at once. The per-field
/// Validate* methods are also used directly for inline field validation in the UI, so both
/// places agree on the exact same rules.
/// </summary>
public static class CustomerValidator
{
    /// <summary>
    /// Attempts to build a <see cref="Customer"/> from raw input. Returns false when any
    /// field is invalid; <paramref name="errors"/> then holds one Polish, user-facing
    /// message per problem. The resulting <see cref="Customer"/> still needs its Id and
    /// CreatedAt filled in by the caller (this method has no notion of "new" vs "existing").
    /// Does not check NIP uniqueness against other customers — that requires the repository
    /// and is the caller's responsibility.
    /// </summary>
    public static bool TryValidate(CustomerInput input, out Customer? customer, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        var nameError = ValidateName(input.Name);
        if (nameError is not null)
            errorList.Add(nameError);

        if (!Nip.TryParse(input.Nip, out var nip))
            errorList.Add(Nip.InvalidFormatMessage);

        if (!Email.TryParse(input.Email, out var email))
            errorList.Add(Email.InvalidFormatMessage);

        if (!Address.TryParse(input.Street, input.PostalCode, input.City, out var address, out var addressErrors))
            errorList.AddRange(addressErrors);

        var phoneError = ValidatePhone(input.Phone);
        if (phoneError is not null)
            errorList.Add(phoneError);

        errors = errorList;
        if (errorList.Count > 0)
        {
            customer = null;
            return false;
        }

        customer = new Customer
        {
            Name = input.Name.Trim(),
            Nip = nip!,
            Address = address!,
            Phone = input.Phone.Trim(),
            Email = email!,
        };
        return true;
    }

    /// <summary>Validates the customer name. Returns null when valid, otherwise a Polish error message.</summary>
    public static string? ValidateName(string? name)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length == 0)
            return "Nazwa jest wymagana.";
        if (value.Length > 200)
            return "Nazwa nie może przekraczać 200 znaków.";
        return null;
    }

    /// <summary>Validates the phone number. Returns null when valid, otherwise a Polish error message.</summary>
    public static string? ValidatePhone(string? phone)
    {
        var value = phone?.Trim() ?? string.Empty;
        return value.Length > 30 ? "Numer telefonu nie może przekraczać 30 znaków." : null;
    }
}
