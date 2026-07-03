namespace CustomerCatalog.Core.Models;

/// <summary>
/// Represents a single customer in the catalog.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>Name (company or person).</summary>
    public string Name { get; set; } = string.Empty;

    // Nip/Address/Email have no meaningful "empty" default (a value either is a valid NIP or
    // doesn't exist), so they default to null-forgiven here. Every code path that produces a
    // Customer (the repository, hydrating from a valid row, or CustomerValidator.TryValidate,
    // which fails the whole validation before a Customer is built) always populates them.

    /// <summary>Polish tax identification number.</summary>
    public Nip Nip { get; set; } = null!;

    /// <summary>Postal address.</summary>
    public Address Address { get; set; } = null!;

    /// <summary>Phone number.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>E-mail address.</summary>
    public Email Email { get; set; } = null!;

    /// <summary>Record creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}
