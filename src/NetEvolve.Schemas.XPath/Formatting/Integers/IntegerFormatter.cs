namespace NetEvolve.Schemas.XPath.Formatting.Integers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

/// <summary>
/// Writes an integer of any width in the numbering sequence of an <see cref="IntegerPicture"/>: decimal digits with grouping,
/// alphabetic, roman, and English words, with the English ordinal form. The output does not depend on the culture.
/// </summary>
/// <remarks>
/// Limitations: a value that its sequence cannot show is written as decimal digits, which the specification permits: zero as a letter or
/// a roman numeral, a roman numeral above 3999, and words above the decillions. Alphabetic and roman sequences have no ordinal form in
/// English, so the modifier has no effect there.
/// </remarks>
internal static class IntegerFormatter
{
    private static readonly string[] Ones =
    [
        "zero",
        "one",
        "two",
        "three",
        "four",
        "five",
        "six",
        "seven",
        "eight",
        "nine",
        "ten",
        "eleven",
        "twelve",
        "thirteen",
        "fourteen",
        "fifteen",
        "sixteen",
        "seventeen",
        "eighteen",
        "nineteen",
    ];

    private static readonly string[] Tens =
    [
        "",
        "",
        "twenty",
        "thirty",
        "forty",
        "fifty",
        "sixty",
        "seventy",
        "eighty",
        "ninety",
    ];

    private static readonly string[] Scales =
    [
        "",
        "thousand",
        "million",
        "billion",
        "trillion",
        "quadrillion",
        "quintillion",
        "sextillion",
        "septillion",
        "octillion",
        "nonillion",
        "decillion",
    ];

