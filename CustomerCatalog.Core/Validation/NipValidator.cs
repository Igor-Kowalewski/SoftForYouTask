namespace CustomerCatalog.Core.Validation;

/// <summary>
/// Validation of a Polish NIP number (10 digits + check digit).
/// </summary>
public static class NipValidator
{
    private static readonly int[] Weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

    /// <summary>
    /// Checks whether the NIP is valid. Accepts values with separators such as
    /// dashes or spaces (e.g. "123-456-32-18").
    /// </summary>
    public static bool IsValid(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip))
            return false;

        var digits = new string(nip.Where(char.IsDigit).ToArray());
        if (digits.Length != 10)
            return false;

        // A NIP made of identical digits (e.g. 0000000000) is formally invalid.
        if (digits.All(c => c == digits[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * Weights[i];

        var checkDigit = sum % 11;
        if (checkDigit == 10)
            return false; // No valid NIP produces a check digit of 10.

        return checkDigit == digits[9] - '0';
    }

    /// <summary>Removes separators, leaving only the digits.</summary>
    public static string Normalize(string? nip) =>
        string.IsNullOrEmpty(nip) ? string.Empty : new string(nip.Where(char.IsDigit).ToArray());
}
