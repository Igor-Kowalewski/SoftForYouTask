namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Raw, unvalidated text coming from the edit form.
/// </summary>
public sealed record CustomerInput(
    string Name,
    string Nip,
    string Street,
    string PostalCode,
    string City,
    string Phone,
    string Email);