    private static readonly string[] RomanSymbols =
    [
        "M",
        "CM",
        "D",
        "CD",
        "C",
        "XC",
        "L",
        "XL",
        "X",
        "IX",
        "V",
        "IV",
        "I",
    ];
    private static readonly int[] RomanValues = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];

    /// <summary>Formats a value.</summary>
    /// <param name="value">The value.</param>
    /// <param name="picture">The parsed picture.</param>
    /// <returns>The text; a negative value is the text of its absolute value after a hyphen-minus.</returns>
    public static string Format(BigInteger value, IntegerPicture picture)
    {
        var magnitude = BigInteger.Abs(value);
        var text = picture.Sequence == IntegerSequence.Decimal ? null : Sequence(magnitude, picture.Sequence);
        text ??= Decimal(magnitude, picture);
        if (picture.Ordinal)
        {
            text = Ordinal(text, magnitude, picture.Sequence);
        }

        return value.Sign < 0 ? "-" + text : text;
    }

    private static string? Sequence(BigInteger value, IntegerSequence sequence)
    {
        switch (sequence)
        {
            case IntegerSequence.AlphaLower:
            case IntegerSequence.AlphaUpper:
                return value.IsZero ? null : Alphabetic(value, sequence == IntegerSequence.AlphaUpper ? 'A' : 'a');
            case IntegerSequence.RomanLower:
            case IntegerSequence.RomanUpper:
                if (value.IsZero || value > 3999)
                {
                    return null;
                }

                var roman = Roman((int)value);
                return sequence == IntegerSequence.RomanLower ? Lower(roman) : roman;
            default:
                var words = Words(value);
                if (words is null)
                {
                    return null;
                }

                return sequence switch
                {
                    IntegerSequence.WordsUpper => words.ToUpperInvariant(),
                    IntegerSequence.WordsTitle => Title(words),
                    _ => words,
                };
        }
    }

    private static string Decimal(BigInteger value, IntegerPicture picture)
    {
        var digits = value.ToString(CultureInfo.InvariantCulture).PadLeft(picture.MinimumDigits, '0');
        var builder = new StringBuilder(digits.Length + 8);
        for (var i = 0; i < digits.Length; i++)
        {
            if (i > 0 && SeparatorBefore(picture, digits.Length - i) is { } separator)
            {
                _ = builder.Append(separator);
            }

            _ = builder.Append(
                picture.ZeroDigit == '0'
                    ? digits[i].ToString()
                    : char.ConvertFromUtf32(picture.ZeroDigit + digits[i] - '0')
            );
        }

        return builder.ToString();
    }

    // The separator written to the left of the digits that have this many digits (the digit itself and the ones to its right).
    private static string? SeparatorBefore(IntegerPicture picture, int digitsFromRight)
    {
        if (picture.RegularInterval > 0)
        {
            return digitsFromRight % picture.RegularInterval == 0 ? picture.Separators[0].Value : null;
        }

        foreach (var pair in picture.Separators)
        {
            if (pair.Key == digitsFromRight)
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static string Alphabetic(BigInteger value, char first)
    {
        var letters = new StringBuilder();
        while (!value.IsZero)
        {
            value = BigInteger.DivRem(value - 1, 26, out var remainder);
            _ = letters.Insert(0, (char)(first + (int)remainder));
        }

        return letters.ToString();
    }

    private static string Roman(int value)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < RomanValues.Length; i++)
        {
            while (value >= RomanValues[i])
            {
                _ = builder.Append(RomanSymbols[i]);
                value -= RomanValues[i];
            }
        }

        return builder.ToString();
    }

    private static string? Words(BigInteger value)
    {
        if (value.IsZero)
        {
            return Ones[0];
        }

        var groups = new List<int>();
        while (!value.IsZero)
        {
            value = BigInteger.DivRem(value, 1000, out var group);
            groups.Add((int)group);
        }

        if (groups.Count > Scales.Length)
        {
            return null;
        }

        var builder = new StringBuilder();
        for (var i = groups.Count - 1; i >= 0; i--)
        {
            var group = groups[i];
            if (group == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                _ = builder.Append(i == 0 && group < 100 ? " and " : " ");
            }

            var hundreds = group / 100;
            var rest = group % 100;
            if (hundreds > 0)
            {
                _ = builder.Append(Ones[hundreds]).Append(" hundred");
            }

            if (rest > 0)
            {
                _ = builder.Append(hundreds > 0 ? " and " : string.Empty);
                _ = builder.Append(
                    rest < 20 ? Ones[rest] : Tens[rest / 10] + (rest % 10 > 0 ? "-" + Ones[rest % 10] : string.Empty)
                );
            }

            if (i > 0)
            {
                _ = builder.Append(' ').Append(Scales[i]);
            }
        }

        return builder.ToString();
    }

    // The text is ASCII, so the culture never matters.
    private static string Lower(string text)
    {
        var chars = text.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'A' and <= 'Z')
            {
                chars[i] = (char)(chars[i] + 32);
            }
        }

        return new string(chars);
    }

    private static string Title(string words)
    {
        var chars = words.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (i == 0 || chars[i - 1] == ' ' || chars[i - 1] == '-')
            {
                chars[i] = char.ToUpperInvariant(chars[i]);
            }
        }

        return new string(chars);
    }

    private static string Ordinal(string text, BigInteger magnitude, IntegerSequence sequence) =>
        sequence switch
        {
            IntegerSequence.Decimal => text + DecimalSuffix(magnitude),
            IntegerSequence.WordsLower or IntegerSequence.WordsUpper or IntegerSequence.WordsTitle => OrdinalWords(
                text
            ),
            _ => text,
        };

    private static string DecimalSuffix(BigInteger magnitude)
    {
        var lastTwo = (int)(magnitude % 100);
        if (lastTwo is >= 11 and <= 13)
        {
            return "th";
        }

        return (lastTwo % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        };
    }

    // The last word takes the ordinal form; the case of the text is kept (a word that was written in capitals stays in capitals).
    private static string OrdinalWords(string text)
    {
        var cut = text.LastIndexOfAny([' ', '-']) + 1;
        var word = text.Substring(cut);
        var lower = Lower(word);
        var ordinal = lower switch
        {
            "one" => "first",
            "two" => "second",
            "three" => "third",
            "five" => "fifth",
            "eight" => "eighth",
            "nine" => "ninth",
            "twelve" => "twelfth",
            _ => lower[lower.Length - 1] == 'y'
                ? new StringBuilder(lower, 0, lower.Length - 1, lower.Length + 3).Append("ieth").ToString()
                : lower + "th",
        };
        if (char.IsUpper(word[word.Length - 1]))
        {
            ordinal = ordinal.ToUpperInvariant();
        }
        else if (char.IsUpper(word[0]))
        {
            var chars = ordinal.ToCharArray();
            chars[0] = char.ToUpperInvariant(chars[0]);
            ordinal = new string(chars);
        }

        return new StringBuilder(text, 0, cut, cut + ordinal.Length).Append(ordinal).ToString();
    }
}
