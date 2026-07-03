namespace CustomerCatalog.Core.Models;

/// <summary>
/// A Polish tax identification number (NIP). Instances are always valid (10 digits with
/// a correct check digit) — validation happens once, at construction, so the type itself
/// guarantees correctness instead of relying on callers to validate a raw string.
/// </summary>
public sealed record Nip
{
    /// <summary>User-facing message used both for inline field validation and the final save check.</summary>
    public const string InvalidFormatMessage = "NIP jest niepoprawny (wymagane 10 cyfr z prawidłową sumą kontrolną).";

    private static readonly int[] Weights = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

    /// <summary>The 10-digit NIP, normalized (no separators).</summary>
    public string Value { get; }

    private Nip(string value) => Value = value;

    /// <summary>Parses a NIP, accepting separators such as dashes or spaces. Throws on invalid input.</summary>
    public static Nip Parse(string? value) =>
        TryParse(value, out var nip) ? nip! : throw new FormatException($"'{value}' is not a valid NIP.");

    /// <summary>Attempts to parse a NIP. Returns false without throwing when the input is invalid.</summary>
    public static bool TryParse(string? value, out Nip? result)
    {
        var digits = ExtractDigits(value);
        if (IsChecksumValid(digits))
        {
            result = new Nip(digits);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>Checks validity without constructing an instance.</summary>
    public static bool IsValid(string? value) => TryParse(value, out _);

    /// <summary>Validates the format only (not uniqueness). Returns null when valid, otherwise a Polish error message.</summary>
    public static string? Validate(string? value) => IsValid(value) ? null : InvalidFormatMessage;

    private static string ExtractDigits(string? value) =>
        value is null ? string.Empty : new string(value.Where(char.IsDigit).ToArray());

    private static bool IsChecksumValid(string digits)
    {
        if (digits.Length != 10)
            return false;

        // A NIP made of identical digits (e.g. 0000000000) is formally invalid.
        if (digits.All(c => c == digits[0]))
            return false;

        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (digits[i] - '0') * Weights[i];

        var checkDigit = sum % 11;
        return checkDigit != 10 && checkDigit == digits[9] - '0';
    }

    public override string ToString() => Value;
}
