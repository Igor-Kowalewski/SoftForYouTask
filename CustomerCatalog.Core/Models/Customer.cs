namespace CustomerCatalog.Core.Models;

/// <summary>
/// Represents a single customer in the catalog.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>Name (company or person).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Polish tax identification number (NIP, 10 digits).</summary>
    public string Nip { get; set; } = string.Empty;

    /// <summary>Address.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Phone number.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>E-mail address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Record creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Creates a shallow copy. Used by the edit form so editing happens on a copy
    /// and the original is only replaced once the user confirms with "Save".
    /// </summary>
    public Customer Clone() => (Customer)MemberwiseClone();
}
