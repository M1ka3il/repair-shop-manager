using System.Text.RegularExpressions;
using RepairShop.Domain.Common;

namespace RepairShop.Domain.ValueObjects;

/// <summary>
/// A German license plate ("Kennzeichen"), e.g. <c>M-AB 1234</c>.
/// Modelled as a value object because a plate is defined by its value, not by an
/// identity, and because the validation belongs next to the data — not in a
/// controller or a database check constraint.
/// </summary>
public sealed partial record LicensePlate
{
    private LicensePlate(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Parses and normalises a plate. Input is trimmed and upper-cased so that
    /// "m-ab 1234" and "M-AB 1234" are the same plate.
    /// </summary>
    /// <exception cref="DomainException">The input is not a well-formed plate.</exception>
    public static LicensePlate Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new DomainException("License plate must not be empty.");
        }

        var normalised = NormaliseWhitespace(input.Trim().ToUpperInvariant());

        if (!PlatePattern().IsMatch(normalised))
        {
            throw new DomainException(
                $"'{input}' is not a valid German license plate (expected e.g. 'M-AB 1234').");
        }

        return new LicensePlate(normalised);
    }

    private static string NormaliseWhitespace(string value) =>
        WhitespaceRuns().Replace(value, " ");

    public override string ToString() => Value;

    // District (1-3 letters) - letters (1-2) space digits (1-4), optional E/H suffix
    // for electric and historic vehicles.
    [GeneratedRegex(@"^[A-ZÄÖÜ]{1,3}-[A-Z]{1,2} ?\d{1,4}[EH]?$")]
    private static partial Regex PlatePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRuns();
}
