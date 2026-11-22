namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Text.Unicode;
using NetEvolve.Schemas.XPath.Abstractions;

/// <summary>
/// An immutable, validated decimal format: every property of the decimal format of Functions and Operators 3.1 (4.7.1) and of
/// <c>xsl:decimal-format</c> (XSLT 2.0, 16.4.1). It is built from the mutable <see cref="XPathDecimalFormat"/> a host supplies, so that
/// the picture parser and the formatter read values that cannot change during a call.
/// </summary>
/// <remarks>
/// Limitation: a property that is a character is a UTF-16 code unit, as in <see cref="XPathDecimalFormat"/>, so a zero digit outside the
/// Basic Multilingual Plane (for instance the mathematical digits) cannot be named. The category check of the zero digit uses the Unicode
/// version pinned by the Xml package, so it is the same on every target framework.
/// </remarks>
internal sealed class DecimalFormatModel
{
    private const int DigitFamilySize = 10;

    private DecimalFormatModel(XPathDecimalFormat source)
    {
        DecimalSeparator = source.DecimalSeparator;
        GroupingSeparator = source.GroupingSeparator;
        ExponentSeparator = source.ExponentSeparator;
        MinusSign = source.MinusSign;
        Percent = source.Percent;
        PerMille = source.PerMille;
        ZeroDigit = source.ZeroDigit;
        Digit = source.Digit;
        PatternSeparator = source.PatternSeparator;
        Infinity = source.Infinity;
        NaN = source.NaN;
    }

    /// <summary>Gets the default format: the properties of <see cref="XPathDecimalFormat"/> with its defaults.</summary>
    public static DecimalFormatModel Default { get; } = new(new XPathDecimalFormat());

    /// <summary>Gets the decimal separator.</summary>
    public char DecimalSeparator { get; }

    /// <summary>Gets the grouping separator.</summary>
    public char GroupingSeparator { get; }

    /// <summary>Gets the exponent separator; the XSLT 2.0 rules do not read it.</summary>
    public char ExponentSeparator { get; }

    /// <summary>Gets the minus sign.</summary>
    public char MinusSign { get; }

    /// <summary>Gets the percent sign.</summary>
    public char Percent { get; }

    /// <summary>Gets the per-mille sign.</summary>
    public char PerMille { get; }

    /// <summary>Gets the zero digit, the first character of the decimal digit family.</summary>
    public char ZeroDigit { get; }

    /// <summary>Gets the optional digit sign of a picture.</summary>
    public char Digit { get; }

    /// <summary>Gets the pattern separator.</summary>
    public char PatternSeparator { get; }

    /// <summary>Gets the string of positive and negative infinity.</summary>
    public string Infinity { get; }

    /// <summary>Gets the string of not-a-number.</summary>
    public string NaN { get; }

    /// <summary>Validates a decimal format and snapshots it.</summary>
    /// <param name="format">The format a host supplied.</param>
    /// <param name="rules">The rule set that reads the pictures of the format: it decides which properties have to differ.</param>
    /// <param name="model">The immutable format, <see langword="null"/> when the format is not valid.</param>
    /// <param name="problem">The reason, <see langword="null"/> when the format is valid.</param>
    /// <returns><see langword="true"/> when the format is valid.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="format"/> is <see langword="null"/>.</exception>
    public static bool TryCreate(
        XPathDecimalFormat format,
        NumberPictureRules rules,
        out DecimalFormatModel? model,
        out string? problem
    )
    {
        ArgumentNullException.ThrowIfNull(format);

        model = null;
        if (format.Infinity is null || format.NaN is null)
        {
            problem = "The infinity and NaN properties of a decimal format must be strings.";
            return false;
        }

        if (!IsZeroDigit(format.ZeroDigit))
        {
            problem =
                "The zero-digit property of a decimal format must be a character of category Nd with the digit value zero.";
            return false;
        }

        if (!AreDistinct(format, rules))
        {
            problem =
                "The decimal-separator, grouping-separator, percent, per-mille, zero-digit, digit, and pattern-separator properties"
                + (
                    rules == NumberPictureRules.Xpath31
                        ? " (and the exponent-separator and the digit family)"
                        : string.Empty
                )
                + " of a decimal format must have distinct values.";
            return false;
        }

        problem = null;
        model = new DecimalFormatModel(format);
        return true;
    }

    // The ten digits of a family follow each other in the code space and the zero is the first: the character before it is no digit
    // of the family, and the last one is (the pinned Unicode tables only, never the digit value that the running .NET knows).
    private static bool IsZeroDigit(char candidate) =>
        UnicodeTables.GetCategory(candidate) == UnicodeGeneralCategory.Nd
        && UnicodeTables.GetCategory(candidate - 1) != UnicodeGeneralCategory.Nd
        && UnicodeTables.GetCategory(candidate + DigitFamilySize - 1) == UnicodeGeneralCategory.Nd;

    private static bool AreDistinct(XPathDecimalFormat format, NumberPictureRules rules)
    {
        var xpath31 = rules == NumberPictureRules.Xpath31;
        var count = xpath31 ? 7 : 6;
        Span<char> characters = stackalloc char[7];
        characters[0] = format.DecimalSeparator;
        characters[1] = format.GroupingSeparator;
        characters[2] = format.Percent;
        characters[3] = format.PerMille;
        characters[4] = format.Digit;
        characters[5] = format.PatternSeparator;
        characters[6] = format.ExponentSeparator;

        for (var i = 0; i < count; i++)
        {
            // The zero digit is one character of the family, and under the 3.1 rules none of the others may be any of the ten.
            var inFamily = xpath31
                ? characters[i] >= format.ZeroDigit && characters[i] < format.ZeroDigit + DigitFamilySize
                : characters[i] == format.ZeroDigit;
            if (inFamily)
            {
                return false;
            }

            for (var j = i + 1; j < count; j++)
            {
                if (characters[i] == characters[j])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
