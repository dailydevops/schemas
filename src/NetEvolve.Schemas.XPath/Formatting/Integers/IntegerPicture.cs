namespace NetEvolve.Schemas.XPath.Formatting.Integers;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// A parsed picture string of <c>format-integer</c> (Functions and Operators 3.1, 4.6.1): a primary format token and an optional
/// modifier after the last semicolon. The parser is a separate component so that the date and time formatting can reuse it.
/// </summary>
/// <remarks>
/// Limitations: a token that names no supported sequence is read as <c>1</c>, which the specification permits; the variation in
/// parentheses of the modifier is checked and not applied, and the alphabetic and traditional markers have no effect.
/// </remarks>
internal sealed class IntegerPicture
{
    private IntegerPicture(IntegerSequence sequence, bool ordinal)
    {
        Sequence = sequence;
        Ordinal = ordinal;
    }

    /// <summary>Gets the numbering sequence.</summary>
    public IntegerSequence Sequence { get; }

    /// <summary>Gets a value indicating whether the modifier asks for the ordinal form.</summary>
    public bool Ordinal { get; }

    /// <summary>Gets the code point of the zero digit of a decimal pattern.</summary>
    public int ZeroDigit { get; private set; } = '0';

    /// <summary>Gets the number of mandatory digit signs of a decimal pattern: the minimum width.</summary>
    public int MinimumDigits { get; private set; } = 1;

    /// <summary>Gets the grouping separators of a decimal pattern as pairs of the position counted from the right and the separator.</summary>
    public IReadOnlyList<KeyValuePair<int, string>> Separators { get; private set; } =
        Array.Empty<KeyValuePair<int, string>>();

    /// <summary>Gets the interval of a regular grouping, or 0 when the separators are irregular or absent.</summary>
    public int RegularInterval { get; private set; }

    /// <summary>Parses a picture string.</summary>
    /// <param name="picture">The picture string.</param>
    /// <param name="result">The picture, <see langword="null"/> when the string is not valid.</param>
    /// <param name="problem">The rule that is broken, <see langword="null"/> when the string is valid.</param>
    /// <returns><see langword="true"/> when the string is valid.</returns>
    public static bool TryParse(string picture, out IntegerPicture? result, out string? problem)
    {
        result = null;
        var semicolon = picture.Length - 1;
        while (semicolon >= 0 && picture[semicolon] != ';')
        {
            semicolon--;
        }

        var token = semicolon < 0 ? picture : picture.Substring(0, semicolon);
        var modifier = semicolon < 0 ? string.Empty : picture.Substring(semicolon + 1);

        if (token.Length == 0)
        {
            problem = "The primary format token of the picture string is empty.";
            return false;
        }

        if (!TryParseModifier(modifier, out var ordinal))
        {
            problem = "The format modifier '" + modifier + "' is not valid.";
            return false;
        }

        if (HasDecimalDigit(token))
        {
            return TryParseDecimal(token, ordinal, out result, out problem);
        }

        problem = null;
        result = new IntegerPicture(
            token switch
            {
                "a" => IntegerSequence.AlphaLower,
                "A" => IntegerSequence.AlphaUpper,
                "i" => IntegerSequence.RomanLower,
                "I" => IntegerSequence.RomanUpper,
                "w" => IntegerSequence.WordsLower,
                "W" => IntegerSequence.WordsUpper,
                "Ww" => IntegerSequence.WordsTitle,
                _ => IntegerSequence.Decimal,
            },
            ordinal
        );
        return true;
    }

    // The modifier matches ^([co](\(.+\))?)?[at]?$.
    private static bool TryParseModifier(string modifier, out bool ordinal)
    {
        var rest = modifier;
        ordinal = rest.Length > 0 && rest[0] == 'o';
        if (rest.Length > 0 && (rest[0] == 'c' || rest[0] == 'o'))
        {
            rest = rest.Substring(1);
            if (rest.Length > 0 && rest[0] == '(')
            {
                if (rest[rest.Length - 1] is 'a' or 't')
                {
                    var trimmed = rest.Substring(0, rest.Length - 1);
                    if (trimmed[trimmed.Length - 1] == ')')
                    {
                        rest = trimmed;
                    }
                }

                return rest.Length >= 3 && rest[rest.Length - 1] == ')';
            }
        }

        return rest.Length == 0 || rest == "a" || rest == "t";
    }

    private static bool HasDecimalDigit(string token)
    {
        for (var i = 0; i < token.Length; i += char.IsHighSurrogate(token, i) ? 2 : 1)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(token, i) == UnicodeCategory.DecimalDigitNumber)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseDecimal(string token, bool ordinal, out IntegerPicture? result, out string? problem)
    {
        result = null;
        var zero = -1;
        var mandatory = 0;
        var digits = 0;
        var previousSeparator = true; // a separator may not start the pattern
        var separators = new List<KeyValuePair<int, string>>();

        for (var i = 0; i < token.Length; i += char.IsHighSurrogate(token, i) ? 2 : 1)
        {
            var length = char.IsHighSurrogate(token, i) ? 2 : 1;
            var category = CharUnicodeInfo.GetUnicodeCategory(token, i);
            if (category == UnicodeCategory.DecimalDigitNumber)
            {
                var familyZero = char.ConvertToUtf32(token, i) - (int)CharUnicodeInfo.GetDecimalDigitValue(token, i);
                if (zero >= 0 && familyZero != zero)
                {
                    problem = "The mandatory digit signs of the picture string are from different digit families.";
                    return false;
                }

                zero = familyZero;
                mandatory++;
                digits++;
                previousSeparator = false;
            }
            else if (token[i] == '#')
            {
                if (mandatory > 0)
                {
                    problem = "An optional digit sign follows a mandatory digit sign in the picture string.";
                    return false;
                }

                digits++;
                previousSeparator = false;
            }
            else if (IsAlphanumeric(category))
            {
                problem = "The picture string mixes digits with letters or other numbers.";
                return false;
            }
            else
            {
                if (previousSeparator)
                {
                    problem = "A grouping separator starts the picture string or follows another one.";
                    return false;
                }

                separators.Add(new KeyValuePair<int, string>(digits, token.Substring(i, length)));
                previousSeparator = true;
            }
        }

        if (previousSeparator)
        {
            problem = "A grouping separator ends the picture string.";
            return false;
        }

        // Counted from the right.
        var fromRight = new List<KeyValuePair<int, string>>(separators.Count);
        for (var i = separators.Count - 1; i >= 0; i--)
        {
            fromRight.Add(new KeyValuePair<int, string>(digits - separators[i].Key, separators[i].Value));
        }

        problem = null;
        result = new IntegerPicture(IntegerSequence.Decimal, ordinal)
        {
            ZeroDigit = zero,
            MinimumDigits = mandatory,
            Separators = fromRight,
            RegularInterval = ComputeInterval(fromRight, digits),
        };
        return true;
    }

    // Regular: one separator character at N, 2N, ... kN, and no multiple of N below the digit count lacks one.
    private static int ComputeInterval(List<KeyValuePair<int, string>> separators, int digits)
    {
        if (separators.Count == 0)
        {
            return 0;
        }

        var interval = separators[0].Key;
        for (var i = 0; i < separators.Count; i++)
        {
            if (separators[i].Key != (i + 1) * interval || separators[i].Value != separators[0].Value)
            {
                return 0;
            }
        }

        return (digits - 1) / interval == separators.Count ? interval : 0;
    }

    private static bool IsAlphanumeric(UnicodeCategory category) =>
        category
            is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.LetterNumber
                or UnicodeCategory.OtherNumber;
}
