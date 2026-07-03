using System.Text.RegularExpressions;

namespace CustomerCatalog.Core.Models;

/// <summary>
/// An e-mail address. Instances are always well-formed — validation happens once, at
/// construction, instead of being re-checked by every caller that happens to hold a raw string.
/// </summary>
public sealed record Email
{
    /// <summary>User-facing message used both for inline field validation and the final save check.</summary>
    public const string InvalidFormatMessage = "E-mail ma niepoprawny format.";

    // Simple, practical e-mail pattern (not full RFC, but sufficient for the catalog).
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>Parses an e-mail address. Throws <see cref="FormatException"/> on invalid input.</summary>
    public static Email Parse(string? value) =>
        TryParse(value, out var email) ? email! : throw new FormatException($"'{value}' is not a valid e-mail address.");

    /// <summary>Attempts to parse an e-mail address. Returns false without throwing when the input is invalid.</summary>
    public static bool TryParse(string? value, out Email? result)
    {
        var candidate = value?.Trim() ?? string.Empty;
        if (candidate.Length is > 0 and <= 200 && Pattern.IsMatch(candidate))
        {
            result = new Email(candidate);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>Validates the format. Returns null when valid, otherwise a Polish error message.</summary>
    public static string? Validate(string? value) => TryParse(value, out _) ? null : InvalidFormatMessage;

    public override string ToString() => Value;
}
