namespace CustomerCatalog.Core.Models;

/// <summary>
/// Reprezentuje pojedynczego klienta w katalogu.
/// </summary>
public class Customer
{
    public int Id { get; set; }

    /// <summary>Nazwa (firmy lub osoby).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Numer NIP (10 cyfr).</summary>
    public string Nip { get; set; } = string.Empty;

    /// <summary>Adres.</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Numer telefonu.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>Adres e-mail.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Data utworzenia rekordu.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Tworzy płytką kopię (przydatne w formularzu edycji – edycja na kopii, zapis dopiero po „Zapisz”).</summary>
    public Customer Clone() => (Customer)MemberwiseClone();
}
