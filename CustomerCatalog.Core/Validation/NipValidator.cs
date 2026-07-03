namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Walidacja polskiego numeru NIP (10 cyfr + cyfra kontrolna).
/// </summary>
public static class NipValidator
{
    private static readonly int[] Weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

    /// <summary>
    /// Sprawdza poprawność NIP. Akceptuje wartości z myślnikami/spacjami (np. "123-456-32-18").
    /// </summary>
    public static bool IsValid(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
            return false;

        var digits = new string(nip.Where(char.IsDigit).ToArray());
        if (digits.Length != 10)
            return false;

        // NIP z samych identycznych cyfr (np. 0000000000) jest formalnie niepoprawny.
        if (digits.All(c => c == digits[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * Weights[i];

        var checkDigit = sum % 11;
        if (checkDigit == 10)
            return false; // NIP z taką sumą kontrolną nie istnieje.

        return checkDigit == digits[9] - '0';
    }

    /// <summary>Usuwa separatory, zostawiając same cyfry.</summary>
    public static string Normalize(string? nip) =>
        string.IsNullOrEmpty(nip) ? string.Empty : new string(nip.Where(char.IsDigit).ToArray());
}
