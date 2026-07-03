using CustomerCatalog.Core.Models;

namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Validates raw form input and, when it is valid, produces a ready-to-save
/// <see cref="Customer"/>. Field-format rules (NIP checksum, e-mail format, postal code
/// format) live on the respective value objects (<see cref="Nip"/>, <see cref="Email"/>,
/// <see cref="Address"/>) — this type only validates the remaining plain fields and
/// aggregates every error message so the UI can display them all at once.
/// </summary>
public static class CustomerValidator
{
    /// <summary>
    /// Attempts to build a <see cref="Customer"/> from raw input. Returns false when any
    /// field is invalid; <paramref name="errors"/> then holds one Polish, user-facing
    /// message per problem. The resulting <see cref="Customer"/> still needs its Id and
    /// CreatedAt filled in by the caller (this method has no notion of "new" vs "existing").
    /// </summary>
    public static bool TryValidate(CustomerInput input, out Customer? customer, out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        var name = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            errorList.Add("Nazwa jest wymagana.");
        else if (name.Length > 200)
            errorList.Add("Nazwa nie może przekraczać 200 znaków.");

        if (!Nip.TryParse(input.Nip, out var nip))
            errorList.Add("NIP jest niepoprawny (wymagane 10 cyfr z prawidłową sumą kontrolną).");

        if (!Email.TryParse(input.Email, out var email))
            errorList.Add("E-mail ma niepoprawny format.");

        if (!Address.TryParse(input.Street, input.PostalCode, input.City, out var address, out var addressErrors))
            errorList.AddRange(addressErrors);

        var phone = input.Phone.Trim();
        if (phone.Length > 30)
            errorList.Add("Numer telefonu nie może przekraczać 30 znaków.");

        errors = errorList;
        if (errorList.Count > 0)
        {
            customer = null;
            return false;
        }

        customer = new Customer
        {
            Name = name,
            Nip = nip!,
            Address = address!,
            Phone = phone,
            Email = email!,
        };
        return true;
    }
}
